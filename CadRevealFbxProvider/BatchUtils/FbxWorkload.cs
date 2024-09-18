namespace CadRevealFbxProvider.BatchUtils;

using System.Text.RegularExpressions;
using Attributes;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Utils;
using Commons;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using static CadRevealComposer.Operations.CameraPositioning;

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
                files.Where(x =>
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

    public static (IReadOnlyList<CadRevealNode>, ModelMetadata?) ReadFbxData(
        IReadOnlyCollection<(string fbxFilename, string? txtFilename)> workload,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering,
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

        Dictionary<string, string> metadata = new();

        IReadOnlyList<CadRevealNode> LoadFbxFile((string fbxFilename, string? attributeFilename) filePair)
        {
            (string fbxFilename, string? infoTextFilename) = filePair;

            Dictionary<string, Dictionary<string, string>?>? attributes = null;
            // there could be an explicit test / determination if this current fbx is scaffolding or not
            if (infoTextFilename != null)
            {
                var lines = File.ReadAllLines(infoTextFilename);
                (attributes, var scaffoldingMetadata) = new ScaffoldingAttributeParser().ParseAttributes(lines);
                // TODO: Should we crash if we dont have expected values?

                scaffoldingMetadata.TryWriteToGenericMetadataDict(metadata);
            }

            var rootNodeOfModel = fbxImporter.LoadFile(fbxFilename);
            var rootNodeConverted = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
                rootNodeOfModel,
                treeIndexGenerator,
                instanceIdGenerator,
                nodeNameFiltering,
                attributes
            );

            if (rootNodeConverted == null)
                return Array.Empty<CadRevealNode>();

            var flatNodes = CadRevealNode.GetAllNodesFlat(rootNodeConverted).ToArray();

            // attach attribute info to the nodes if there is any
            if (attributes != null)
            {
                bool totalMismatch = true;
                var fbxNameIdRegex = new Regex(@"\[(\d+)\]");
                foreach (CadRevealNode cadRevealNode in flatNodes)
                {
                    var match = fbxNameIdRegex.Match(cadRevealNode.Name);
                    if (match.Success)
                    {
                        var id = match.Groups[1].Value;

                        if (attributes.TryGetValue(id, out Dictionary<string, string>? attributesId))
                        {
                            totalMismatch = false;
                            if (attributesId != null)
                            {
                                foreach (var kvp in attributesId)
                                {
                                    cadRevealNode.Attributes.Add(kvp.Key, kvp.Value);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Data Id {id} has missing attributes.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Data Id {id} does not exist in the attribute file.");
                        }
                    }
                }

                if (totalMismatch)
                    throw new Exception(
                        $"FBX model {fbxFilename} and its attribute file {infoTextFilename} completely mismatch."
                    );
            }

            progressReport?.Report((Path.GetFileNameWithoutExtension(fbxFilename), ++progress, workload.Count));
            return flatNodes;
        }

        var fbxNodesFlat = workload.SelectMany(LoadFbxFile).ToArray();

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
        return (fbxNodesFlat, new ModelMetadata(metadata));
    }
}
