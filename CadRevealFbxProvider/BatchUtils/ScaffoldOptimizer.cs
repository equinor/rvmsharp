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

    public void OptimizeNode(CadRevealNode node)
    {
        var name = node.Name;

        var newGeometries = new List<APrimitive>();
        foreach (APrimitive primitive in node.Geometries)
        {
            APrimitive[]? newPrimitiveList = OptimizePrimitive(primitive, name);
            newGeometries.AddRange(newPrimitiveList ?? [primitive]);
        }

        node.Geometries = newGeometries.ToArray();
    }

    private APrimitive[]? OptimizePrimitive(APrimitive primitive, string name)
    {
        // Handle only primitives that have their own Mesh objects, then pull out the mesh and optimize. These are the ones that need to be optimized.
        var primitiveList = new List<APrimitive>();
        switch (primitive)
        {
            case InstancedMesh instancedMesh:
            {
                Mesh[] meshes = OptimizeMesh(instancedMesh.TemplateMesh, name);
                primitiveList.AddRange(
                    meshes.Select(mesh => new InstancedMesh(
                        instancedMesh.InstanceId,
                        mesh,
                        instancedMesh.InstanceMatrix,
                        instancedMesh.TreeIndex,
                        instancedMesh.Color,
                        instancedMesh.AxisAlignedBoundingBox
                    ))
                );
                break;
            }
            case TriangleMesh triangleMesh:
            {
                Mesh[] meshes = OptimizeMesh(triangleMesh.Mesh, name);
                primitiveList.AddRange(
                    meshes.Select(mesh => new TriangleMesh(
                        mesh,
                        triangleMesh.TreeIndex,
                        triangleMesh.Color,
                        triangleMesh.AxisAlignedBoundingBox
                    ))
                );
                break;
            }
        }

        return primitiveList.Count > 0 ? primitiveList.ToArray() : null;
    }

    private Mesh[] OptimizeMesh(Mesh mesh, string name)
    {
        Mesh[] optimizedMesh = [mesh];
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
                    optimizedMesh = partOptimizer.Optimize(mesh);
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

        return optimizedMesh;
    }

    private readonly List<IScaffoldPartOptimizer> _partOptimizers =
    [
        // :TODO: Fill in the available part optimizers here
    ];
}
