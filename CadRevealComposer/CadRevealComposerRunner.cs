namespace CadRevealComposer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
        var nodePriorityFiltering = new PriorityMapping(
            composerParameters.PrioritizedDisciplinesRegex,
            composerParameters.LowPrioritizedDisciplineRegex,
            composerParameters.PrioritizedNodeNamesRegex
        );

        ModelMetadata metadataFromAllFiles = new ModelMetadata(new());
        foreach (IModelFormatProvider modelFormatProvider in modelFormatProviders)
        {
            var timer = Stopwatch.StartNew();
            (IReadOnlyList<CadRevealNode> cadRevealNodes, var generalMetadata) = modelFormatProvider.ParseFiles(
                inputFolderPath.EnumerateFiles(),
                treeIndexGenerator,
                instanceIdGenerator,
                filtering,
                nodePriorityFiltering
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
        ProcessPrimitives(geometriesToProcessArray, outputDirectory, modelParameters, composerParameters);

        if (!exportHierarchyDatabaseTask.IsCompleted)
            Console.WriteLine("Waiting for hierarchy export to complete...");
        exportHierarchyDatabaseTask.Wait();

        WriteParametersToParamsFile(modelParameters, composerParameters, outputDirectory);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
    }

    public static void ProcessPrimitives(
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

        var sectors = splitter.SplitIntoSectors(allPrimitives).OrderBy(x => x.SectorId).ToArray();

        Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");

        stopwatch.Restart();
        SceneCreator.CreateSceneFile(allPrimitives, outputDirectory, modelParameters, maxTreeIndex, stopwatch, sectors);
        Console.WriteLine($"Wrote scene file in {stopwatch.Elapsed}");
        stopwatch.Restart();
    }

    private static void PrintSectorStats(ImmutableArray<SceneCreator.SectorInfo> sectorsWithDownloadSize)
    {
        // Helpers
        static float BytesToMegabytes(long bytes) => bytes / 1024f / 1024f;

        (string, string, string, string, string, string, string, string, string, string) headers = (
            "Depth",
            "Sectors",
            "μ drawCalls",
            "μ Triangles",
            "μ sectDiam",
            "^ sectDiam",
            "v sectDiam",
            "μ s/l part",
            "μ DLsize",
            "v DLsize"
        );
        // Add stuff you would like for a quick overview here:
        using (new TeamCityLogBlock("Sector Stats"))
        {
            Console.WriteLine($"Sector Count: {sectorsWithDownloadSize.Length}");
            Console.WriteLine(
                $"Sum all sectors .glb size megabytes: {BytesToMegabytes(sectorsWithDownloadSize.Sum(x => x.DownloadSize)):F2}MB"
            );
            Console.WriteLine(
                $"Total Estimated Triangle Count: {sectorsWithDownloadSize.Sum(x => x.EstimatedTriangleCount)}"
            );
            Console.WriteLine($"Depth Stats:");
            Console.WriteLine(
                $"|{headers.Item1, 5}|{headers.Item2, 7}|{headers.Item3, 10}|{headers.Item4, 11}|{headers.Item5, 10}|{headers.Item6, 10}|{headers.Item7, 10}|{headers.Item8, 17}|{headers.Item9, 10}|{headers.Item10, 8}|"
            );
            Console.WriteLine(new String('-', 110));
            foreach (
                IGrouping<long, SceneCreator.SectorInfo> g in sectorsWithDownloadSize
                    .GroupBy(x => x.Depth)
                    .OrderBy(x => x.Key)
            )
            {
                var anyHasGeometry = g.Any(x => x.Geometries.Any());
                var sizeMinAvgExceptEmpty = anyHasGeometry
                    ? g.Where(x => x.Geometries.Any()).Average(x => x.MinNodeDiagonal)
                    : 0;
                var sizeMaxAvgExceptEmpty = anyHasGeometry
                    ? g.Where(x => x.Geometries.Any()).Average(x => x.MaxNodeDiagonal)
                    : 0;
                var maxSize = "N/A";
                if (g.Count() > 1)
                {
                    maxSize = $"{BytesToMegabytes(g.Max(x => x.DownloadSize)):F2}";
                }

                var formatted = $@"|
{g.Key, 5}|
{g.Count(), 7}|
{g.Average(x => x.EstimatedDrawCalls), 11:F2}|
{g.Average(x => x.EstimatedTriangleCount), 11:F0}|
{g.Average(x => x.SubtreeBoundingBox.Diagonal), 9:F2}m|
{g.Min(x => x.SubtreeBoundingBox.Diagonal), 9:F2}m|
{g.Max(x => x.SubtreeBoundingBox.Diagonal), 9:F2}m|
{sizeMinAvgExceptEmpty, 7:F2}m/{sizeMaxAvgExceptEmpty, 7:F2}m|
{g.Average(x => x.DownloadSize / 1024f / 1024f), 8:F}MB|
{maxSize, 8}|
".Replace(Environment.NewLine, "");
                Console.WriteLine(formatted);
            }
        }
    }

    private static SceneCreator.SectorInfo SerializeSector(InternalSector p, string outputDirectory)
    {
        var sectorFilename = p.Geometries.Any() ? $"sector_{p.SectorId}.glb" : null;

        if (p.Prioritized && sectorFilename != null)
        {
            sectorFilename = $"pri_{sectorFilename}";
        }

        var (estimatedTriangleCount, estimatedDrawCalls) = DrawCallEstimator.Estimate(p.Geometries);

        var sectorInfo = new SceneCreator.SectorInfo(
            SectorId: p.SectorId,
            ParentSectorId: p.ParentSectorId,
            Depth: p.Depth,
            Path: p.Path,
            Filename: sectorFilename,
            EstimatedTriangleCount: estimatedTriangleCount,
            EstimatedDrawCalls: estimatedDrawCalls,
            MinNodeDiagonal: p.MinNodeDiagonal,
            MaxNodeDiagonal: p.MaxNodeDiagonal,
            Geometries: p.Geometries,
            SubtreeBoundingBox: p.SubtreeBoundingBox,
            GeometryBoundingBox: p.GeometryBoundingBox
        );

        if (sectorFilename != null)
            SceneCreator.ExportSectorGeometries(sectorInfo.Geometries, sectorFilename, outputDirectory);
        return sectorInfo;
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
