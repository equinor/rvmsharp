namespace CadRevealComposer
{
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
            var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
            {
                Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
            });
            var rvmStore = Workload.ReadRvmData(workload, progressReport);
            var fileSizesTotal = workload.Sum(w => new FileInfo(w.rvmFilename).Length);
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
            SequentialIdGenerator sectorIdGenerator = new();

            Console.WriteLine("Generating i3d");

            var total = Stopwatch.StartNew();
            var stopwatch = Stopwatch.StartNew();
            var allNodes = RvmStoreToCadRevealNodesConverter.RvmStoreToCadRevealNodes(rvmStore, nodeIdGenerator, treeIndexGenerator);
            Console.WriteLine("Converted to reveal nodes in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var geometries = allNodes
                .AsParallel()
                .AsOrdered()
                .SelectMany(x => x.RvmGeometries.Select(primitive =>
                    APrimitive.FromRvmPrimitive(x, x.Group as RvmNode ?? throw new InvalidOperationException(),
                        primitive)))
                .WhereNotNull()
                .ToArray();

            Console.WriteLine("Primitives converted in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var exportHierarchyDatabaseTask = Task.Run(() =>
            {
                var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
                SceneCreator.ExportHierarchyDatabase(databasePath, allNodes);
                Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
            });

            var protoMeshesFromFacetGroups = geometries.OfType<ProtoMeshFromFacetGroup>().ToArray();
            var protoMeshesFromPyramids = geometries.OfType<ProtoMeshFromPyramid>().ToArray();

            const uint defaultInstancingThreshold = 300; // We should consider making this threshold dynamic. Value of 300 is picked arbitrary.
            uint instanceCandidateThreshold = modelParameters.InstancingThresholdOverride?.Value ?? defaultInstancingThreshold;

            var sourceMeshes = protoMeshesFromFacetGroups.Select(x => x.SourceFacetGroup).ToArray();
            var facetGroupInstancingResult = RvmFacetGroupMatcher.MatchAll(sourceMeshes, instanceCandidateThreshold)
                .GroupBy(x => x.Value.template);

            Console.WriteLine("Facet groups matched in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var pyramidInstancingResult = RvmPyramidInstancer.Process(protoMeshesFromPyramids)
                .GroupBy(x => x.Value.template);

            Console.WriteLine("Pyramids matched in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var instancedMeshesFromFacetGroups = facetGroupInstancingResult
                .Where(g => g.Count() >= instanceCandidateThreshold)
                .ToArray();
            var instancedMeshesFromPyramids = pyramidInstancingResult
                .Where(g => g.Count() >= instanceCandidateThreshold)
                .ToArray();
            var instancedTemplateAndTransformByOriginalFacetGroup = instancedMeshesFromFacetGroups
                .SelectMany(g => g)
                .ToDictionary(g => g.Key, g => g.Value);
            var instancedTemplateAndTranformByOriginalPyramid = instancedMeshesFromPyramids
                .SelectMany(g => g)
                .ToDictionary(g => g.Key, g => g.Value);

            const float unusedTesValue = 0;
            var meshByInstance = instancedMeshesFromFacetGroups.ToDictionary(g => g.Key,
                g => TessellatorBridge.Tessellate(g.Key, unusedTesValue));
            var meshByPyramidInstance = instancedMeshesFromPyramids.ToDictionary(g => g.Key,
                g => TessellatorBridge.Tessellate(g.Key, unusedTesValue));

            var exporter = new PeripheralFileExporter(outputDirectory.FullName, composerParameters.Mesh2CtmToolPath);
            var (instancedMeshFileId, instancedMeshLookup) = await exporter.ExportMeshesToObjAndCtmFile(meshByInstance
                .Select(im => im.Value).Concat(meshByPyramidInstance.Select(im => im.Value))
                .ToArray());
            var offsetByTemplate =
                meshByInstance.ToDictionary(g => g.Key, g => instancedMeshLookup[new RefLookup<Mesh>(g.Value!)]);
            var offsetByTemplate2 =
                meshByPyramidInstance.ToDictionary(g => g.Key, g => instancedMeshLookup[new RefLookup<Mesh>(g.Value!)]);

            Console.WriteLine("Composed instance dictionaries in " + stopwatch.Elapsed);
            stopwatch.Restart();

            Console.WriteLine("Start Tessellate");

            var iMeshesTimer = Stopwatch.StartNew();
            var iMeshes = protoMeshesFromFacetGroups
                .Where(p => instancedTemplateAndTransformByOriginalFacetGroup.ContainsKey(p.SourceFacetGroup))
                .Select(p =>
                {
                    var (template, transform) = instancedTemplateAndTransformByOriginalFacetGroup[p.SourceFacetGroup];
                    var (triangleOffset, triangleCount) = offsetByTemplate[template];
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
                            (Vector3.UnitZ, 0), p.SourcePrimitive),
                        instancedMeshFileId, (ulong)triangleOffset, (ulong)triangleCount, translation.X,
                        translation.Y, translation.Z,
                        rollX, pitchY, yawZ, scale.X, scale.Y, scale.Z);
                }).Concat(protoMeshesFromPyramids
                    .Where(p => instancedTemplateAndTranformByOriginalPyramid.ContainsKey(p))
                    .Select(p =>
                    {
                        var (template, transform) = instancedTemplateAndTranformByOriginalPyramid[p];
                        var (triangleOffset, triangleCount) = offsetByTemplate2[template];
                        if (!Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation))
                        {
                            throw new Exception("Could not decompose");
                        }

                        rotation = Quaternion.Normalize(rotation);
                        (float rollX, float pitchY, float yawZ) = rotation.ToEulerAngles();
                        AlgebraUtils.AssertEulerAnglesCorrect((rollX, pitchY, yawZ), rotation);

                        return new InstancedMesh(
                            new CommonPrimitiveProperties(p.NodeId, p.TreeIndex, Vector3.Zero, Quaternion.Identity,
                                Vector3.One,
                                p.Diagonal, p.AxisAlignedBoundingBox, p.Color,
                                (Vector3.UnitZ, 0), p.SourcePrimitive),
                            instancedMeshFileId, (ulong)triangleOffset, (ulong)triangleCount, translation.X,
                            translation.Y, translation.Z,
                            rollX, pitchY, yawZ, scale.X, scale.Y, scale.Z);
                    }))
                .ToArray();

            Console.WriteLine($"\tTessellated {iMeshes.Length} Instanced Meshes in " + iMeshesTimer.Elapsed);
            var tMeshesTimer = Stopwatch.StartNew();
            var tMeshes = protoMeshesFromFacetGroups
                .AsParallel()
                .Where(p => !instancedTemplateAndTransformByOriginalFacetGroup.ContainsKey(p.SourceFacetGroup))
                .Select(p =>
                    {
                        Mesh? mesh;
                        try
                        {
                            mesh = TessellatorBridge.Tessellate(p.SourceFacetGroup, unusedTesValue)!;
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);
                            Console.WriteLine("Error caused by tessellating: " + p.SourceFacetGroup);
                            var fileName = $"treeIndex_{p.TreeIndex}.json";
                            var diagnosticsOutputDir = Path.Join(outputDirectory.FullName, "Failed");
                            // Ensure output folder exists.
                            Directory.CreateDirectory(diagnosticsOutputDir);

                            Console.WriteLine($"Writing Bad mesh to to file {fileName}");
                            JsonUtils.JsonSerializeToFile(p.SourceFacetGroup.Polygons,
                                Path.Join(outputDirectory.FullName, "Failed", fileName));
                            mesh = new Mesh(Array.Empty<float>(), Array.Empty<float>(), Array.Empty<int>(), 0);
                        }

                        if (mesh.Vertices.Count == 0)
                        {
                            Console.WriteLine("WARNING: Could not tessellate facet group! " +
                                              p.SourceFacetGroup.Polygons.Length);
                        }
                        var triangleCount = mesh.Triangles.Count / 3;
                        return new TriangleMesh(
                                new CommonPrimitiveProperties(p.NodeId, p.TreeIndex,
                                    Vector3.Zero, Quaternion.Identity, Vector3.One,
                                    p.Diagonal, p.AxisAlignedBoundingBox, p.Color,
                                    (Vector3.UnitZ, 0), p.SourcePrimitive), 0, (ulong)triangleCount, mesh);
                    }
                )
                .Concat(protoMeshesFromPyramids
                    .AsParallel()
                    .Where(p => !instancedTemplateAndTranformByOriginalPyramid.ContainsKey(p))
                    .Select(p =>
                    {
                        var mesh = TessellatorBridge.Tessellate(p.SourcePyramid, unusedTesValue);
                        if (mesh!.Vertices.Count == 0)
                        {
                            Console.WriteLine("WARNING: Could not tessellate facet group!");
                        }

                        var triangleCount = mesh.Triangles.Count / 3;
                        return new TriangleMesh(
                            new CommonPrimitiveProperties(p.NodeId, p.TreeIndex,
                                Vector3.Zero, Quaternion.Identity, Vector3.One,
                                p.Diagonal, p.AxisAlignedBoundingBox, p.Color,
                                (Vector3.UnitZ, 0), p.SourcePrimitive), 0, (ulong)triangleCount, mesh);
                    }))
                .Where(t => t.TempTessellatedMesh!.Vertices.Count > 0)
                .AsParallel()
                .ToArray();

            Console.WriteLine($"\tTessellated {tMeshes.Length} Triangle Meshes in " + tMeshesTimer.Elapsed);

            geometries = geometries
                .Where(g => g is not ProtoMesh)
                .Concat(iMeshes)
                .Concat(tMeshes)
                .ToArray();

            Console.WriteLine($"Tessellated all geometries in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var maxDepth = composerParameters.SingleSector
                ? SectorSplitter.StartDepth
                : 5U;
            var sectors = SectorSplitter.SplitIntoSectors(
                    geometries,
                    sectorIdGenerator,
                    maxDepth)
                .OrderBy(x => x.SectorId).ToArray();

            Console.WriteLine($"Split into {sectors.Length} sectors in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var faceSectors = sectors.AsParallel().Select(s => FacesConverter.ConvertSector(s, outputDirectory.FullName)).ToArray();
            Console.WriteLine("Converted into sectors in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var sectorInfoTasks = sectors.Select(s => SerializeSector(s, outputDirectory.FullName, exporter));
            var sectorInfos = await Task.WhenAll(sectorInfoTasks);

            Console.WriteLine($"Serialized {sectorInfos.Length} sectors in " + stopwatch.Elapsed);
            stopwatch.Restart();

            var sectorsWithDownloadSize = CalculateDownloadSizes(sectorInfos, outputDirectory).ToImmutableArray();
            SceneCreator.WriteSceneFile(sectorsWithDownloadSize, modelParameters, outputDirectory, treeIndexGenerator.CurrentMaxGeneratedIndex, faceSectors);

            Console.WriteLine("Wrote scene file in " + stopwatch.Elapsed);
            stopwatch.Restart();

            Task.WaitAll(exportHierarchyDatabaseTask);

            Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
            Console.WriteLine("Convert completed in " + total.Elapsed);
        }

        private static async Task<SceneCreator.SectorInfo> SerializeSector(SectorSplitter.ProtoSector p, string outputDirectory, PeripheralFileExporter exporter)
        {
            var sectorFileName = $"sector_{p.SectorId}.i3d";
            var meshes = p.Geometries
                .OfType<TriangleMesh>()
                .Select(t => t.TempTessellatedMesh)
                .ToArray();
            var geometries = p.Geometries;
            if (meshes.Length > 0)
            {
                var (triangleMeshFileId, _) = await exporter.ExportMeshesToObjAndCtmFile(meshes);
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
    }
}