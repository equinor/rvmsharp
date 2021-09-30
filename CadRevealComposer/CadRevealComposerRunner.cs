namespace CadRevealComposer
{
    using IdProviders;
    using Operations;
    using Primitives;
    using Primitives.Converters;
    using Primitives.Instancing;
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

    public record ProjectId(long Value);

    public record ModelId(long Value);

    public record RevisionId(long Value);

    public static class CadRevealComposerRunner
    {
        private static readonly TreeIndexGenerator TreeIndexGenerator = new();
        private static readonly NodeIdProvider NodeIdGenerator = new();
        private static readonly SequentialIdGenerator MeshFileIdGenerator = new();
        private static readonly SequentialIdGenerator SectorIdGenerator = new();

        public record Parameters(ProjectId ProjectId, ModelId ModelId, RevisionId RevisionId, bool NoInstancing, bool CreateSingleSector, bool DeterministicOutput);

        public record ToolsParameters(string Mesh2CtmToolPath, string I3dfDumpToolPath, bool GenerateSectorDumpFiles);

        public static void Process(
            DirectoryInfo inputRvmFolderPath,
            DirectoryInfo outputDirectory,
            Parameters parameters,
            ToolsParameters toolsParameters)
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

            ProcessRvmStore(rvmStore, outputDirectory, parameters, toolsParameters);
        }

        private static void ProcessRvmStore(
            RvmStore rvmStore,
            DirectoryInfo outputDirectory,
            Parameters parameters,
            ToolsParameters toolsParameters)
        {
            IEnumerable<APrimitive> ToMeshes(IGrouping<RvmFacetGroup, KeyValuePair<RvmFacetGroup, (RvmFacetGroup _, Matrix4x4 transform)>> group, Dictionary<RvmFacetGroup, ProtoMesh> protoMeshesMap)
            {
                var isGroupWithSingleItem = group.Count() == 1;
                if (isGroupWithSingleItem || parameters.NoInstancing)
                {
                    foreach (var facetGroup in group)
                    {
                        var transform = facetGroup.Value.transform * facetGroup.Key.Matrix;
                        var template = group.Key with
                        {
                            Matrix = transform
                        };
                        var mesh = TessellatorBridge.Tessellate(template, -1f); // tolerance unused for RvmFacetGroup

                        var protoMesh = protoMeshesMap[facetGroup.Key];

                        yield return new TriangleMesh(
                            new CommonPrimitiveProperties(protoMesh.NodeId, protoMesh.TreeIndex, Vector3.Zero, Quaternion.Identity, Vector3.Zero, 0, protoMesh.AxisAlignedBoundingBox, protoMesh.Color, (Vector3.One, 0f)),
                            ulong.MaxValue, // NOTE: FileId will be set later on
                            (ulong)mesh.Triangles.Count / 3,
                            mesh);
                    }
                }
                else
                {
                    var templateMesh = TessellatorBridge.TessellateWithoutApplyingMatrix(group.Key, 1.0f, -1f); // tolerance unused for RvmFacetGroup

                    foreach (var facetGroup in group)
                    {
                        var transform = facetGroup.Value.transform * facetGroup.Key.Matrix;
                        if (!Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation))
                        {
                            throw new Exception("Could not decompose transformation matrix.");
                        }
                        var (rollX, pitchY, yawZ) = rotation.ToEulerAngles();

                        var protoMesh = protoMeshesMap[facetGroup.Key];

                        yield return new InstancedMesh(
                            new CommonPrimitiveProperties(protoMesh.NodeId, protoMesh.TreeIndex, Vector3.Zero, Quaternion.Identity, Vector3.Zero, protoMesh.AxisAlignedBoundingBox.Diagonal, protoMesh.AxisAlignedBoundingBox, protoMesh.Color, (Vector3.Zero, 0f)),
                            ulong.MaxValue, ulong.MaxValue, // NOTE: FileId, TriangleOffset will be set later on
                            (ulong)(templateMesh.Triangles.Count / 3),
                            translation.X, translation.Y, translation.Z,
                            rollX, pitchY, yawZ,
                            scale.X, scale.Y, scale.Z)
                        {
                            TempTessellatedMesh = templateMesh
                        };
                    }
                }
            }

            Console.WriteLine("Generating i3d");

            var rootNode = new CadRevealNode
            {
                NodeId = NodeIdGenerator.GetNodeId(null),
                TreeIndex = TreeIndexGenerator.GetNextId(),
                Parent = null,
                Group = null,
                Children = null
            };

            rootNode.Children = rvmStore.RvmFiles
                .SelectMany(f => f.Model.Children)
                .Select(root => RvmNodeToCadRevealNodeConverter.CollectGeometryNodesRecursive(root, rootNode, NodeIdGenerator, TreeIndexGenerator))
                .ToArray();

            rootNode.BoundingBoxAxisAligned = rootNode.Children
                .Select(x => x.BoundingBoxAxisAligned)
                .WhereNotNull()
                .ToArray().Aggregate((a, b) => a.Encapsulate(b));

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null, "Root node has no bounding box. Are there any meshes in the input?");

            var allNodes = GetAllNodesFlat(rootNode).ToArray();

            var pyramidInstancingTimer = Stopwatch.StartNew();
            var pyramidInstancingHelper = new PyramidInstancingHelper(allNodes);
            Console.WriteLine($"Prepared Pyramids in {pyramidInstancingTimer.Elapsed}");

            var geometryConversionTimer = Stopwatch.StartNew();
            // AsOrdered is important. And I dont like it...
            //  - Its important  of the "TriangleMesh TriangleCount" is "sequential-additive".
            // So the position offset in the mesh is determined on the TriangleCount of all items in the Sequence "12"+"16"+"10", and needs the identical order.
            var geometries = allNodes
                .AsParallel()
                .AsOrdered()
                .SelectMany(x => x.RvmGeometries.Select(primitive =>
                    APrimitive.FromRvmPrimitive(x, x.Group as RvmNode ?? throw new InvalidOperationException(),
                        primitive, pyramidInstancingHelper)))
                .WhereNotNull()
                .ToList();

            var exportHierarchyDatabaseTask = Task.Run(() =>
            {
                var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
                SceneCreator.ExportHierarchyDatabase(databasePath, allNodes);
                Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
            });

            var exportInstancedMeshes = Stopwatch.StartNew();
            var instancedMeshesFileId = MeshFileIdGenerator.GetNextId();

            // The following code should be refactored, i'm just not sure how
            // We need to remove all instancedMeshes, and the re-add them.
            //  The reason for this is that they are immutable, and we actually add new copies with altered data.
            var instancedMeshes = geometries.OfType<InstancedMesh>().ToArray();
            var protoMeshes = geometries.OfType<ProtoMesh>().ToArray();
            var protoMeshesMap = protoMeshes.ToDictionary(x => x.SourceMesh);

            var rvmFacetGroupResults = RvmFacetGroupMatcher.MatchAll(protoMeshes.Select(x => x.SourceMesh).ToArray(), parameters.DeterministicOutput)
                .GroupBy(x => x.Value.template)
                .SelectMany(x => ToMeshes(x, protoMeshesMap))
                .ToImmutableList();

            var allInstancedMeshes = instancedMeshes.Concat(rvmFacetGroupResults.OfType<InstancedMesh>()).ToList();
            var exportedInstancedMeshes = InstancedMeshFileExporter.ExportInstancedMeshesToObjFile(outputDirectory, instancedMeshesFileId, allInstancedMeshes);

            var geometriesToExport = geometries
                .Except(instancedMeshes)
                .Except(protoMeshes)
                .Concat(exportedInstancedMeshes)
                .Concat(rvmFacetGroupResults.OfType<TriangleMesh>())
                .ToList();

            Console.WriteLine($"Exported instances in {exportInstancedMeshes.Elapsed}");

            Console.WriteLine($"Finished Geometry Conversion in: {geometryConversionTimer.Elapsed}");

            var maxDepth = parameters.CreateSingleSector
                ? 0U
                : 5U;
            var sectors = SectorSplitter.SplitIntoSectors(geometriesToExport, instancedMeshesFileId, 0, null, null, MeshFileIdGenerator, SectorIdGenerator, maxDepth)
                .OrderBy(x => x.SectorId)
                .ToImmutableArray();

            var convertedObjFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sector in sectors)
            {
                SceneCreator.ExportSector(sector, outputDirectory);

                if (sector.MeshFileId.HasValue)
                {
                    foreach (var sectorPeripheralFile in sector.PeripheralFiles.Where(filename => !convertedObjFiles.Contains(filename)))
                    {
                        var inputFilename = $"{Path.GetFileNameWithoutExtension(sectorPeripheralFile)}.obj";
                        var inputPath = Path.Combine(outputDirectory.FullName, inputFilename);
                        var outputPath = Path.Combine(outputDirectory.FullName, sectorPeripheralFile);
                        var sw = Stopwatch.StartNew();
                        System.Diagnostics.Process.Start(toolsParameters.Mesh2CtmToolPath, $"\"{inputPath}\" \"{outputPath}\" --comment \"RvmSharp\" --method MG1 --level 4 --no-texcoords --no-colors --upaxis Y")
                            .WaitForExit();
                        sw.Stop();

                        Console.WriteLine($"Saving {outputPath}... {sw.ElapsedMilliseconds} ms");

                        convertedObjFiles.Add(sectorPeripheralFile);
                    }
                }

                if (toolsParameters.GenerateSectorDumpFiles)
                {
                    var inputPath = Path.Combine(outputDirectory.FullName, sector.Filename);
                    var outputPath = Path.Combine(outputDirectory.FullName, $"{sector.Filename}.dump");

                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = toolsParameters.I3dfDumpToolPath,
                            Arguments = $"\"{inputPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    var sw = Stopwatch.StartNew();
                    process.Start();
                    using var streamWriter = File.CreateText(outputPath);
                    using var streamReader = process.StandardOutput;
                    string? line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        streamWriter.WriteLine(line);
                    }
                    streamWriter.Flush();
                    process.WaitForExit();
                    sw.Stop();

                    Console.WriteLine($"Saving {outputPath}... {sw.ElapsedMilliseconds} ms");
                }
            }

            var sectorsWithDownloadSize = CalculateDownloadSizes(sectors, outputDirectory).ToImmutableArray();
            SceneCreator.WriteSceneFile(sectorsWithDownloadSize, parameters, outputDirectory, TreeIndexGenerator.CurrentMaxGeneratedIndex);

            Task.WaitAll(exportHierarchyDatabaseTask);

            Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
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

        private static IEnumerable<CadRevealNode> GetAllNodesFlat(CadRevealNode root)
        {
            yield return root;

            if (root.Children != null)
            {
                foreach (CadRevealNode cadRevealNode in root.Children)
                {
                    foreach (CadRevealNode revealNode in GetAllNodesFlat(cadRevealNode))
                    {
                        yield return revealNode;
                    }
                }
            }
        }
    }
}