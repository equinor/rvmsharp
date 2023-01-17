namespace CadRevealComposer;

using Ben.Collections.Specialized;
using CadRevealFbxProvider.BatchUtils;
using Configuration;
using IdProviders;
using Operations;
using Primitives;
using Primitives.Reflection;
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
    public static async Task Process(
        DirectoryInfo inputRvmFolderPath,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters)
    {
        var workload = Workload.CollectWorkload(new[] { inputRvmFolderPath.FullName });

        Console.WriteLine("Reading RvmData");
        var rvmTimer = Stopwatch.StartNew();

        var teamCityReadRvmFilesLogBlock = new TeamCityLogBlock("Reading Rvm Files");
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });

        var stringInternPool = new BenStringInternPool(new SharedInternPool());
        var rvmStore = Workload.ReadRvmData(workload, progressReport, stringInternPool);
        var fileSizesTotal = workload.Sum(w => new FileInfo(w.rvmFilename).Length);
        teamCityReadRvmFilesLogBlock.CloseBlock();
        Console.WriteLine(
            $"Read RvmData in {rvmTimer.Elapsed}. (~{fileSizesTotal / 1024 / 1024}mb of .rvm files (excluding .txt file size))");

        await ProcessRvmStore(rvmStore, outputDirectory, modelParameters, composerParameters);
    }

    public static async Task ProcessRvmStore(
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
            var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
            SceneCreator.ExportHierarchyDatabase(databasePath, allNodes);
            Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
        });

        static bool IsValidGeometry(RvmPrimitive geometry)
        {
            // TODO: Investigate why we have negative extents in the RVM data, and clarify if we have negligable data loss by excluding these parts. Follow up in AB#58629 ( https://dev.azure.com/EquinorASA/DT%20%E2%80%93%20Digital%20Twin/_workitems/edit/58629 )
            var extents = geometry.BoundingBoxLocal.Extents;
            if (extents.X < 0 || extents.Y < 0 || extents.Z < 0)
            {
                return false;
            }
            return true;
        }

        var invalidGeometriesGroupedByType = allNodes
            .SelectMany(x => x.RvmGeometries.Where(g => !IsValidGeometry(g)))
            .GroupBy(g => g.GetType())
            .OrderBy(g => g.Key.Name);

        foreach (var group in invalidGeometriesGroupedByType)
        {
            Console.WriteLine($"Excluded {group.Count()} {group.Key.Name} due to negative extents in either X/Y/Z.");
        }

        var geometries = allNodes
            .AsParallel()
            .AsOrdered()
            .SelectMany(x => x.RvmGeometries
                .Where(IsValidGeometry)
                .Select(primitive => APrimitive.FromRvmPrimitive(x, primitive)))
            .WhereNotNull()
            .ToArray();

        Console.WriteLine($"Primitives converted in {stopwatch.Elapsed}");
        stopwatch.Restart();

        var facetGroupsWithEmbeddedProtoMeshes = geometries
            .OfType<ProtoMeshFromFacetGroup>()
            .Select(p => new RvmFacetGroupWithProtoMesh(p, p.FacetGroup.Version, p.FacetGroup.Matrix, p.FacetGroup.BoundingBoxLocal, p.FacetGroup.Polygons))
            .Cast<RvmFacetGroup>()
            .ToArray();

        RvmFacetGroupMatcher.Result[] facetGroupInstancingResult;
        if (composerParameters.NoInstancing)
        {
            facetGroupInstancingResult = facetGroupsWithEmbeddedProtoMeshes
                .Select(x => new RvmFacetGroupMatcher.NotInstancedResult(x))
                .Cast<RvmFacetGroupMatcher.Result>()
                .ToArray();
            Console.WriteLine("Facet group instancing disabled.");
        }
        else
        {
            facetGroupInstancingResult = RvmFacetGroupMatcher.MatchAll(
                facetGroupsWithEmbeddedProtoMeshes,
                facetGroups => facetGroups.Length >= modelParameters.InstancingThreshold.Value);
            Console.WriteLine($"Facet groups instance matched in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }

        var protoMeshesFromPyramids = geometries.OfType<ProtoMeshFromPyramid>().ToArray();
        // We have models where several pyramids on the same "part" are completely identical.
        var uniqueProtoMeshesFromPyramid = protoMeshesFromPyramids.Distinct().ToArray();
        if (uniqueProtoMeshesFromPyramid.Length < protoMeshesFromPyramids.Length)
        {
            var diffCount = protoMeshesFromPyramids.Length - uniqueProtoMeshesFromPyramid.Length;
            Console.WriteLine($"Found and ignored {diffCount} duplicate pyramids (including: position, mesh, parent, id, etc).");
        }
        RvmPyramidInstancer.Result[] pyramidInstancingResult;
        if (composerParameters.NoInstancing)
        {
            pyramidInstancingResult = uniqueProtoMeshesFromPyramid
                .Select(x => new RvmPyramidInstancer.NotInstancedResult(x))
                .OfType<RvmPyramidInstancer.Result>()
                .ToArray();
            Console.WriteLine("Pyramid instancing disabled.");
        }
        else
        {
            pyramidInstancingResult = RvmPyramidInstancer.Process(
                uniqueProtoMeshesFromPyramid,
                pyramids => pyramids.Length >= modelParameters.InstancingThreshold.Value);
            Console.WriteLine($"Pyramids instance matched in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }
        
        var exporter = new PeripheralFileExporter(outputDirectory.FullName, composerParameters.Mesh2CtmToolPath);

        Console.WriteLine("Start tessellate");
        var meshes = await TessellateAndOutputInstanceMeshes(
            facetGroupInstancingResult,
            pyramidInstancingResult,
            exporter);

        var geometriesIncludingMeshes = geometries
            .Where(g => g is not ProtoMesh)
            .Concat(meshes)
            .ToArray();

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

        var sectorInfoTasks = sectors.Select(s => SerializeSector(s, outputDirectory.FullName, exporter));
        var sectorInfos = await Task.WhenAll(sectorInfoTasks);

        Console.WriteLine($"Serialized {sectorInfos.Length} sectors in {stopwatch.Elapsed}");
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

    private static async Task<SceneCreator.SectorInfo> SerializeSector(SectorSplitter.ProtoSector p, string outputDirectory, PeripheralFileExporter exporter)
    {
        var sectorFileName = $"sector_{p.SectorId}.i3d";
        var meshes = p.Geometries
            .OfType<TriangleMesh>()
            .Select(t => t.TempTessellatedMesh)
            .WhereNotNull()
            .ToArray();
        var geometries = p.Geometries;
        if (meshes.Length > 0)
        {
            var (triangleMeshFileId, _) = await exporter.ExportMeshesToObjAndCtmFile(meshes, mesh => mesh);
            geometries = p.Geometries.Select(g => g switch
            {
                TriangleMesh t => t with { FileId = triangleMeshFileId },
                _ => g
            }).ToArray();
        }

        var (estimatedTriangleCount, estimatedDrawCallCount) = DrawCallEstimator.Estimate(geometries);

        var peripheralFiles = APrimitiveReflectionHelpers
            .GetDistinctValuesOfAllPropertiesMatchingKind<ulong>(geometries, I3dfAttribute.AttributeType.FileId)
            .Distinct()
            .Select(id => $"mesh_{id}.ctm")
            .ToArray();
        var sectorInfo = new SceneCreator.SectorInfo(p.SectorId, p.ParentSectorId, p.Depth, p.Path, sectorFileName,
            peripheralFiles, estimatedTriangleCount, estimatedDrawCallCount, geometries, p.BoundingBoxMin, p.BoundingBoxMax);
        SceneCreator.ExportSector(sectorInfo, outputDirectory);

        return sectorInfo;
    }

    private static IEnumerable<SceneCreator.SectorInfo> CalculateDownloadSizes(IEnumerable<SceneCreator.SectorInfo> sectors, DirectoryInfo outputDirectory)
    {
        foreach (var sector in sectors)
        {
            var downloadSize = sector.PeripheralFiles
                .Concat(new[] { sector.Filename })
                .Select(filename => Path.Combine(outputDirectory.FullName, filename))
                .Select(filepath => new FileInfo(filepath).Length)
                .Sum();
            yield return sector with
            {
                DownloadSize = downloadSize
            };
        }
    }

    private static async Task<APrimitive[]> TessellateAndOutputInstanceMeshes(
        RvmFacetGroupMatcher.Result[] facetGroupInstancingResult,
        RvmPyramidInstancer.Result[] pyramidInstancingResult,
        PeripheralFileExporter exporter)
    {
        static TriangleMesh TessellateAndCreateTriangleMesh(ProtoMesh p)
        {
            var mesh = Tessellate(p.ProtoPrimitive);
            var triangleCount = mesh.Triangles.Count / 3;
            return new TriangleMesh(
                new CommonPrimitiveProperties(p.NodeId, p.TreeIndex,
                    Vector3.Zero, Quaternion.Identity, Vector3.One,
                    p.Diagonal, p.AxisAlignedBoundingBox, p.Color,
                    (Vector3.UnitZ, 0), p.ProtoPrimitive), 0, (ulong)triangleCount, mesh);
        }

        static InstancedMesh CreateInstanceMesh(ProtoMesh p, Matrix4x4 transform, uint meshFileId, ulong triangleOffset, ulong triangleCount)
        {
            if (!transform.DecomposeAndNormalize(out var scale, out var rotation, out var translation))
            {
                throw new Exception("Could not decompose");
            }

            (float rollX, float pitchY, float yawZ) = rotation.ToEulerAngles();
            AlgebraUtils.AssertEulerAnglesCorrect((rollX, pitchY, yawZ), rotation);

            return new InstancedMesh(
                new CommonPrimitiveProperties(p.NodeId, p.TreeIndex, Vector3.Zero, Quaternion.Identity,
                    Vector3.One,
                    p.Diagonal, p.AxisAlignedBoundingBox, p.Color,
                    (Vector3.UnitZ, 0), p.ProtoPrimitive),
                meshFileId, triangleOffset, triangleCount, translation.X,
                translation.Y, translation.Z,
                rollX, pitchY, yawZ, scale.X, scale.Y, scale.Z);
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
            .Where(g => g.Mesh.Triangles.Count > 0) // ignore empty meshes
            .ToArray();
        var totalCount = meshes.Sum(m => m.InstanceGroup.Count());
        Console.WriteLine($"Tessellated {meshes.Length:N0} meshes for {totalCount:N0} instanced meshes in {stopwatch.Elapsed}");

        // write instanced meshes to file
        var exportedMeshes = await exporter.ExportMeshesToObjAndCtmFile(meshes, m => m.Mesh);

        // create InstancedMesh objects
        var instancedMeshes = exportedMeshes.Results
            .SelectMany(x =>
            {
                return x.Item.InstanceGroup.Select(y => CreateInstanceMesh(y.ProtoMesh, y.Transform, exportedMeshes.FileId, x.TriangleOffset, x.TriangleCount));
            })
            .ToArray();

        // tessellate and create TriangleMesh objects
        stopwatch.Restart();
        var triangleMeshes = facetGroupsNotInstanced
            .Concat(pyramidsNotInstanced)
            .AsParallel()
            .Select(TessellateAndCreateTriangleMesh)
            .Where(t => t.TriangleCount > 0) // ignore empty meshes
            .ToArray();
        Console.WriteLine($"Tessellated {triangleMeshes.Length:N0} triangle meshes in {stopwatch.Elapsed}");

        return instancedMeshes
            .Cast<APrimitive>()
            .Concat(triangleMeshes)
            .ToArray();
    }

    private static Mesh Tessellate(RvmPrimitive primitive)
    {
        Mesh?  mesh;
        try
        {
            mesh = TessellatorBridge.Tessellate(primitive, 0f);
            mesh = mesh != null ? MeshTools.DeduplicateVertices(mesh) : Mesh.Empty;
        }
        catch
        {
            mesh = Mesh.Empty;
        }

        if (mesh.Vertices.Count == 0)
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
    private record RvmFacetGroupWithProtoMesh(ProtoMeshFromFacetGroup ProtoMesh, uint Version, Matrix4x4 Matrix, RvmBoundingBox BoundingBoxLocal, RvmFacetGroup.RvmPolygon[] Polygons)
        : RvmFacetGroup(Version, Matrix, BoundingBoxLocal, Polygons);
}