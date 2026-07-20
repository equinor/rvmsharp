namespace CadRevealFbxProvider.BatchUtils;

using System.Text.RegularExpressions;
using Attributes;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Utils;
using Commons;
using UserFriendlyLogger;

public static class FbxWorkload
{
    public static (string fbxFilename, string? attributeFilename, string? stidMetadataFilename)[] CollectWorkload(
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
                    || x.Equals(".json", StringComparison.OrdinalIgnoreCase)
                )
            ) // Append single files
            .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
            .GroupBy(Path.GetFileNameWithoutExtension)
            .ToArray(); // Group by filename (rvm, txt)

        var workload = (
            from fileTouple in inputFiles
            select fileTouple.ToArray() into fileToupleStatic
            let fbxFilename = fileToupleStatic.FirstOrDefault(f => f.ToLower().EndsWith(".fbx"))
            let csvFilename = fileToupleStatic.FirstOrDefault(f => f.ToLower().EndsWith(".csv"))
            let jsonFilename = fileToupleStatic.FirstOrDefault(f => f.ToLower().EndsWith(".json"))
            select (fbxFilename, csvFilename, jsonFilename)
        ).ToArray();

        var result = new List<(string, string?, string?)>();
        foreach ((string? fbxFilename, string? attributeFilename, string? stidMetadataFilename) in workload)
        {
            if (fbxFilename == null && attributeFilename == null && stidMetadataFilename == null)
                continue; // Nothing found

            if (fbxFilename == null)
            {
                Console.WriteLine(
                    $"No corresponding FBX file found for attributes: '{attributeFilename}', the file will be skipped."
                );
            }
            else
                result.Add((fbxFilename, attributeFilename, stidMetadataFilename));
        }

        return result.ToArray();
    }

    public static (IReadOnlyList<CadRevealNode>, ModelMetadata?) ReadFbxData(
        IReadOnlyCollection<(string fbxFilename, string? txtFilename, string? stidMetadataFilename)> workload,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering,
        IProgress<(string fileName, int progress, int total)>? progressReport = null,
        IStringInternPool? stringInternPool = null
    )
    {
        var progress = 0;

        // FBX Importer is intentionally not disposed as the dispose operation takes too long. If this becomes a problem with memory leaks we need to investigate why the dispose is so slow.
        var fbxImporter = new FbxImporter();
        if (!fbxImporter.HasValidSdk())
        {
            Console.WriteLine("Did not find valid SDK, cannot import FBX file.");
            throw new UserFriendlyLogException(
                "FBX import failed due to an invalid import library, please notify the developing team.",
                new Exception("FBX import failed due to outdated FBX SDK! Scene would be invalid, hence exiting.")
            );
        }

        Dictionary<string, string> metadata = new();

        // the local function LoadFbxFile modifies model's metadata as well
        var fbxNodesFlat = workload.SelectMany(LoadFbxFile).ToArray();

        if (stringInternPool != null)
        {
            Console.WriteLine(
                $"{stringInternPool.Considered:N0} PDMS strings were deduped into {stringInternPool.Added:N0} string objects. Reduced string allocation by {(float)stringInternPool.Deduped / stringInternPool.Considered:P1}."
            );
        }

        return (fbxNodesFlat, new ModelMetadata(metadata));

        IReadOnlyList<CadRevealNode> LoadFbxFile(
            (string fbxFilename, string? attributeFilename, string? stidMetadataFilename) fileTupple
        )
        {
            (string fbxFilename, string? infoTextFilename, string? stidMetadataFilename) = fileTupple;

            Dictionary<string, Dictionary<string, string>?>? attributes = null;
            // there could be an explicit test / determination if this current fbx is scaffolding or not
            if (infoTextFilename != null)
            {
                var lines = File.ReadAllLines(infoTextFilename);

                // default value for temp, because we have two different ways of determining it
                // old pipeline: from the filename
                // new pipleine: from stid metadata
                var isTemp = false;

                var isNewPipeline = (stidMetadataFilename != null);

                // old pipeline, we perform checking of the scaffolding filename here, because we will later deduce some metadata from it
                if (!isNewPipeline)
                {
                    var fileNameonly = Path.GetFileNameWithoutExtension(infoTextFilename);

                    //check the length of the filename. Upload artifacts will fail with too long filenames
                    if (fileNameonly.Length > 55)
                    {
                        throw new UserFriendlyLogException(
                            $"Scaffolding file's {fileNameonly} filename has {fileNameonly.Length} characters and is exceeding the maximum allowed length of 55 characters. Check the naming guide.",
                            new ScaffoldingFilenameException($"Filename too long: {fileNameonly}")
                        );
                    }

                    isTemp = fileNameonly.Contains("TEMP", StringComparison.OrdinalIgnoreCase);

                    (attributes, var scaffoldingMetadata) = ScaffoldingAttributeParser.ParseAttributes(lines, isTemp);

                    if (!isTemp)
                    {
                        // check if the WO from filename actually matches the metadata
                        // check if the filename is complying with the guidlines
                        // for non-temp scaffs only
                        // crashes if there is a mismatch
                        scaffoldingMetadata.ThrowIfFilenameInvalid(fileNameonly);
                    }
                    scaffoldingMetadata.GetSuffixFromFilename(fileNameonly);
                    // We crash if we dont have expected values
                    scaffoldingMetadata.TryWriteToGenericMetadataDict(metadata);
                }
                else
                {
                    // parse stid-metadata.json to determine if the scaffolding has status temp or not
                    //
                    throw new UserFriendlyLogException($"New STID metadata parsing not implemented yet.");
                }
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
                return [];

            if (rootNodeConverted.BoundingBoxAxisAligned == null)
            {
                throw new UserFriendlyLogException(
                    $"The FBX file {Path.GetFileName(fbxFilename)} has no bounding box. This may indicate that the model is empty (without geometry) or invalid. Verify the FBX export.",
                    new FbxGeometryException(
                        "The FBX file seems to have no nodes containing geometry as the bounding box of the model is null. Nodes without geometry might exist."
                    )
                );
            }

            var flatNodes = CadRevealNode.GetAllNodesFlat(rootNodeConverted).ToArray();

            // attach attribute info to the nodes if there is any
            if (attributes != null)
            {
                var requestNewInstanceId = new Func<ulong>(() => instanceIdGenerator.GetNextId());
                var optimizer = new ScaffoldOptimizer.ScaffoldOptimizer();
                var statisticsBefore = new GeometryDistributionNodeStats(flatNodes.ToList());
                optimizer.OptimizeNodes(flatNodes.ToList(), requestNewInstanceId);
                var statisticsAfter = new GeometryDistributionNodeStats(flatNodes.ToList());
                var diffStatistics = new GeometryDistributionNodeStatsDiff(statisticsBefore, statisticsAfter);
                statisticsBefore.PrintStatistics("scaffolds before optimization");
                statisticsAfter.PrintStatistics("scaffolds after optimization");
                diffStatistics.PrintStatistics("improvement of scaffolds geometry");

                bool totalMismatch = true;
                var fbxNameIdRegex = new Regex(@"\[(\d+)\]");
                foreach (CadRevealNode cadRevealNode in flatNodes)
                {
                    var match = fbxNameIdRegex.Match(cadRevealNode.Name);
                    if (match.Success)
                    {
                        var id = match.Groups[1].Value;

                        if (attributes.ContainsKey(id))
                        {
                            totalMismatch = false;
                            var attributesId = attributes[id];
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
                    throw new UserFriendlyLogException(
                        $"No item in the attribute file {Path.GetFileName(infoTextFilename)} can be matched with its geometry in the FBX model {Path.GetFileName(fbxFilename)} based on the Item Code. Either the CSV and FBX do not belong together, or all rows in the CSV are invalid. Check if your CSV matches the CSV-template and if the required metadata are actually exported."
                    );
            }

            progressReport?.Report((Path.GetFileNameWithoutExtension(fbxFilename), ++progress, workload.Count));
            return flatNodes;
        }
    }
}
