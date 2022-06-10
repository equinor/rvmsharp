namespace CadRevealComposer;

using Ben.Collections.Specialized;
using Configuration;
using IdProviders;
using Operations;
using Primitives;
using RvmSharp.BatchUtils;
using RvmSharp.Containers;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Utils;

public static class CadRevealComposerRunner
{
    public static void Process(
        DirectoryInfo inputRvmFolderPath,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters)
    {
        var workload = Workload.CollectWorkload(new[] { inputRvmFolderPath.FullName });

        Console.WriteLine("Reading RvmData");
        var rvmTimer = Stopwatch.StartNew();
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });
        var stringInternPool = new BenStringInternPool(new SharedInternPool());
        var rvmStore = Workload.ReadRvmData(workload, progressReport, stringInternPool);
        var fileSizesTotal = workload.Sum(w => new FileInfo(w.rvmFilename).Length);
        Console.WriteLine(
            $"Read RvmData in {rvmTimer.Elapsed}. (~{fileSizesTotal / 1024 / 1024}mb of .rvm files (excluding .txt file size))");

        ProcessRvmStore(rvmStore, outputDirectory, modelParameters, composerParameters);
    }

    public static void ProcessRvmStore(
        RvmStore rvmStore,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters)
    {
        TreeIndexGenerator treeIndexGenerator = new();
        NodeIdProvider nodeIdGenerator = new();

        Console.WriteLine("Generating i3d");

        var total = Stopwatch.StartNew();
        var stopwatch = Stopwatch.StartNew();
        var allNodes = RvmStoreToCadRevealNodesConverter.RvmStoreToCadRevealNodes(rvmStore, nodeIdGenerator, treeIndexGenerator);
        Console.WriteLine($"Converted to reveal nodes in {stopwatch.Elapsed}");
        stopwatch.Restart();

        var exportHierarchyDatabaseTask = Task.Run(() =>
        {
            var databaseExportTimer = Stopwatch.StartNew();
            var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
            SceneCreator.ExportHierarchyDatabase(databasePath, allNodes);
            Console.WriteLine(
                $"Exported hierarchy database to path \"{databasePath}\" in {databaseExportTimer.Elapsed}");
        });

        var geometries = allNodes
            .AsParallel()
            .AsOrdered()
            .SelectMany(x => x.RvmGeometries.SelectMany(
                primitive => APrimitive.FromRvmPrimitive(x, x.Group as RvmNode ?? throw new InvalidOperationException(), primitive)))
            .ToArray();

        Console.WriteLine($"Primitives converted in {stopwatch.Elapsed}");
        stopwatch.Restart();

        // var facetGroupsWithEmbeddedProtoMeshes = geometries
        //     .OfType<ProtoMeshFromFacetGroup>()
        //     .Select(p => new RvmFacetGroupWithProtoMesh(p, p.FacetGroup.Version, p.FacetGroup.Matrix,
        //         p.FacetGroup.BoundingBoxLocal, p.FacetGroup.Polygons))
        //     .Cast<RvmFacetGroup>()
        //     .ToArray();
        //
        // RvmFacetGroupMatcher.Result[] facetGroupInstancingResult;
        // if (composerParameters.NoInstancing)
        // {
        //     facetGroupInstancingResult = facetGroupsWithEmbeddedProtoMeshes
        //         .Select(x => new RvmFacetGroupMatcher.NotInstancedResult(x))
        //         .Cast<RvmFacetGroupMatcher.Result>()
        //         .ToArray();
        //     Console.WriteLine("Facet group instancing disabled.");
        // }
        // else
        // {
        //     facetGroupInstancingResult = RvmFacetGroupMatcher.MatchAll(
        //         facetGroupsWithEmbeddedProtoMeshes,
        //         facetGroups => facetGroups.Length >= modelParameters.InstancingThreshold.Value);
        //     Console.WriteLine($"Facet groups instance matched in {stopwatch.Elapsed}");
        //     stopwatch.Restart();
        // }
        //
        // var protoMeshesFromPyramids = geometries.OfType<ProtoMeshFromPyramid>().ToArray();
        // // We have models where several pyramids on the same "part" are completely identical.
        // var uniqueProtoMeshesFromPyramid = protoMeshesFromPyramids.Distinct().ToArray();
        // if (uniqueProtoMeshesFromPyramid.Length < protoMeshesFromPyramids.Length)
        // {
        //     var diffCount = protoMeshesFromPyramids.Length - uniqueProtoMeshesFromPyramid.Length;
        //     Console.WriteLine(
        //         $"Found and ignored {diffCount} duplicate pyramids (including: position, mesh, parent, id, etc).");
        // }
        //
        // RvmPyramidInstancer.Result[] pyramidInstancingResult;
        // if (composerParameters.NoInstancing)
        // {
        //     pyramidInstancingResult = uniqueProtoMeshesFromPyramid
        //         .Select(x => new RvmPyramidInstancer.NotInstancedResult(x))
        //         .OfType<RvmPyramidInstancer.Result>()
        //         .ToArray();
        //     Console.WriteLine("Pyramid instancing disabled.");
        // }
        // else
        // {
        //     pyramidInstancingResult = RvmPyramidInstancer.Process(
        //         uniqueProtoMeshesFromPyramid,
        //         pyramids => pyramids.Length >= modelParameters.InstancingThreshold.Value);
        //     Console.WriteLine($"Pyramids instance matched in {stopwatch.Elapsed}");
        //     stopwatch.Restart();
        // }
        //
        //
        // Console.WriteLine("Start tessellate");
        // var meshes = TessellateAndOutputInstanceMeshes(
        //     facetGroupInstancingResult,
        //     pyramidInstancingResult);
        //
        // var geometriesIncludingMeshes = geometries
        //     .Where(g => g is not ProtoMesh)
        //     .Concat(meshes)
        //     .ToArray();

        var geometriesIncludingMeshes = geometries;

        Console.WriteLine($"Tessellated all meshes in {stopwatch.Elapsed}");
        stopwatch.Restart();

        SectorSplitter.ProtoSector[] sectors;
        if (composerParameters.SingleSector)
        {
            sectors = SectorSplitter.CreateSingleSector(geometriesIncludingMeshes).ToArray();
        }
        else if (composerParameters.SplitIntoZones)
        {
            var zones = ZoneSplitter.SplitIntoZones(geometriesIncludingMeshes, outputDirectory);
            Console.WriteLine($"Split into {zones.Length} zones in {stopwatch.Elapsed}");
            stopwatch.Restart();

            sectors = SectorSplitter.SplitIntoSectors(zones)
                .OrderBy(x => x.SectorId)
                .ToArray();
            Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }
        else
        {
            sectors = SectorSplitter.SplitIntoSectors(geometriesIncludingMeshes)
                .OrderBy(x => x.SectorId)
                .ToArray();
            Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }

        var sectorInfos = sectors
            .Select(s => SerializeSector(s, outputDirectory.FullName))
            .ToArray();

        Console.WriteLine($"Serialized {sectors.Length} sectors in {stopwatch.Elapsed}");
        stopwatch.Restart();

        var sectorsWithDownloadSize = CalculateDownloadSizes(sectorInfos, outputDirectory).ToImmutableArray();
        var cameraPosition = CameraPositioning.CalculateInitialCamera(geometriesIncludingMeshes);
        SceneCreator.WriteSceneFile(
            sectorsWithDownloadSize,
            modelParameters,
            outputDirectory,
            treeIndexGenerator.CurrentMaxGeneratedIndex,
            cameraPosition);

        Console.WriteLine($"Wrote scene file in {stopwatch.Elapsed}");
        stopwatch.Restart();

        Task.WaitAll(exportHierarchyDatabaseTask);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {total.Elapsed}");
    }

    private static SceneCreator.SectorInfo SerializeSector(SectorSplitter.ProtoSector p, string outputDirectory)
    {
        var estimateDrawCalls = DrawCallEstimator.Estimate(p.Geometries);

        var sectorInfo = new SceneCreator.SectorInfo(
            p.SectorId,
            p.ParentSectorId,
            p.Depth,
            p.Path,
            $"sector_{p.SectorId}.glb",
            EstimatedTriangleCount: estimateDrawCalls.EstimatedTriangleCount,
            EstimatedDrawCalls: estimateDrawCalls.EstimatedDrawCalls,
            p.Geometries,
            p.BoundingBoxMin,
            p.BoundingBoxMax);
        SceneCreator.ExportSector(sectorInfo, outputDirectory);

        return sectorInfo;
    }

    private static IEnumerable<SceneCreator.SectorInfo> CalculateDownloadSizes(IEnumerable<SceneCreator.SectorInfo> sectors, DirectoryInfo outputDirectory)
    {
        foreach (var sector in sectors)
        {
            var filepath = Path.Combine(outputDirectory.FullName, sector.Filename);
            yield return sector with
            {
                DownloadSize = new FileInfo(filepath).Length
            };
        }
    }

    public static APrimitive[] TessellateAndOutputInstanceMeshes(
        RvmFacetGroupMatcher.Result[] facetGroupInstancingResult,
        RvmPyramidInstancer.Result[] pyramidInstancingResult)
    {
        static TriangleMesh TessellateAndCreateTriangleMesh(ProtoMesh p)
        {
            var mesh = Tessellate(p.RvmPrimitive);
            return new TriangleMesh(mesh, p.TreeIndex, p.Color, p.AxisAlignedBoundingBox);
        }

        var facetGroupsNotInstanced = facetGroupInstancingResult
            .OfType<RvmFacetGroupMatcher.NotInstancedResult>()
            .Select(result => ((RvmFacetGroupWithProtoMesh)result.FacetGroup).ProtoMesh)
            .Cast<ProtoMesh>()
            .ToArray();

        var pyramidsNotInstanced = pyramidInstancingResult
            .OfType<RvmPyramidInstancer.NotInstancedResult>()
            .Select(result => result.Pyramid)
            .Cast<ProtoMesh>()
            .ToArray();

        var facetGroupInstanced = facetGroupInstancingResult
            .OfType<RvmFacetGroupMatcher.InstancedResult>()
            .GroupBy(result => (RvmPrimitive)result.Template, x => (ProtoMesh: (ProtoMesh)((RvmFacetGroupWithProtoMesh)x.FacetGroup).ProtoMesh, x.Transform))
            .ToArray();

        var pyramidsInstanced = pyramidInstancingResult
            .OfType<RvmPyramidInstancer.InstancedResult>()
            .GroupBy(result => (RvmPrimitive)result.Template, x => (ProtoMesh: (ProtoMesh)x.Pyramid, x.Transform))
            .ToArray();

        // tessellate instanced geometries
        var stopwatch = Stopwatch.StartNew();
        var meshes = facetGroupInstanced
            .Concat(pyramidsInstanced)
            .AsParallel()
            .Select(g => (InstanceGroup: g, Mesh: Tessellate(g.Key)))
            .Where(g => g.Mesh.Triangles.Length > 0) // ignore empty meshes
            .ToArray();
        var totalCount = meshes.Sum(m => m.InstanceGroup.Count());
        // Console.WriteLine(
        //     $"Tessellated {meshes.Length:N0} meshes for {totalCount:N0} instanced meshes in {stopwatch.Elapsed}");

        var instancedMeshes = meshes
            .SelectMany((group, index) => group.InstanceGroup.Select(item => new InstancedMesh(
                InstanceId: index,
                group.Mesh,
                item.Transform,
                item.ProtoMesh.TreeIndex,
                item.ProtoMesh.Color,
                item.ProtoMesh.AxisAlignedBoundingBox)))
            .ToArray();

        // tessellate and create TriangleMesh objects
        stopwatch.Restart();
        var triangleMeshes = facetGroupsNotInstanced
            .Concat(pyramidsNotInstanced)
            .AsParallel()
            .Select(TessellateAndCreateTriangleMesh)
            .Where(t => t.Mesh.Triangles.Length > 0) // ignore empty meshes
            .ToArray();
        // Console.WriteLine($"Tessellated {triangleMeshes.Length:N0} triangle meshes in {stopwatch.Elapsed}");

        return instancedMeshes
            .Cast<APrimitive>()
            .Concat(triangleMeshes)
            .ToArray();
    }

    private static Mesh Tessellate(RvmPrimitive primitive)
    {
        Mesh mesh;
        try
        {
            mesh = TessellatorBridge.Tessellate(primitive, 0f) ?? Mesh.Empty;
        }
        catch
        {
            mesh = Mesh.Empty;
        }

        if (mesh.Vertices.Length == 0)
        {
            if (primitive is RvmFacetGroup f)
            {
                Console.WriteLine($"WARNING: Could not tessellate facet group! Polygon count: {f.Polygons.Length}");
            }
            else if (primitive is RvmPyramid)
            {
                Console.WriteLine("WARNING: Could not tessellate pyramid!");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        return mesh;
    }

    /// <summary>
    /// Sole purpose is to keep the <see cref="ProtoMeshFromFacetGroup"/> through processing of facet group instancing.
    /// </summary>
    public record RvmFacetGroupWithProtoMesh(
            ProtoMeshFromFacetGroup ProtoMesh,
            uint Version,
            Matrix4x4 Matrix,
            RvmBoundingBox BoundingBoxLocal,
            RvmFacetGroup.RvmPolygon[] Polygons)
        : RvmFacetGroup(Version, Matrix, BoundingBoxLocal, Polygons);
}