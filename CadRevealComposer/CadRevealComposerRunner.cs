namespace CadRevealComposer;

using Operations.SectorSplitting;
using CadRevealFbxProvider.BatchUtils;
using Configuration;
using IdProviders;
using ModelFormatProvider;
using Operations;
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
        IReadOnlyList<IModelFormatProvider> modelFormatProviders)
    {
        var totalTimeElapsed = Stopwatch.StartNew();

        List<CadRevealNode> nodesToProcess = new List<CadRevealNode>();
        List<APrimitive> geometriesToProcess = new List<APrimitive>();
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIdGenerator = new InstanceIdGenerator();

        foreach (IModelFormatProvider modelFormatProvider in modelFormatProviders)
        {
            var timer = Stopwatch.StartNew();
            IReadOnlyList<CadRevealNode> cadRevealNodes =
                modelFormatProvider.ParseFiles(inputFolderPath.EnumerateFiles(), treeIndexGenerator,
                    instanceIdGenerator);
            if (cadRevealNodes != null)
            {
                Console.WriteLine(
                    $"Imported all files for {modelFormatProvider.GetType().Name} in {timer.Elapsed}. Got {cadRevealNodes.Count} nodes.");

                if (cadRevealNodes.Count > 0)
                {
                    // collect all nodes for later sector division of the entire scene
                    nodesToProcess.AddRange(cadRevealNodes);

                    var inputGeometries = cadRevealNodes
                        .AsParallel()
                        .AsOrdered()
                        .SelectMany(x => x.Geometries)
                        .ToArray();

                    var geometriesIncludingMeshes = modelFormatProvider.ProcessGeometries(
                        inputGeometries,
                        composerParameters,
                        modelParameters,
                        instanceIdGenerator);
                    geometriesToProcess.AddRange(geometriesIncludingMeshes);
                }
            }
        }

        var exportHierarchyDatabaseTask = Task.Run(() =>
        {
            // Exporting hierarchy on side thread to allow it to run in parallel
            var hierarchyExportTimer = Stopwatch.StartNew();
            var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
            SceneCreator.ExportHierarchyDatabase(databasePath, nodesToProcess);
            Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\" in {hierarchyExportTimer.Elapsed}");
        });

        geometriesToProcess = OptimizeVertexCountInMeshes(geometriesToProcess);

        ProcessPrimitives(geometriesToProcess.ToArray(), outputDirectory, modelParameters, composerParameters,
            treeIndexGenerator);

        if (!exportHierarchyDatabaseTask.IsCompleted) Console.WriteLine("Waiting for hierarchy export to complete...");
        Task.WaitAll(exportHierarchyDatabaseTask);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
    }

    public static void ProcessPrimitives(APrimitive[] allPrimitives, DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        TreeIndexGenerator treeIndexGenerator)
    {
        var stopwatch = Stopwatch.StartNew();

        ISectorSplitter splitter;
        if (composerParameters.SingleSector)
        {
            splitter = new SectorSplitterSingle();
        }
        else if (composerParameters.SplitIntoZones)
        {
            splitter = new SectorSplitterZones(outputDirectory);
        }
        else
        {
            splitter = new SectorSplitterOctree();
        }

        var sectors = splitter
            .SplitIntoSectors(allPrimitives)
            .OrderBy(x => x.SectorId).ToArray();

        Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");
        stopwatch.Restart();

        var sectorInfos = sectors
            .Select(s => SerializeSector(s, outputDirectory.FullName))
            .ToArray();

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
            cameraPosition);

        Console.WriteLine($"Wrote scene file in {stopwatch.Elapsed}");
        stopwatch.Restart();
    }

    private static void PrintSectorStats(ImmutableArray<SceneCreator.SectorInfo> sectorsWithDownloadSize)
    {
        // Helpers
        float BytesToMegabytes(long bytes) => bytes / 1024f / 1024f;

        // Add stuff you would like for a quick overview here:
        using (new TeamCityLogBlock("Sector Stats"))
        {
            Console.WriteLine($"Sector Count: {sectorsWithDownloadSize.Length}");
            Console.WriteLine($"Sum all sectors .glb size megabytes: {BytesToMegabytes(sectorsWithDownloadSize.Sum(x => x.DownloadSize)):F2}MB");
            Console.WriteLine($"Total Estimated Triangle Count: {sectorsWithDownloadSize.Sum(x => x.EstimatedTriangleCount)}");
            Console.WriteLine($"Depth Stats:");
            foreach (IGrouping<long,SceneCreator.SectorInfo> g in sectorsWithDownloadSize.GroupBy(x => x.Depth).OrderBy(x => x.Key))
            {
                Console.WriteLine($"\t{g.Key,2}: Sectors: {g.Count(),4}, Avg DrawCalls: {g.Average(x => x.EstimatedDrawCalls),7:F2}, Avg Triangles: {g.Average(x => x.EstimatedTriangleCount),10:F0}, Avg Download Size: {g.Average(x => x.DownloadSize / 1024f/1024f),6:F}MB");
                if(g.Count() > 1)
                {
                    Console.WriteLine($"\t\tMax Download Size :{BytesToMegabytes(g.Max(x => x.DownloadSize)):F2}.");
                }
            }

        }
    }

    private static SceneCreator.SectorInfo SerializeSector(ProtoSector p, string outputDirectory)
    {
        var estimateDrawCalls = DrawCallEstimator.Estimate(p.Geometries);

        var sectorFilename = p.Geometries.Any() ? $"sector_{p.SectorId}.glb" : null;
        var sectorInfo = new SceneCreator.SectorInfo(
            SectorId: p.SectorId,
            ParentSectorId: p.ParentSectorId,
            Depth: p.Depth,
            Path: p.Path,
            Filename: sectorFilename,
            EstimatedTriangleCount: estimateDrawCalls.EstimatedTriangleCount,
            EstimatedDrawCalls: estimateDrawCalls.EstimatedDrawCalls,
            Geometries: p.Geometries,
            SubtreeBoundingBox: p.SubtreeBoundingBox,
            GeometryBoundingBox: p.GeometryBoundingBox
        );

        if (sectorFilename != null)
            SceneCreator.ExportSectorGeometries(sectorInfo.Geometries, sectorFilename, outputDirectory);
        return sectorInfo;
    }

    private static IEnumerable<SceneCreator.SectorInfo> CalculateDownloadSizes(
        IEnumerable<SceneCreator.SectorInfo> sectors, DirectoryInfo outputDirectory)
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
                yield return sector with { DownloadSize = new FileInfo(filepath).Length };
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
        var processedGeometries = geometriesToProcess.AsParallel().AsOrdered().Select(primitive =>
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
        }).ToList();

        using (new TeamCityLogBlock("Vertex Dedupe Stats"))
        {
            Console.WriteLine(
                $"Vertice Dedupe Stats (Vertex Count) for {meshCount} meshes:\nBefore: {beforeOptimizationTotalVertices,11}\nAfter:  {afterOptimizationTotalVertices,11}\nPercent: {(float)afterOptimizationTotalVertices / beforeOptimizationTotalVertices,11:P2}\nTime: {timer.Elapsed}");
        }

        return processedGeometries;
    }
}