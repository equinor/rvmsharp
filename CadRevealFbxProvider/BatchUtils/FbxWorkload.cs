﻿namespace CadRevealFbxProvider.BatchUtils;

using Attributes;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Tessellation;
using Commons;
using System.Text.RegularExpressions;

public static class FbxWorkload
{
    public static (string fbxFilename, string? attributeFilename)[] CollectWorkload(
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
            .SelectMany(directory => Directory.GetFiles(directory, "*.fbx")) // Collect fbx files
            .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.csv"))) // Collect CSVs
            .Concat(
                files.Where(
                    x =>
                        x.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)
                        || x.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                )
            ) // Append single files
            .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
            .GroupBy(Path.GetFileNameWithoutExtension)
            .ToArray(); // Group by filename (rvm, txt)

        var workload = (
            from filePair in inputFiles
            select filePair.ToArray() into filePairStatic
            let fbxFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".fbx"))
            let csvFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".csv"))
            select (fbxFilename, csvFilename)
        ).ToArray();

        var result = new List<(string, string?)>();
        foreach ((string? fbxFilename, string? attributeFilename) in workload)
        {
            if (fbxFilename == null && attributeFilename == null)
                continue; // Nothing found

            if (fbxFilename == null)
            {
                Console.WriteLine(
                    $"No corresponding FBX file found for attributes: '{attributeFilename}', the file will be skipped."
                );
            }
            else
                result.Add((fbxFilename, attributeFilename));
        }
        return result.ToArray();
    }

    public static IReadOnlyList<CadRevealNode> ReadFbxData(
        IReadOnlyCollection<(string fbxFilename, string? txtFilename)> workload,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        IProgress<(string fileName, int progress, int total)>? progressReport = null,
        IStringInternPool? stringInternPool = null
    )
    {
        var progress = 0;
        using var fbxImporter = new FbxImporter();
        if (!fbxImporter.HasValidSdk())
        {
            // returns an empty list and issues a warning
            Console.WriteLine("Did not find valid SDK, cannot import FBX file.");
            throw new Exception("FBX import failed due to outdated FBX SDK! Scene would be invalid, hence exiting.");
        }

        IReadOnlyList<CadRevealNode> LoadFbxFile((string fbxFilename, string? attributeFilename) filePair)
        {
            (string fbxFilename, string? infoTextFilename) = filePair;

            var rootNodeOfModel = fbxImporter.LoadFile(fbxFilename);
            var lookupA = new Dictionary<IntPtr, (Mesh, ulong)>();
            var nodesToProcess = FbxNodeToCadRevealNodeConverter
                .ConvertRecursive(rootNodeOfModel, treeIndexGenerator, instanceIdGenerator, fbxImporter, lookupA)
                .ToList();

            // attach attribute info to the nodes if there is any
            if (infoTextFilename != null)
            {
                var lines = File.ReadAllLines(infoTextFilename);
                var data = new ScaffoldingAttributeParser().ParseAttributes(lines);
                var flatNodes = nodesToProcess.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();

                var fbxNameIdRegex = new Regex(@"\[(\d+)\]");
                foreach (CadRevealNode cadRevealNode in flatNodes)
                {
                    var match = fbxNameIdRegex.Match(cadRevealNode.Name);
                    if (match.Success)
                    {
                        var id = match.Groups[1].Value;
                        if (data.TryGetValue(id, out Dictionary<string, string>? value))
                        {
                            foreach (var kvp in value)
                            {
                                cadRevealNode.Attributes.Add(kvp.Key, kvp.Value);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Data Id {id} does not exist in the attribute file.");
                        }
                    }
                }
            }

            progressReport?.Report((Path.GetFileNameWithoutExtension(fbxFilename), ++progress, workload.Count));
            return nodesToProcess;
        }

        var fbxNodes = workload.SelectMany(LoadFbxFile).ToArray();

        var fbxNodesFlat = fbxNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();

        if (stringInternPool != null)
        {
            Console.WriteLine(
                $"{stringInternPool.Considered:N0} PDMS strings were deduped into {stringInternPool.Added:N0} string objects. Reduced string allocation by {(float)stringInternPool.Deduped / stringInternPool.Considered:P1}."
            );
        }

        // TODO: check if/how something similar has to be done for FBX models
        //var rvmStore = new RvmStore();
        //rvmStore.RvmFiles.AddRange(fbxFiles);
        //progressReport?.Report(("Connecting geometry", 0, 2));
        //RvmConnect.Connect(rvmStore);
        //progressReport?.Report(("Aligning geometry", 1, 2));
        //RvmAlign.Align(rvmStore);
        //progressReport?.Report(("Import finished", 2, 2));
        return fbxNodesFlat;
    }
}
