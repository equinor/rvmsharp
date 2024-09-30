namespace CadRevealFbxProvider.BatchUtils;

using System.Linq;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using ScaffoldPartOptimizers;

public class ScaffoldOptimizer
{
    public void AddPartOptimizer(IScaffoldPartOptimizer optimizer)
    {
        _partOptimizers.Add(optimizer);
    }

    public void OptimizeNode(CadRevealNode node, Func<ulong> requestNewInstanceId)
    {
        var partName = node.Name;

        var newGeometries = new List<APrimitive>();
        foreach (APrimitive primitive in node.Geometries)
        {
            APrimitive[]? newPrimitiveList = OptimizePrimitive(primitive, partName, requestNewInstanceId);
            newGeometries.AddRange(newPrimitiveList ?? [primitive]);
        }

        node.Geometries = newGeometries.ToArray();
    }

    private APrimitive[]? OptimizePrimitive(APrimitive primitive, string partName, Func<ulong> requestNewInstanceId)
    {
        // Handle only primitives that have their own Mesh objects, then pull out the mesh and optimize. These are the ones that need to be optimized.
        var primitiveList = new List<APrimitive>();
        switch (primitive)
        {
            case InstancedMesh instancedMesh:
            {
                OptimizeMeshAndAddResult(instancedMesh.TemplateMesh);
                break;
            }
            case TriangleMesh triangleMesh:
            {
                OptimizeMeshAndAddResult(triangleMesh.Mesh);
                break;
            }
        }

        return primitiveList.Count > 0 ? primitiveList.ToArray() : null;

        void OptimizeMeshAndAddResult(Mesh mesh)
        {
            IScaffoldOptimizerResult[] results = OptimizeMesh(primitive, mesh, partName, requestNewInstanceId);
            primitiveList.AddRange(results.Select(result => result.Get()));
        }
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

    private IScaffoldOptimizerResult[] OptimizeMesh(
        APrimitive basePrimitive,
        Mesh mesh,
        string name,
        Func<ulong> requestNewInstanceId
    )
    {
        var onRequestChildMeshInstanceId = (ulong instanceIdParentMesh, int indexChildMesh) =>
            OnRequestChildMeshInstanceId(instanceIdParentMesh, indexChildMesh, requestNewInstanceId);

        IScaffoldOptimizerResult[] optimizedResult =
        [
            new ScaffoldOptimizerResult(basePrimitive, mesh, 0, onRequestChildMeshInstanceId)
        ];
        var triggeredOptimizers = new List<IScaffoldPartOptimizer>();
        foreach (IScaffoldPartOptimizer partOptimizer in _partOptimizers)
        {
            bool partNameContainsPartOptimizerTrigger = false;
            foreach (string partNameTriggerKeyword in partOptimizer.GetPartNameTriggerKeywords())
            {
                if (name.Contains(partNameTriggerKeyword))
                {
                    partNameContainsPartOptimizerTrigger = true;
                }
            }

            if (partNameContainsPartOptimizerTrigger)
            {
                if (triggeredOptimizers.Count == 0)
                {
                    optimizedResult = partOptimizer.Optimize(basePrimitive, mesh, onRequestChildMeshInstanceId);
                }
                triggeredOptimizers.Add(partOptimizer);
            }
        }

        if (triggeredOptimizers.Count > 1)
        {
            Console.WriteLine(
                $"Warning, the '{name}' scaffold part triggered {triggeredOptimizers.Count} optimizers, where only the first was applied:"
            );

            foreach (IScaffoldPartOptimizer partOptimizer in triggeredOptimizers)
            {
                Console.WriteLine($"    * Optimizer named '{partOptimizer.Name}' which triggers on:");
                foreach (string partNameTriggerKeyword in partOptimizer.GetPartNameTriggerKeywords())
                {
                    Console.WriteLine($"        - String '{partNameTriggerKeyword}'");
                }
            }
        }

        return optimizedResult;
    }

    private readonly Dictionary<ulong, Dictionary<int, ulong>> _childMeshInstanceIdLookup =
        new Dictionary<ulong, Dictionary<int, ulong>>();
    private readonly List<IScaffoldPartOptimizer> _partOptimizers =
    [
        // :TODO: Fill in the available part optimizers here
    ];
}
