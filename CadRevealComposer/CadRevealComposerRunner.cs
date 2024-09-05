namespace CadRevealComposer;

using Configuration;
using Devtools;
using IdProviders;
using ModelFormatProvider;
using Operations;
using Operations.SectorSplitting;
using Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                ProcessPrimitives(cachedAPrimitives, outputDirectory, modelParameters, composerParameters);
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
        var treeIndexToPrioritizedSector = ProcessPrimitives(geometriesToProcessArray, outputDirectory, modelParameters, composerParameters);

        if (!exportHierarchyDatabaseTask.IsCompleted)
            Console.WriteLine("Waiting for hierarchy export to complete...");
        exportHierarchyDatabaseTask.Wait();

        WriteParametersToParamsFile(modelParameters, composerParameters, outputDirectory);

        // TODO Is this the best place to do this?
        var prioritizedSectorInsertionStopwatch = Stopwatch.StartNew();
        SceneCreator.AddPrioritizedSectorsToDatabase(treeIndexToPrioritizedSector, outputDirectory);
        Console.WriteLine($"Inserted prioritized sectors in db in {prioritizedSectorInsertionStopwatch.Elapsed}");

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
    }

    public static Dictionary<ulong, uint> ProcessPrimitives(
        APrimitive[] allPrimitives,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters
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

        var highlightSplitter = new HighlightSectorSplitter();

        var sectors = splitter.SplitIntoSectors(allPrimitives, 0).OrderBy(x => x.SectorId).ToArray();

        var prioritizedForHighlighting = allPrimitives.Where(x => x.Priority == 1).ToArray();

        var nextSectorId = sectors.Last().SectorId + 1;
        var highlightSectors = highlightSplitter
            .SplitIntoSectors(prioritizedForHighlighting, nextSectorId)
            .OrderBy(x => x.SectorId)
            .ToArray();

        // Remove redundant root and point to the original root
        var originalRootId = sectors.First().SectorId;
        var redundantRootId = highlightSectors.First().SectorId;
        highlightSectors = highlightSectors
            .Skip(1)
            .Select(sector =>
            {
                return sector.ParentSectorId == redundantRootId
                    ? (sector with { ParentSectorId = originalRootId })
                    : sector;
            })
            .ToArray();

        Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");

        var allSectors = sectors.Concat(highlightSectors).ToArray();

        stopwatch.Restart();
        SceneCreator.CreateSceneFile(
            allPrimitives,
            outputDirectory,
            modelParameters,
            maxTreeIndex,
            stopwatch,
            allSectors
        );
        Console.WriteLine($"Wrote scene file in {stopwatch.Elapsed}");
        stopwatch.Restart();

        return GetTreeIndexToSectorIdDict(highlightSectors);
    }

    private static Dictionary<ulong, uint> GetTreeIndexToSectorIdDict(InternalSector[] sectors)
    {
        var sectorIdToTreeIndex = new Dictionary<ulong, uint>();
        foreach (var sector in sectors)
        {
            var sectorId = sector.SectorId;
            foreach (var node in sector.Geometries)
            {
                sectorIdToTreeIndex.TryAdd(node.TreeIndex, sectorId);
            }
        }

        return sectorIdToTreeIndex;
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
