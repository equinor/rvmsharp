namespace RvmSharp.BatchUtils;

using Containers;
using Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class Workload
{
    public static (string rvmFilename, string? txtFilename)[] CollectWorkload(IReadOnlyCollection<string> filesAndFolders, string? filter = null)
    {
        var regexFilter = filter != null ? new Regex(filter) : null;
        var directories = filesAndFolders.Where(Directory.Exists).ToArray();
        var files = filesAndFolders.Where(File.Exists).ToArray();
        var missingInputs = filesAndFolders.Where(i => !files.Contains(i) && !directories.Contains(i)).ToArray();

        if (missingInputs.Any())
        {
            throw new FileNotFoundException(
                $"Missing file or folder: {Environment.NewLine}{string.Join(Environment.NewLine, missingInputs)}");
        }

        var inputFiles =
            directories.SelectMany(directory => Directory.GetFiles(directory, "*.rvm")) // Collect RVMs
                .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
                .Concat(files) // Append single files
                .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
                .GroupBy(Path.GetFileNameWithoutExtension).ToArray(); // Group by filename (rvm, txt)

        var workload = (from filePair in inputFiles
                        select filePair.ToArray()
            into filePairStatic
                        let rvmFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".rvm"))
                        let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
                        select (rvmFilename, txtFilename)).ToArray();

        var result = new List<(string, string?)>();
        foreach ((string? rvmFilename, string? txtFilename) in workload)
        {
            if (rvmFilename == null)
                Console.WriteLine(
                    $"No corresponding RVM file found for attributes: '{txtFilename}', the file will be skipped.");
            else
                result.Add((rvmFilename, txtFilename));
        }

        return result.ToArray();
    }

    public static async Task<RvmStore> ReadRvmDataAsync(
        IReadOnlyCollection<(string rvmFilename, string? txtFilename)> workload,
        IProgress<(string fileName, int progress, int total)>? progressReport = null,
        IStringInternPool? stringInternPool = null)
    {
        var progress = 0;
        var redundantPdmsAttributesToExclude = new[] { "Name", "Position" };

        async Task<RvmFile> ParseRvmFileAsync((string rvmFilename, string? txtFilename) filePair)
        {
            (string rvmFilename, string? txtFilename) = filePair;
            var bytes = await File.ReadAllBytesAsync(rvmFilename);
            using var _ms = new MemoryStream(bytes, 0, bytes.Length, false, false);
            var rvmFile = RvmParser.ReadRvm(_ms);
            if (!string.IsNullOrEmpty(txtFilename))
            {
                rvmFile.AttachAttributes(txtFilename!, redundantPdmsAttributesToExclude, stringInternPool);
            }
            progressReport?.Report((Path.GetFileNameWithoutExtension(rvmFilename), ++progress, workload.Count));
            return rvmFile;
        }
        var filesTimer = Stopwatch.StartNew();

        var rvmFiles = new List<RvmFile>();
        await Parallel.ForEachAsync(workload,
            new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 1) * 2.0)) }
            ,
            async (filePair,cancellationToken) =>
            {
                var res = await ParseRvmFileAsync(filePair);
                
                lock (rvmFiles)
                {
                    rvmFiles.Add(res);
                }
            }
        );

        if (stringInternPool != null)
        {
            Console.WriteLine(
                $"{stringInternPool.Considered:N0} PDMS strings were deduped into {stringInternPool.Added:N0} string objects. Reduced string allocation by {(float)stringInternPool.Deduped / stringInternPool.Considered:P1}.");
        }
        Console.WriteLine($"Files parsed in {filesTimer.Elapsed}");

        var rvmStore = new RvmStore();
        rvmStore.RvmFiles.AddRange(rvmFiles);
        progressReport?.Report(("Connecting geometry", 0, 2));
        RvmConnect.Connect(rvmStore);
        progressReport?.Report(("Aligning geometry", 1, 2));
        RvmAlign.Align(rvmStore);
        progressReport?.Report(("Import finished", 2, 2));
        return rvmStore;
    }
}