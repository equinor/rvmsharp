namespace CadRevealRvmProvider;

static internal class RvmStoreToCadRevealNodesConverterWithPyramidsTemp
{
    // public static CadRevealNode[] RvmStoreToCadRevealNodes(RvmStore rvmStore,
    //     TreeIndexGenerator treeIndexGenerator)
    // {
    //     var rootNode = new CadRevealNode
    //     {
    //         TreeIndex = treeIndexGenerator.GetNextId(),
    //         Parent = null,
    //         Children = null
    //     };
    //
    //     var stopwatch = Stopwatch.StartNew();
    //
    //     var allGeometriesByRvmNode = rvmStore.RvmFiles
    //         .SelectMany(f => f.Model.Children)
    //         .SelectMany(GetAllNodesFlat)
    //         .ToDictionary(key => key, value =>
    //             value.Children.OfType<RvmPrimitive>()
    //                 .Select(x => RvmPrimitiveToAPrimitive.FromRvmPrimitive(0, x, value)
    //                 )
    //             );
    //
    //
    //
    //     var protoMeshesFromRvmPyramids = allGeometriesByRvmNode.
    //         nodes
    //             .SelectMany(x =>
    //                 x.Geometries.OfType<ProtoMeshFromRvmPyramid>())
    //             .ToArray();
    //
    //     // We have models where several pyramids on the same "part" are completely identical.
    //     var uniqueRvmPyramidProtoMeshes = protoMeshesFromRvmPyramids.Distinct().ToArray();
    //     if (uniqueRvmPyramidProtoMeshes.Length < protoMeshesFromRvmPyramids.Length)
    //     {
    //         var diffCount = protoMeshesFromRvmPyramids.Length - uniqueRvmPyramidProtoMeshes.Length;
    //         Console.WriteLine($"Found and ignored {diffCount} duplicate pyramids (including: position, mesh, parent, id, etc).");
    //     }
    //     // Match and Tessellate pyramids
    //     RvmPyramidInstancer.Result[] pyramidInstancingResult;
    //     if (/* composerParameters.NoInstancing */ false)
    //     {
    //         // pyramidInstancingResult = uniqueProtoMeshesFromPyramid
    //         //     .Select(x => new RvmPyramidInstancer.NotInstancedResult(x))
    //         //     .OfType<RvmPyramidInstancer.Result>()
    //         //     .ToArray();
    //         // Console.WriteLine("Pyramid instancing disabled.");
    //     }
    //     else
    //     {
    //         pyramidInstancingResult = RvmPyramidInstancer.Process(
    //             uniqueRvmPyramidProtoMeshes,
    //             pyramids => pyramids.Length >= 300); // TODO: modelParameters.InstancingThreshold.Value);
    //         Console.WriteLine($"Pyramids instance matched in {stopwatch.Elapsed}");
    //         stopwatch.Restart();
    //     }
    //
    //     nodes[0].Geometries[0]
    //
    //
    //     rootNode.Children = rvmStore.RvmFiles
    //         .SelectMany(f => f.Model.Children)
    //         .Select(root =>
    //             RvmNodeToCadRevealNodeConverter.CollectGeometryNodesRecursive(root, rootNode,
    //                 treeIndexGenerator, allGeometriesByRvmNode))
    //         .ToArray();
    //
    //     rootNode.BoundingBoxAxisAligned = rootNode.Children
    //         .Select(x => x.BoundingBoxAxisAligned)
    //         .WhereNotNull()
    //         .ToArray().Aggregate((a, b) => a.Encapsulate(b));
    //
    //     Debug.Assert(rootNode.BoundingBoxAxisAligned != null,
    //         "Root node has no bounding box. Are there any meshes in the input?");
    //
    //     var allNodes = GetAllNodesFlat(rootNode).ToArray();
    //     return allNodes;
    // }
    //
    //
    // private static IEnumerable<RvmNode> GetAllNodesFlat(RvmNode root)
    // {
    //     yield return root;
    //
    //     foreach (var rvmNode in root.Children.OfType<RvmNode>())
    //     {
    //         foreach (RvmNode rvmNodeChild in GetAllNodesFlat(rvmNode))
    //         {
    //             yield return rvmNodeChild;
    //         }
    //     }
    // }
    //
    // private static IEnumerable<CadRevealNode> GetAllNodesFlat(CadRevealNode root)
    // {
    //     yield return root;
    //
    //     if (root.Children == null)
    //     {
    //         yield break;
    //     }
    //
    //     foreach (CadRevealNode cadRevealNode in root.Children)
    //     {
    //         foreach (CadRevealNode revealNode in GetAllNodesFlat(cadRevealNode))
    //         {
    //             yield return revealNode;
    //         }
    //     }
    // }
    //
    //
    // private static APrimitive[] TessellateAndOutputInstanceMeshes(RvmPyramidInstancer.Result[] pyramidInstancingResult)
    // {
    //     static TriangleMesh TessellateAndCreateTriangleMesh(ProtoMesh p)
    //     {
    //         var mesh = Tessellate(p.RvmPrimitive);
    //         return new TriangleMesh(mesh, p.TreeIndex, p.Color, p.AxisAlignedBoundingBox);
    //     }
    //
    //     var pyramidsNotInstanced = pyramidInstancingResult
    //         .OfType<RvmPyramidInstancer.NotInstancedResult>()
    //         .Select(result => result.Pyramid)
    //         .Cast<ProtoMesh>()
    //         .ToArray();
    //
    //     var pyramidsInstanced = pyramidInstancingResult
    //         .OfType<RvmPyramidInstancer.InstancedResult>()
    //         .GroupBy(result => (RvmPrimitive)result.Template, x => (ProtoMesh: (ProtoMesh)x.Pyramid, x.Transform))
    //         .ToArray();
    //
    //     var stopwatch = Stopwatch.StartNew();
    //     var instancedPyramidMeshes =
    //         pyramidsInstanced
    //         .AsParallel()
    //         .Select(g => (InstanceGroup: g, Mesh: Tessellate(g.Key)))
    //         .Where(g => g.Mesh.Triangles.Length > 0) // ignore empty meshes
    //         .ToArray();
    //     var totalCount = instancedPyramidMeshes.Sum(m => m.InstanceGroup.Count());
    //     Console.WriteLine($"Tessellated {instancedPyramidMeshes.Length:N0} meshes for {totalCount:N0} instanced meshes in {stopwatch.Elapsed}");
    //
    //     var instancedMeshPrimitives = instancedPyramidMeshes
    //         .SelectMany((group, index) => group.InstanceGroup.Select(item => new InstancedMesh(
    //             InstanceId: index,
    //             group.Mesh,
    //             item.Transform,
    //             item.ProtoMesh.TreeIndex,
    //             item.ProtoMesh.Color,
    //             item.ProtoMesh.AxisAlignedBoundingBox)))
    //         .ToArray();
    //
    //     stopwatch.Restart();
    //     var triangleMeshes = pyramidsNotInstanced
    //         .AsParallel()
    //         .Select(TessellateAndCreateTriangleMesh)
    //         .Where(t => t.Mesh.Triangles.Length > 0) // ignore empty meshes
    //         .ToArray();
    //     Console.WriteLine($"Tessellated {triangleMeshes.Length:N0} triangle meshes in {stopwatch.Elapsed}");
    //
    //     return instancedMeshPrimitives.Cast<APrimitive>().Concat(triangleMeshes).ToArray();
    // }
    //
    // public static Mesh Tessellate(RvmPrimitive primitive)
    // {
    //     Mesh mesh;
    //     try
    //     {
    //         mesh = TessellatorBridge.Tessellate(primitive, 0f) ?? Mesh.Empty;
    //     }
    //     catch
    //     {
    //         mesh = Mesh.Empty;
    //     }
    //
    //     if (mesh.Vertices.Length == 0)
    //     {
    //         if (primitive is RvmFacetGroup f)
    //         {
    //             Console.WriteLine($"WARNING: Could not tessellate facet group! Polygon count: {f.Polygons.Length}");
    //         }
    //         else if (primitive is RvmPyramid)
    //         {
    //             Console.WriteLine("WARNING: Could not tessellate pyramid!");
    //         }
    //         else
    //         {
    //             throw new NotImplementedException();
    //         }
    //     }
    //
    //     return mesh;
    // }
}