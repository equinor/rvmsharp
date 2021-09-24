namespace CadRevealComposer
{
    using IdProviders;
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
        private static readonly SequentialIdGenerator MeshIdGenerator = new ();
        private static readonly SequentialIdGenerator SectorIdGenerator = new ();

        public record Parameters(ProjectId ProjectId, ModelId ModelId, RevisionId RevisionId);

        public static void Process(
            DirectoryInfo inputRvmFolderPath,
            DirectoryInfo outputDirectory,
            Parameters parameters)
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

            ProcessRvmStore(rvmStore, outputDirectory, parameters);
        }

        private static void ProcessRvmStore(RvmStore rvmStore, DirectoryInfo outputDirectory, Parameters parameters)
        {
            static IEnumerable<InstancedMesh> ToInstanceMesh(IGrouping<RvmFacetGroup, KeyValuePair<RvmFacetGroup, (RvmFacetGroup template, Matrix4x4 transform)>> group, Dictionary<RvmFacetGroup, ProtoMesh> protoMeshesMap)
            {
                var template = group.Key;
                var mesh = TessellatorBridge.Tessellate(template, -1f); // tolerance unused for RvmFacetGroup
                foreach (var primitive in group)
                {
                    var protoMesh = protoMeshesMap[primitive.Key];
                    Matrix4x4.Decompose(primitive.Value.transform, out var scale, out var rotation, out var translation);
                    var (rollX, pitchY, yawZ) = rotation.ToEulerAngles();
                    yield return new InstancedMesh(
                        new CommonPrimitiveProperties(protoMesh.NodeId, protoMesh.TreeIndex, translation, rotation, scale, protoMesh.Diagonal, protoMesh.AxisAlignedBoundingBox, protoMesh.Color, (Vector3.One, 0f)), // TODO: fix
                        0, 0, 0,
                        translation.X, translation.Y, translation.Z,
                        rollX, pitchY, yawZ,
                        scale.X, scale.Y, scale.Z)
                    {
                        TempTessellatedMesh = mesh
                    };
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
                .Select(root => CollectGeometryNodesRecursive(root, rootNode))
                .ToArray();

            rootNode.BoundingBoxAxisAligned = BoundingBoxEncapsulate(rootNode.Children
                .Select(x => x.BoundingBoxAxisAligned)
                .WhereNotNull()
                .ToArray());

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null, "Root node has no bounding box. Are there any meshes in the input?");

            var allNodes = GetAllNodesFlat(rootNode).ToArray();

            var pyramidInstancingTimer = Stopwatch.StartNew();
            PyramidInstancingHelper pyramidInstancingHelper = new PyramidInstancingHelper(allNodes);
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
            var instancedMeshesFileId = MeshIdGenerator.GetNextId();

            // The following code should be refactored, i'm just not sure how
            // We need to remove all instancedMeshes, and the re-add them.
            //  The reason for this is that they are immutable, and we actually add new copies with altered data.
            var instancedMeshes = geometries.OfType<InstancedMesh>().ToArray();
            var protoMeshes = geometries.OfType<ProtoMesh>().ToArray();
            var protoMeshesMap = protoMeshes.ToDictionary(x => x.SourceMesh);

            var instancedMeshesFromProtoMeshes = RvmFacetGroupMatcher.MatchAll(protoMeshes.Select(x => x.SourceMesh).ToArray())
                .GroupBy(x => x.Value.template)
                .SelectMany(x => ToInstanceMesh(x, protoMeshesMap))
                .ToImmutableList();
            
            var allInstancedMeshes = instancedMeshes.Concat(instancedMeshesFromProtoMeshes).ToList();
            var exportedInstancedMeshes = SceneCreator.ExportInstancedMeshesToObjFile(outputDirectory, instancedMeshesFileId, allInstancedMeshes);

            var geometriesToExport = geometries
                .Except(instancedMeshes)
                .Except(protoMeshes)
                .Concat(exportedInstancedMeshes)
                .ToList();

            Console.WriteLine($"Exported instances in {exportInstancedMeshes.Elapsed}");

            Console.WriteLine($"Finished Geometry Conversion in: {geometryConversionTimer.Elapsed}");

            var sectors = SceneCreator.SplitIntoSectors(geometriesToExport, instancedMeshesFileId, 0, null, null, MeshIdGenerator, SectorIdGenerator)
                .OrderBy(x => x.SectorId)
                .ToImmutableArray();
            foreach (var sector in sectors)
            {
                SceneCreator.ExportSector(sector, outputDirectory);
            }
            SceneCreator.WriteSceneFile(sectors, parameters, outputDirectory, TreeIndexGenerator.CurrentMaxGeneratedIndex);

            Task.WaitAll(exportHierarchyDatabaseTask);

            Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
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

        public static CadRevealNode CollectGeometryNodesRecursive(RvmNode root, CadRevealNode parent)
        {
            var newNode = new CadRevealNode
            {
                NodeId = NodeIdGenerator.GetNodeId(null),
                TreeIndex = TreeIndexGenerator.GetNextId(),
                Group = root,
                Parent = parent,
                Children = null
            };

            CadRevealNode[] childrenCadNodes;
            RvmPrimitive[] rvmGeometries = Array.Empty<RvmPrimitive>();


            if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
            {
                childrenCadNodes = root.Children.Select(child =>
                {
                    switch (child)
                    {
                        case RvmPrimitive rvmPrimitive:
                            return CollectGeometryNodesRecursive(
                                new RvmNode(2, "Implicit geometry", root.Translation, root.MaterialId)
                                {
                                    Children = { rvmPrimitive }
                                }, newNode);
                        case RvmNode rvmNode:
                            return CollectGeometryNodesRecursive(rvmNode, newNode);
                        default:
                            throw new Exception();
                    }
                }).ToArray();
            }
            else
            {
                childrenCadNodes = root.Children.OfType<RvmNode>()
                    .Select(n => CollectGeometryNodesRecursive(n, newNode))
                    .ToArray();
                rvmGeometries = root.Children.OfType<RvmPrimitive>().ToArray();
            }

            newNode.RvmGeometries = rvmGeometries;
            newNode.Children = childrenCadNodes;

            var primitiveBoundingBoxes = root.Children.OfType<RvmPrimitive>()
                .Select(x => x.CalculateAxisAlignedBoundingBox()).ToArray();
            var childrenBounds = newNode.Children.Select(x => x.BoundingBoxAxisAligned)
                .WhereNotNull();

            var primitiveAndChildrenBoundingBoxes = primitiveBoundingBoxes.Concat(childrenBounds).ToArray();
            newNode.BoundingBoxAxisAligned = BoundingBoxEncapsulate(primitiveAndChildrenBoundingBoxes);

            return newNode;
        }

        private static RvmBoundingBox? BoundingBoxEncapsulate(RvmBoundingBox[] boundingBoxes)
        {
            if (!boundingBoxes.Any())
                return null;

            // Find the min and max values for each of x,y, and z dimensions.
            var min = boundingBoxes.Select(x => x.Min).Aggregate(Vector3.Min);
            var max = boundingBoxes.Select(x => x.Max).Aggregate(Vector3.Max);
            return new RvmBoundingBox(Min: min, Max: max);
        }
    }
}