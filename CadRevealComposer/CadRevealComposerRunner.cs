namespace CadRevealComposer;

using CadRevealFbxProvider.BatchUtils;
using Configuration;
using IdProviders;
using ModelFormatProvider;
using Operations;
using Operations.SectorSplitting;
using Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessellation;
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

        var nodesToExport = new List<CadRevealNode>();
        var geometriesToProcess = new List<APrimitive>();
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIdGenerator = new InstanceIdGenerator();

        var filtering = new NodeNameFiltering(composerParameters.NodeNameExcludeRegex);
        var nodePriorityFiltering = new PriorityMapping(
            composerParameters.PrioritizedDisciplinesRegex,
            composerParameters.PrioritizedNodeNamesRegex
        );

        foreach (IModelFormatProvider modelFormatProvider in modelFormatProviders)
        {
            var timer = Stopwatch.StartNew();
            IReadOnlyList<CadRevealNode> cadRevealNodes = modelFormatProvider.ParseFiles(
                inputFolderPath.EnumerateFiles(),
                treeIndexGenerator,
                instanceIdGenerator,
                filtering,
                nodePriorityFiltering
            );

            Console.WriteLine(
                $"Imported all files for {modelFormatProvider.GetType().Name} in {timer.Elapsed}. Got {cadRevealNodes.Count} nodes."
            );

            if (cadRevealNodes.Count > 0)
            {
                // collect all nodes for later sector division of the entire scene
                nodesToExport.AddRange(cadRevealNodes);

                var inputGeometries = cadRevealNodes
                    .AsParallel()
                    .AsOrdered()
                    .SelectMany(node => node.Geometries)
                    .ToArray();

                var geometriesIncludingMeshes = modelFormatProvider.ProcessGeometries(
                    inputGeometries,
                    composerParameters,
                    modelParameters,
                    instanceIdGenerator
                );
                geometriesToProcess.AddRange(geometriesIncludingMeshes);
            }
        }

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

        geometriesToProcess = OptimizeVertexCountInMeshes(geometriesToProcess);

        ProcessPrimitives(
            geometriesToProcess.ToArray(),
            outputDirectory,
            modelParameters,
            composerParameters,
            treeIndexGenerator
        );

        if (!exportHierarchyDatabaseTask.IsCompleted)
            Console.WriteLine("Waiting for hierarchy export to complete...");
        exportHierarchyDatabaseTask.Wait();

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
    }

    public static void ProcessPrimitives(
        APrimitive[] allPrimitives,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        TreeIndexGenerator treeIndexGenerator
    )
    {
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

        var sectorInfos = sectors.Select(s => SerializeSector(s, outputDirectory.FullName)).ToArray();

        Console.WriteLine($"Serialized {sectors.Length} sectors in {stopwatch.Elapsed}");
        stopwatch.Restart();

        var sectorsWithDownloadSize = CalculateDownloadSizes(sectorInfos, outputDirectory).ToImmutableArray();

        PrintSectorStats(sectorsWithDownloadSize);

        var cameraPosition = CameraPositioning.CalculateInitialCamera(allPrimitives);
        SceneCreator.WriteSceneFile(
            sectorsWithDownloadSize,
            modelParameters,
            outputDirectory,
            treeIndexGenerator.CurrentMaxGeneratedIndex,
            cameraPosition
        );

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

    private static IEnumerable<SceneCreator.SectorInfo> CalculateDownloadSizes(
        IEnumerable<SceneCreator.SectorInfo> sectors,
        DirectoryInfo outputDirectory
    )
    {
        foreach (var sector in sectors)
        {
            if (string.IsNullOrEmpty(sector.Filename))
            {
                yield return sector;
            }
            else
            {
                var filepath = Path.Combine(outputDirectory.FullName, sector.Filename);
                yield return sector with
                {
                    DownloadSize = new FileInfo(filepath).Length
                };
            }
        }
    }

    private static List<APrimitive> OptimizeVertexCountInMeshes(IEnumerable<APrimitive> geometriesToProcess)
    {
        var meshCount = 0;
        var beforeOptimizationTotalVertices = 0;
        var afterOptimizationTotalVertices = 0;
        var timer = Stopwatch.StartNew();
        // Optimize TriangleMesh meshes for least memory use
        var processedGeometries = geometriesToProcess
            .AsParallel()
            .AsOrdered()
            .Select(primitive =>
            {
                if (primitive is not TriangleMesh triangleMesh)
                {
                    return primitive;
                }

                Mesh newMesh = MeshTools.DeduplicateVertices(triangleMesh.Mesh);
                Interlocked.Increment(ref meshCount);
                Interlocked.Add(ref beforeOptimizationTotalVertices, triangleMesh.Mesh.Vertices.Length);
                Interlocked.Add(ref afterOptimizationTotalVertices, newMesh.Vertices.Length);
                return triangleMesh with { Mesh = newMesh };
            })
            .ToList();

        using (new TeamCityLogBlock("Vertex Dedupe Stats"))
        {
            Console.WriteLine(
                $"Vertice Dedupe Stats (Vertex Count) for {meshCount} meshes:\nBefore: {beforeOptimizationTotalVertices, 11}\nAfter:  {afterOptimizationTotalVertices, 11}\nPercent: {(float)afterOptimizationTotalVertices / beforeOptimizationTotalVertices, 11:P2}\nTime: {timer.Elapsed}"
            );
        }

        return processedGeometries;
    }
}
