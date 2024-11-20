namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using System.Linq;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

static class ScaffoldOptimizerExtensions
{
    public static bool ContainsAny(this string str, string[] keywordList) => keywordList.Any(str.Contains);
}

public class ScaffoldOptimizerBase
{
    private static Mesh?[] ExtractMeshes(APrimitive[] primitives)
    {
        var meshes = new List<Mesh?>();
        foreach (APrimitive primitive in primitives)
        {
            Mesh? mesh = primitive switch
            {
                InstancedMesh instancedMesh => instancedMesh.TemplateMesh,
                TriangleMesh triangleMesh => triangleMesh.Mesh,
                _ => null
            };

            meshes.Add(mesh);
        }

        return meshes.ToArray();
    }

    public void OptimizeNodes(List<CadRevealNode> nodes, Func<ulong> requestNewInstanceId)
    {
        foreach (CadRevealNode node in nodes)
        {
            OptimizeNode(node, requestNewInstanceId);
        }
        GeometryInstancer.Invoke(nodes);
    }

    public void OptimizeNode(CadRevealNode node, Func<ulong> requestNewInstanceId)
    {
        Mesh?[] meshes = ExtractMeshes(node.Geometries);
        if (meshes.All(u => u == null))
        {
            // If we do not find any meshes in the primitive, then we have nothing to optimize and should not update the node primitives
            return;
        }

        var results = OptimizeNode(node.Name, meshes, node.Geometries, RequestChildMeshInstanceId);
        if (results == null)
        {
            // If we do not have a result from the optimization, then we have not optimized anything and should not update the node primitives
            return;
        }

        var primitiveList = new List<APrimitive>();
        primitiveList.AddRange(results.Select(result => result.Get()));
        node.Geometries = primitiveList.ToArray();

        return;
        ulong RequestChildMeshInstanceId(ulong instanceIdParentMesh, int indexChildMesh) =>
            OnRequestChildMeshInstanceId(instanceIdParentMesh, indexChildMesh, requestNewInstanceId);
    }

    protected virtual List<ScaffoldOptimizerResult>? OptimizeNode(
        string nodeName,
        Mesh?[] meshes,
        APrimitive[] nodeGeometries,
        Func<ulong, int, ulong> requestChildMeshInstanceId
    )
    {
        throw new NotImplementedException();
    }

    private ulong OnRequestChildMeshInstanceId(
        ulong instanceIdParentMesh,
        int indexChildMesh,
        Func<ulong> requestNewInstanceId
    )
    {
        if (_childMeshInstanceIdLookup.TryGetValue(instanceIdParentMesh, out Dictionary<int, ulong>? retInstanceIdDict))
        {
            if (retInstanceIdDict.TryGetValue(indexChildMesh, out ulong retInstanceId))
            {
                return retInstanceId;
            }

            _childMeshInstanceIdLookup[instanceIdParentMesh].Add(indexChildMesh, requestNewInstanceId());
            return _childMeshInstanceIdLookup[instanceIdParentMesh][indexChildMesh];
        }

        _childMeshInstanceIdLookup.Add(instanceIdParentMesh, new Dictionary<int, ulong>());
        _childMeshInstanceIdLookup[instanceIdParentMesh].Add(indexChildMesh, requestNewInstanceId());
        return _childMeshInstanceIdLookup[instanceIdParentMesh][indexChildMesh];
    }

    private readonly Dictionary<ulong, Dictionary<int, ulong>> _childMeshInstanceIdLookup = new();
}
