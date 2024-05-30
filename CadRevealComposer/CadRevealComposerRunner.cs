namespace CadRevealComposer;

using Configuration;
using Devtools;
using IdProviders;
using Microsoft.Data.Sqlite;
using ModelFormatProvider;
using Operations;
using Operations.SectorSplitting;
using Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Threading.Tasks;
using Utils;

public static class CadRevealComposerRunner
{
    public static void Process(
        DirectoryInfo inputFolderPath,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        IReadOnlyList<IModelFormatProvider> modelFormatProviders
    )
    {
        var totalTimeElapsed = Stopwatch.StartNew();
        if (composerParameters.DevPrimitiveCacheFolder != null)
        {
            var primitiveCache = new DevPrimitiveCacheFolder(composerParameters.DevPrimitiveCacheFolder);
            primitiveCache.PrintStatsToConsole();
            var cacheFile = primitiveCache.GetCacheFileForInputDirectory(inputFolderPath);
            var cachedAPrimitives = primitiveCache.ReadPrimitiveCache(inputFolderPath);
            if (cachedAPrimitives != null)
            {
                Console.WriteLine("Using developer cache file: " + cacheFile);
                ProcessPrimitives(cachedAPrimitives, outputDirectory, modelParameters, composerParameters, null);
                Console.WriteLine(
                    $"Ran {nameof(ProcessPrimitives)} using cache file {cacheFile} in {totalTimeElapsed.Elapsed}"
                );
                return;
            }
            Console.WriteLine(
                "Did not find a Primitive Cache file for the current input folder. Processing as normal, and saving a new cache for next run."
            );
        }

        var nodesToExport = new List<CadRevealNode>();
        var geometriesToProcess = new List<APrimitive>();
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIdGenerator = new InstanceIdGenerator();

        var filtering = new NodeNameFiltering(composerParameters.NodeNameExcludeRegex);

        ModelMetadata metadataFromAllFiles = new ModelMetadata(new());
        foreach (IModelFormatProvider modelFormatProvider in modelFormatProviders)
        {
            var timer = Stopwatch.StartNew();
            (IReadOnlyList<CadRevealNode> cadRevealNodes, var generalMetadata) = modelFormatProvider.ParseFiles(
                inputFolderPath.EnumerateFiles(),
                treeIndexGenerator,
                instanceIdGenerator,
                filtering
            );

            if (generalMetadata != null)
            {
                // Log that we added some metadata
                Console.WriteLine("Adding an entry to model metadata");
                metadataFromAllFiles.Add(generalMetadata);
            }

            Console.WriteLine(
                $"Imported all files for {modelFormatProvider.GetType().Name} in {timer.Elapsed}. Got {cadRevealNodes.Count} nodes."
            );

            if (cadRevealNodes.Count <= 0)
            {
                continue;
            }

            // collect all nodes for later sector division of the entire scene
            nodesToExport.AddRange(cadRevealNodes);

            var inputGeometries = cadRevealNodes.AsParallel().AsOrdered().SelectMany(x => x.Geometries).ToArray();

            var geometriesIncludingMeshes = modelFormatProvider.ProcessGeometries(
                inputGeometries,
                composerParameters,
                modelParameters,
                instanceIdGenerator
            );
            geometriesToProcess.AddRange(geometriesIncludingMeshes);
        }


        var primitiveGroupsByTag = new HashSet<APrimitive[]>();

        var allNodesWithTag = new List<CadRevealNode>();
        foreach (var node in nodesToExport)
        {
            node.Attributes.TryGetValue("Tag", out string? tag);

            if (tag != null)
            {
                allNodesWithTag.Add(node);
            }
        }

        var primitivesGroupedByTreeIndexLookup = geometriesToProcess.ToLookup(x => x.TreeIndex);

        var allUsedNodes = new List<CadRevealNode>();
        foreach (var node in allNodesWithTag)
        {
            var nodeAndAllChildren = GetAllNodeAndChildrenFlat(node);
            var treeIndexes = nodeAndAllChildren.Select(x => x.TreeIndex).Distinct().ToArray();

            var primitivesInGroup = new List<APrimitive>();
            foreach (var treeIndex in treeIndexes)
            {
                primitivesInGroup.AddRange(primitivesGroupedByTreeIndexLookup[treeIndex]);
            }

            primitiveGroupsByTag.Add(primitivesInGroup.ToArray());
            allUsedNodes.AddRange(nodeAndAllChildren);
        }

        allUsedNodes = allUsedNodes.Distinct().ToList();
        var theRest = nodesToExport.Except(allUsedNodes);

        foreach (var node in theRest)
        {
            primitiveGroupsByTag.Add(primitivesGroupedByTreeIndexLookup[node.TreeIndex].ToArray());
        }

        // If there is no metadata for this model, the json will be empty
        Console.WriteLine("Exporting model metadata");
        SceneCreator.ExportModelMetadata(outputDirectory, metadataFromAllFiles);

        filtering.PrintFilteringStatsToConsole();

        var exportHierarchyDatabaseTask = Task.Run(() =>
        {
            // Exporting hierarchy on side thread to allow it to run in parallel
            var hierarchyExportTimer = Stopwatch.StartNew();
            var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
            SceneCreator.ExportHierarchyDatabase(databasePath, nodesToExport);
            Console.WriteLine(
                $"Exported hierarchy database to path \"{databasePath}\" in {hierarchyExportTimer.Elapsed}"
            );
        });

        geometriesToProcess = Simplify.OptimizeVertexCountInMeshes(geometriesToProcess);

        var geometriesToProcessArray = geometriesToProcess.ToArray();
        if (composerParameters.DevPrimitiveCacheFolder != null)
        {
            Console.WriteLine("Writing to DevCache!");
            var devCache = new DevPrimitiveCacheFolder(composerParameters.DevPrimitiveCacheFolder);
            devCache.WriteToPrimitiveCache(geometriesToProcessArray, inputFolderPath);
        }
        var treeIndexWithSector = ProcessPrimitives(
            geometriesToProcessArray,
            outputDirectory,
            modelParameters,
            composerParameters,
            primitiveGroupsByTag
        );

        if (!exportHierarchyDatabaseTask.IsCompleted)
            Console.WriteLine("Waiting for hierarchy export to complete...");
        exportHierarchyDatabaseTask.Wait();

        // Sector in metadata hack //////////////////
        var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
        using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText =
                "CREATE TABLE sectors (treeindex INTEGER NOT NULL, sectorId INTEGER NOT NULL, PRIMARY KEY (treeindex, sectorId)) WITHOUT ROWID; ";
            createTableCommand.ExecuteNonQuery();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO sectors (treeindex, sectorId) VALUES ($TreeIndex, $SectorId)";
            var treeIndexParameter = command.CreateParameter();
            treeIndexParameter.ParameterName = "$TreeIndex";
            var sectorIdParameter = command.CreateParameter();
            sectorIdParameter.ParameterName = "$SectorId";

            command.Parameters.AddRange([treeIndexParameter, sectorIdParameter]);

            var transaction = connection.BeginTransaction();
            command.Transaction = transaction;
            foreach (var pair in treeIndexWithSector)
            {
                treeIndexParameter.Value = pair.treeIndex;
                sectorIdParameter.Value = pair.sectorId;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        //////////////////////////////////////////////
        WriteParametersToParamsFile(modelParameters, composerParameters, outputDirectory);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
    }

    private static CadRevealNode[] GetAllNodeAndChildrenFlat(CadRevealNode node)
    {
        var allChildren = new List<CadRevealNode>();

        allChildren.Add(node);

        if (node.Children is { Length: > 0 })
        {
            foreach (var child in node.Children)
            {
                allChildren.AddRange(GetAllNodeAndChildrenFlat(child));
            }
        }

        return allChildren.ToArray();
    }

    public static (ulong treeIndex, uint sectorId)[] ProcessPrimitives(
        APrimitive[] allPrimitives,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        HashSet<APrimitive[]> primitiveGroupsByTag
    )
    {
        var maxTreeIndex = allPrimitives.Max(x => x.TreeIndex);

        var stopwatch = Stopwatch.StartNew();

        ISectorSplitter splitter;
        if (composerParameters.SingleSector)
        {
            splitter = new SectorSplitterSingle();
        }
        else if (composerParameters.SplitIntoZones)
        {
            throw new ArgumentException("SplitIntoZones is no longer supported. Use regular Octree splitting instead.");
        }
        else
        {
            splitter = new SectorSplitterOctree();
        }

        var sectors = splitter.SplitIntoSectors(allPrimitives, primitiveGroupsByTag).OrderBy(x => x.SectorId).ToArray();

        Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");

        stopwatch.Restart();
        SceneCreator.CreateSceneFile(allPrimitives, outputDirectory, modelParameters, maxTreeIndex, stopwatch, sectors);
        Console.WriteLine($"Wrote scene file in {stopwatch.Elapsed}");
        stopwatch.Restart();

        var treeIndexSectorIdList = new List<(ulong, uint)>();
        foreach (var sector in sectors)
        {
            var sectorId = sector.SectorId;
            foreach (var node in sector.Geometries)
            {
                treeIndexSectorIdList.Add((node.TreeIndex, sectorId));
            }
        }

        return treeIndexSectorIdList.ToArray();
    }

    /// <summary>
    /// Writes the input parameters to a file to easier replicate a run.
    /// </summary>
    private static void WriteParametersToParamsFile(
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        DirectoryInfo outputDirectory
    )
    {
        var json = new
        {
            note = "This file is not considered stable api. It is meant for humans to read, not computers. See 'scene.json' for a more stable file.",
            modelParameters,
            composerParameters,
            timestampUtc = DateTimeOffset.UtcNow
        };

        File.WriteAllText(
            Path.Join(outputDirectory.FullName, "params.json"),
            JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true })
        );
    }
}
