namespace CadRevealComposer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Configuration;
using Devtools;
using IdProviders;
using ModelFormatProvider;
using Operations;
using Operations.SectorSplitting;
using Primitives;
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
                SplitAndExportSectors(cachedAPrimitives, outputDirectory, modelParameters, composerParameters);
                Console.WriteLine(
                    $"Ran {nameof(SplitAndExportSectors)} using cache file {cacheFile} in {totalTimeElapsed.Elapsed}"
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

        ModelMetadata metadataFromAllFiles = new ModelMetadata(new Dictionary<string, string>());
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

            // Todo should this return a new list of cadrevealnodes instead of mutating the input?
            PrioritySplittingUtils.SetPriorityForHighlightSplittingWithMutation(cadRevealNodes);

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

        var treeIndexToPrioritizedSector = SplitAndExportSectors(
            geometriesToProcessArray,
            outputDirectory,
            modelParameters,
            composerParameters
        );

        if (!exportHierarchyDatabaseTask.IsCompleted)
            Console.WriteLine("Waiting for hierarchy export to complete...");
        exportHierarchyDatabaseTask.Wait();

        WriteParametersToParamsFile(modelParameters, composerParameters, outputDirectory);

        // TODO Is this the best place to do this?
        var prioritizedSectorInsertionStopwatch = Stopwatch.StartNew();
        SceneCreator.AddPrioritizedSectorsToDatabase(
            treeIndexToPrioritizedSector.TreeIndexToSectorIdMap,
            outputDirectory
        );
        Console.WriteLine($"Inserted prioritized sectors in db in {prioritizedSectorInsertionStopwatch.Elapsed}");

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
    }

    public record SplitAndExportResults(Dictionary<uint, uint> TreeIndexToSectorIdMap);

    public static SplitAndExportResults SplitAndExportSectors(
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

        const uint rootSectorId = 0;
        var sectorIdGenerator = new SequentialIdGenerator(firstIdReturned: rootSectorId);
        // First split into normal sectors for the entire model
        var normalSectors = splitter
            .SplitIntoSectors(allPrimitives, sectorIdGenerator)
            .OrderBy(x => x.SectorId)
            .ToArray();

        // Then split into prioritized sectors, these are loaded on demand based on metadata in the Hierarchy database
        var prioritySplitter = new PrioritySectorSplitter();
        var prioritizedPrimitives = allPrimitives.Where(x => x.Priority > 0).ToArray();
        var prioritizedSectors = prioritySplitter
            .SplitIntoSectors(prioritizedPrimitives, sectorIdGenerator)
            .OrderBy(x => x.SectorId)
            .ToArray();

        InternalSector[] remappedPrioritizedSectors = RemapPrioritizedSectorsRootSectorId(
            prioritizedSectors,
            rootSectorId
        );
        var allSectors = normalSectors.Concat(remappedPrioritizedSectors).OrderBy(x => x.SectorId).ToArray();

        Console.WriteLine(
            $"Split into {normalSectors.Length} sectors and {remappedPrioritizedSectors.Length} prioritized sectors in {stopwatch.Elapsed}"
        );

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

        return new SplitAndExportResults(TreeIndexToSectorIdMap: GetTreeIndexToSectorIdDict(prioritizedSectors));
    }

    /// <summary>
    /// Remap Root sector ids to the given input rootId for all sectors in input
    /// </summary>
    [Pure]
    private static InternalSector[] RemapPrioritizedSectorsRootSectorId(
        InternalSector[] prioritizedSectors,
        uint newRootId
    )
    {
        var redundantRootId = prioritizedSectors.Min(x => x.SectorId);
        var remappedPrioritizedSectors = prioritizedSectors
            .Where(x => x.SectorId != redundantRootId)
            .Select(sector =>
                sector.ParentSectorId == redundantRootId ? (sector with { ParentSectorId = newRootId }) : sector
            )
            .ToArray();
        return remappedPrioritizedSectors;
    }

    private static Dictionary<uint, uint> GetTreeIndexToSectorIdDict(InternalSector[] sectors)
    {
        var sectorIdToTreeIndex = new Dictionary<uint, uint>();
        foreach (var sector in sectors)
        {
            var sectorId = sector.SectorId;
            foreach (var geometry in sector.Geometries)
            {
                // TODO: Cant a TreeIndex be in many sectors? // NIH
                sectorIdToTreeIndex.TryAdd(geometry.TreeIndex, sectorId);
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
