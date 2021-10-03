namespace CadRevealComposer
{
    using Configuration;
    using IdProviders;
    using Operations;
    using Primitives;
    using Primitives.Converters;
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
            var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
            {
                Console.WriteLine($"{x.fileName} ({x.progress}/{x.total})");
            });
            var rvmStore = Workload.ReadRvmData(workload, progressReport);
            Console.WriteLine($"Read RvmData in {rvmTimer.Elapsed}");

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
            SequentialIdGenerator sectorIdGenerator = new();

            Console.WriteLine("Generating i3d");

            var allNodes = RvmStoreToCadRevealNodesConverter.RvmStoreToCadRevealNodes(rvmStore, nodeIdGenerator, treeIndexGenerator);

            // TODO: move to ProtoMesh
            var pyramidInstancingTimer = Stopwatch.StartNew();
            var pyramidInstancingHelper = new PyramidInstancingHelper(allNodes);
            Console.WriteLine($"Prepared Pyramids in {pyramidInstancingTimer.Elapsed}");

            var geometries = allNodes
                .AsParallel()
                .AsOrdered()
                .SelectMany(x => x.RvmGeometries.Select(primitive =>
                    APrimitive.FromRvmPrimitive(x, x.Group as RvmNode ?? throw new InvalidOperationException(),
                        primitive)))
                .WhereNotNull()
                .ToArray();

            var exportHierarchyDatabaseTask = Task.Run(() =>
            {
                var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
                SceneCreator.ExportHierarchyDatabase(databasePath, allNodes);
                Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
            });

            var protoMeshes = geometries.OfType<ProtoMesh>().ToArray();

            var rvmFacetGroupResults = RvmFacetGroupMatcher.MatchAll(protoMeshes.Select(x => x.SourceMesh).ToArray()).GroupBy(x => x.Value.template);


            var instancedMeshes = rvmFacetGroupResults.Where(g => g.Count() > 1).ToArray();
            var instancedTemplateAndTransformByOriginalFacetGroup = instancedMeshes
                .SelectMany(g => g)
                .ToDictionary(g => g.Key, g => g.Value);

            const float unusedTesValue = 0;
            var meshByInstance = instancedMeshes.ToDictionary(g => g.Key, g => TessellatorBridge.Tessellate(g.Key, unusedTesValue));
            var exporter = new PeripheralFileExporter(outputDirectory.FullName, composerParameters.Mesh2CtmToolPath);
            var (instancedMeshFileId, instancedMeshLookup) = await exporter.ExportInstancedMeshesToObjFile(meshByInstance.Select(im => im.Value).ToArray());
            var offsetByTemplate = meshByInstance.ToDictionary(g => g.Key, g => instancedMeshLookup[g.Value!]);


            var iMeshes = protoMeshes.Where(p => instancedTemplateAndTransformByOriginalFacetGroup.ContainsKey(p.SourceMesh))
                .Select(p =>
                {
                    var (template, transform) = instancedTemplateAndTransformByOriginalFacetGroup[p.SourceMesh];
                    var (triangleOffset, triangleCount) = offsetByTemplate[template];
                    if (!Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation))
                    {
                        throw new Exception("Could not decompose");
                    }

                    (float rollX, float pitchY, float yawZ) = rotation.ToEulerAngles();
                    return new InstancedMesh(
                        new CommonPrimitiveProperties(p.NodeId, p.TreeIndex, Vector3.Zero, Quaternion.Identity,
                            Vector3.One,
                            p.Diagonal, p.AxisAlignedBoundingBox, p.Color,
                            (Vector3.UnitZ, 0)),
                        instancedMeshFileId, (ulong)triangleOffset, (ulong)triangleCount, translation.X,
                        translation.Y, translation.Z,
                        rollX, pitchY, yawZ, scale.X, scale.Y, scale.Z);
                }).ToArray();
            var tMeshes = protoMeshes
                .Where(p => !instancedTemplateAndTransformByOriginalFacetGroup.ContainsKey(p.SourceMesh))
                .Select(p =>
                    {
                        var mesh = TessellatorBridge.Tessellate(p.SourceMesh, unusedTesValue);
                        if (mesh.Vertices.Count == 0)
                        {
                            Console.WriteLine("WARNING: Could not tesselate facet group!");
                        }
                        var triangleCount = mesh.Triangles.Count / 3;
                        return new TriangleMesh(
                            new CommonPrimitiveProperties(p.NodeId, p.TreeIndex, Vector3.Zero, Quaternion.Identity,
                                Vector3.One,
                                p.Diagonal, p.AxisAlignedBoundingBox, p.Color,
                                (Vector3.UnitZ, 0)), 0, (ulong)triangleCount, mesh);
                    }
                ).Where(t => t.TempTessellatedMesh.Vertices.Count > 0).ToArray();

            geometries = geometries.Where(g => g is not ProtoMesh).Concat(iMeshes).Concat(tMeshes).ToArray();

            var maxDepth = composerParameters.SingleSector
                ? 0U
                : 5U;
            var sectors = SectorSplitter.SplitIntoSectors(
                    geometries,
                    0,
                    null,
                    null,
                    sectorIdGenerator,
                    maxDepth)
                .OrderBy(x => x.SectorId).ToArray();
            var sectorInfoTasks = sectors.Select(s => SerializeSector(s, outputDirectory.FullName, exporter));
            var sectorInfos = await Task.WhenAll(sectorInfoTasks);

            var sectorsWithDownloadSize = CalculateDownloadSizes(sectorInfos, outputDirectory).ToImmutableArray();
            SceneCreator.WriteSceneFile(sectorsWithDownloadSize, modelParameters, outputDirectory, treeIndexGenerator.CurrentMaxGeneratedIndex);

            Task.WaitAll(exportHierarchyDatabaseTask);

            Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        }

        private static async Task<SceneCreator.SectorInfo> SerializeSector(SectorSplitter.ProtoSector p, string outputDirectory, PeripheralFileExporter exporter)
        {
            var sectorFileName = $"sector_{p.SectorId}.i3d";
            var meshes = p.Geometries.OfType<TriangleMesh>().Select(t => t.TempTessellatedMesh).ToArray();
            var geometries = p.Geometries;
            if (meshes.Length > 0)
            {
                var (triangleMeshFileId, _) = await exporter.ExportInstancedMeshesToObjFile(meshes);
                geometries = p.Geometries.Select(g =>
                {
                    switch (g)
                    {
                        case TriangleMesh t:
                            return t with { FileId = triangleMeshFileId };
                        default:
                            return g;
                    }
                }).ToArray();
            }

            // DEBUG: uncomment to disable triangle meshes
            //geometries = geometries.Where(g => g is not TriangleMesh).ToArray();
            // DEBUG: uncomment to disable instanced meshes
            //geometries = geometries.Where(g => g is not InstancedMesh).ToArray();
            // DEBUG: uncomment to show only instanced meshes
            //geometries = geometries.Where(g => g is InstancedMesh).ToArray();

            var (estimatedTriangleCount, estimatedDrawCallCount) = DrawCallEstimator.Estimate(geometries);

            var peripheralFiles = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<ulong>(
                geometries, I3dfAttribute.AttributeType.FileId).Distinct().Select(id => $"mesh_{id}.ctm").ToArray();
            var sectorInfo = new SceneCreator.SectorInfo(p.SectorId, p.ParentSectorId, p.Depth, p.Path, sectorFileName, peripheralFiles,
                estimatedTriangleCount, estimatedDrawCallCount, geometries, p.BoundingBox);
            SceneCreator.ExportSector(sectorInfo, outputDirectory);
            return sectorInfo;
        }

        private static IEnumerable<SceneCreator.SectorInfo> CalculateDownloadSizes(IEnumerable<SceneCreator.SectorInfo> sectors, DirectoryInfo outputDirectory)
        {
            foreach (var sector in sectors)
            {
                var downloadSize = sector.PeripheralFiles.Concat(new[] { sector.Filename })
                    .Select(filename => Path.Combine(outputDirectory.FullName, filename))
                    .Select(filepath => new FileInfo(filepath).Length)
                    .Sum();
                yield return sector with
                {
                    DownloadSize = downloadSize
                };
            }
        }
    }
}