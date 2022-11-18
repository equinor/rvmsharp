namespace CadRevealFbxProvider.BatchUtils;

using CadRevealFbxProvider.Utils;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

using Commons;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class FbxWorkload
{
    public static (string fbxFilename, string? txtFilename)[] CollectWorkload(
        IReadOnlyCollection<string> filesAndFolders,
        string? filter = null)
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
            directories.SelectMany(directory => Directory.GetFiles(directory, "*.fbx")) // Collect fbx files
                .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
                .Concat(files) // Append single files
                .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
                .GroupBy(Path.GetFileNameWithoutExtension).ToArray(); // Group by filename (rvm, txt)

        var workload = (from filePair in inputFiles
            select filePair.ToArray()
            into filePairStatic
            let fbxFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".fbx"))
            let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
            select (fbxFilename, txtFilename)).ToArray();

        var result = new List<(string, string?)>();
        foreach ((string? fbxFilename, string? txtFilename) in workload)
        {
            if (fbxFilename == null)
            {
                Console.WriteLine(
                    $"No corresponding FBX file found for attributes: '{txtFilename}', the file will be skipped.");
            }

            else
                result.Add((fbxFilename, txtFilename));
        }
        return result.ToArray();
    }

    public static IReadOnlyList<CadRevealNode> ReadFbxData(
        IReadOnlyCollection<(string fbxFilename, string? txtFilename)> workload,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        IProgress<(string fileName, int progress, int total)>? progressReport = null,
        IStringInternPool? stringInternPool = null)
    {
        var progress = 0;
        var redundantPdmsAttributesToExclude = new[] { "Name", "Position" };

        using var fbxImporter = new FbxImporter();

        IReadOnlyList<CadRevealNode> LoadFbxFile((string rvmFilename, string? txtFilename) filePair)
        {
            (string fbxFilename, string? infoTextFilename) = filePair;

            var rootNodeOfModel = fbxImporter.LoadFile(fbxFilename);

            if (!string.IsNullOrEmpty(infoTextFilename))
            {
                // TODO
                // attach attributes if they exist !!
               // (...).AttachAttributes(txtFilename!, redundantPdmsAttributesToExclude, stringInternPool);
            }

            var lookupA = new Dictionary<IntPtr, (Mesh, ulong)>();
            List<APrimitive> geometriesToProcess = new List<APrimitive>();
            var nodesToProcess = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
                rootNodeOfModel,
                treeIndexGenerator,
                instanceIdGenerator,
                fbxImporter,
                lookupA).ToList();

            progressReport?.Report((Path.GetFileNameWithoutExtension(fbxFilename), ++progress, workload.Count));
            return nodesToProcess;
        }

        var fbxNodes = workload.SelectMany(LoadFbxFile).ToArray();

        var fbxNodesFlat = fbxNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();

        if (stringInternPool != null)
        {
            Console.WriteLine(
                $"{stringInternPool.Considered:N0} PDMS strings were deduped into {stringInternPool.Added:N0} string objects. Reduced string allocation by {(float)stringInternPool.Deduped / stringInternPool.Considered:P1}.");
        }

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