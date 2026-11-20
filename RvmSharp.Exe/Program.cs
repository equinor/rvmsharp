namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using Containers;
using Operations;
using ShellProgressBar;

static class Program
{
    private static void Main(string[] args)
    {
        // use full Profile Guided Optimization
        Environment.SetEnvironmentVariable("DOTNET_ReadyToRun", "0");
        Environment.SetEnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1");
        Environment.SetEnvironmentVariable("DOTNET_TieredPGO", "1");

        var result = Parser
            .Default.ParseArguments<Options>(args)
            .MapResult(RunOptionsAndReturnExitCode, HandleParseError);
        Environment.Exit(result);
    }

    private static int HandleParseError(IEnumerable<Error> e)
    {
        return -1;
    }

    private static int RunOptionsAndReturnExitCode(Options options)
    {
        var workload = CollectWorkload(options);
        if (options.NativeMetadataOutput != null && (workload.Length != 1
            || options.NativeInstancesCsvOutput == null || options.NativeInstancesOutput != null
            || options.NativeInventoryOutput != null
            || (options.NativeSourceId != null && string.IsNullOrWhiteSpace(options.NativeSourceId))
            || (options.NativeRunId != null && !Guid.TryParse(options.NativeRunId, out _))))
            throw new ArgumentException("Deferred metadata requires one source RVM, CSV output, a nonempty source name and a valid run UUID (if supplied); JSON output is not supported.");

        var attributeExclusions = options.ExcludeAttributes
            .Select(s =>
            {
                var idx = s.IndexOf('=');
                if (idx < 1)
                    throw new ArgumentException(
                        $"Invalid --exclude-attribute value '{s}'. Expected format: 'AttributeKey=ValueRegex'.");
                var key = s[..idx];
                var pattern = new Regex(s[(idx + 1)..], RegexOptions.IgnoreCase);
                return (Key: key, ValuePattern: pattern);
            })
            .ToList();

        if (options.NativeReuseProbe)
        {
            NativeReuseProbe.Run(workload, attributeExclusions.Count > 0 ? attributeExclusions : null, options.Output);
            return 0;
        }

        if (options.NativeFacetCatalogBuild)
        {
            NativeFacetCatalog.Build(
                workload.Select(source => source.rvmFilename).ToArray(),
                options.Output,
                options.NativeFacetTolerance);
            return 0;
        }

        using var parentProgressBar = new ProgressBar(2, "Converting RVM to OBJ");

        var rvmStore = ReadRvmData(workload);

        using var connectProgressBar = parentProgressBar.Spawn(2, "Connecting geometry");
        RvmConnect.Connect(rvmStore);
        connectProgressBar.Tick();
        connectProgressBar.Message = "Aligning geometry";
        RvmAlign.Align(rvmStore);
        connectProgressBar.Tick();
        parentProgressBar.Tick();

        using var tessellationProgressBar = parentProgressBar.Spawn(1, "Tessellating");
        using var exportProgressBar = parentProgressBar.Spawn(1, "Exporting");

        if (options.NativeRemainsObj == null)
        {
            RvmObjExporter.ExportToObj(
                rvmStore,
                options.Tolerance,
                options.Output,
                attributeExclusions.Count > 0 ? attributeExclusions : null,
                ((i) => tessellationProgressBar.MaxTicks = i, () => tessellationProgressBar.Tick()),
                ((i) => exportProgressBar.MaxTicks = i, () => exportProgressBar.Tick()),
                options.TagNaming
            );
        }

        NativeInstanceExporter.Export(
            rvmStore,
            options.Tolerance,
            attributeExclusions.Count > 0 ? attributeExclusions : null,
            options.NativeInstancesOutput,
            options.NativeInventoryOutput,
            options.NativeInstancesCsvOutput,
            options.NativeInventoryCsvOutput,
            options.NativeRemainsObj,
            options.NativeUniqueObj,
            options.NativeObjectsPerFile,
            options.NativeTemplateUsageCsv,
            options.NativeMetadataOutput,
            options.NativeRunId,
            options.NativeSourceId,
            workload.Select(source => source.rvmFilename).ToArray(),
            options.NativeFacetCatalogPath
        );
        parentProgressBar.Tick();
        Console.WriteLine("Done!");
        return 0;
    }

    private static (string rvmFilename, string? txtFilename)[] CollectWorkload(Options options)
    {
        var regexFilter = options.Filter != null ? new Regex(options.Filter) : null;
        var inputs = options.InputList == null
            ? options.Inputs.ToArray()
            : options.Inputs
                .Concat(File.ReadLines(options.InputList).Select(line => line.Trim()).Where(line => line.Length > 0))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        var directories = inputs.Where(Directory.Exists).ToArray();
        var files = inputs.Where(File.Exists).ToArray();
        var missingInputs = inputs.Where(i => !files.Contains(i) && !directories.Contains(i)).ToArray();

        if (missingInputs.Any())
        {
            throw new FileNotFoundException(
                $"Missing file or folder: {Environment.NewLine}{string.Join(Environment.NewLine, missingInputs)}"
            );
        }

        var inputFiles = directories
            .SelectMany(directory => Directory.GetFiles(directory, "*.rvm")) // Collect RVMs
            .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
            .Concat(files) // Append single files
            .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
            .GroupBy(Path.GetFileNameWithoutExtension)
            .ToArray(); // Group by filename (rvm, txt)

        var workload = (
            from filePair in inputFiles
            select filePair.ToArray() into filePairStatic
            let rvmFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".rvm"))
            let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
            select (rvmFilename, txtFilename)
        ).ToArray();

        var result = new List<(string, string?)>();
        foreach ((string? rvmFilename, string? txtFilename) in workload)
        {
            if (rvmFilename == null)
                Console.WriteLine(
                    $"No corresponding RVM file found for attributes: '{txtFilename}', the file will be skipped."
                );
            else
                result.Add((rvmFilename, txtFilename));
        }

        return result.ToArray();
    }

    private static RvmStore ReadRvmData(IReadOnlyCollection<(string rvmFilename, string? txtFilename)> workload)
    {
        using var progressBar = new ProgressBar(workload.Count, "Parsing input");

        var rvmFiles = workload
            .Select(filePair =>
            {
                (string rvmFilename, string? txtFilename) = filePair;
                progressBar.Message = Path.GetFileNameWithoutExtension(rvmFilename);
                using var stream = File.OpenRead(rvmFilename);
                var rvmFile = RvmParser.ReadRvm(stream);
                if (!string.IsNullOrEmpty(txtFilename))
                {
                    rvmFile.AttachAttributes(txtFilename);
                }

                progressBar.Tick();
                return rvmFile;
            })
            .ToArray();
        var rvmStore = new RvmStore();
        rvmStore.RvmFiles.AddRange(rvmFiles);
        return rvmStore;
    }
}
