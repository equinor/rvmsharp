﻿namespace CadRevealRvmProvider.BatchUtils;

using System.Text.RegularExpressions;
using Commons;
using RvmSharp;
using RvmSharp.Containers;
using RvmSharp.Operations;

public static class RvmWorkload
{
    public static (string rvmFilename, string? txtFilename)[] CollectWorkload(
        IReadOnlyCollection<string> filesAndFolders,
        string? filter = null
    )
    {
        var regexFilter = filter != null ? new Regex(filter) : null;
        var directories = filesAndFolders.Where(Directory.Exists).ToArray();
        var files = filesAndFolders.Where(File.Exists).ToArray();
        var missingInputs = filesAndFolders.Where(i => !files.Contains(i) && !directories.Contains(i)).ToArray();

        if (missingInputs.Any())
        {
            throw new FileNotFoundException(
                $"Missing file or folder: {Environment.NewLine}{string.Join(Environment.NewLine, missingInputs)}"
            );
        }

        var inputFiles = directories
            .SelectMany(directory => Directory.GetFiles(directory, "*.rvm")) // Collect RVMs
            .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
            .Concat(
                files.Where(x =>
                    x.EndsWith(".rvm", StringComparison.OrdinalIgnoreCase)
                    || x.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                )
            ) // Append single files
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

    public static RvmStore ReadRvmFiles(
        IReadOnlyCollection<(string rvmFilename, string? txtFilename)> workload,
        IProgress<(string fileName, int progress, int total)>? progressReport = null,
        IStringInternPool? stringInternPool = null
    )
    {
        var progress = 0;
        var redundantPdmsAttributesToExclude = new[] { "Name", "Position" };

        RvmFile ParseRvmFile((string rvmFilename, string? txtFilename) filePair)
        {
            try
            {
                (string rvmFilename, string? txtFilename) = filePair;
                using var stream = File.OpenRead(rvmFilename);
                var rvmFile = RvmParser.ReadRvm(stream);
                if (!string.IsNullOrEmpty(txtFilename))
                {
                    rvmFile.AttachAttributes(txtFilename!, redundantPdmsAttributesToExclude, stringInternPool);
                }

                progressReport?.Report((Path.GetFileNameWithoutExtension(rvmFilename), ++progress, workload.Count));
                return rvmFile;
            }
            catch (Exception)
            {
                Console.Error.WriteLine(
                    "Failed to parse RVM file:"
                        + filePair.rvmFilename
                        + " or its attributes file: "
                        + filePair.txtFilename
                        + ". Check the exception log."
                );
                throw;
            }
        }

        var rvmFiles = workload.AsParallel().AsOrdered().Select(ParseRvmFile).ToArray();

        if (stringInternPool != null)
        {
            Console.WriteLine(
                $"{stringInternPool.Considered:N0} PDMS strings were deduped into {stringInternPool.Added:N0} string objects. Reduced string allocation by {(float)stringInternPool.Deduped / stringInternPool.Considered:P1}."
            );
        }

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
