namespace CadRevealFbxProvider.BatchUtils;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System.Linq;
using System.Numerics;

// :TODO: Move all ScaffoldPartOptimizer based classes to their own files!!!
public abstract class ScaffoldPartOptimizer
{
    public abstract string GetName();
    public abstract Mesh[] Optimize(Mesh mesh);
    public abstract string[] GetPartNameTriggerKeywords();
}

public abstract class ScaffoldPartOptimizerTest : ScaffoldPartOptimizer
{
    public abstract List<Vector3> GetVerticesTruth();
    public abstract List<uint> GetIndicesTruth();
}

public class ScaffoldPartOptimizerTestPartA : ScaffoldPartOptimizerTest
{
    public override List<Vector3> GetVerticesTruth()
    {
        return
        [
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f)
        ];
    }

    public override List<uint> GetIndicesTruth()
    {
        return [1, 0, 2];
    }

    public override string GetName()
    {
        return "Part A test optimizer";
    }

    public override Mesh[] Optimize(Mesh mesh)
    {
        return [new Mesh(GetVerticesTruth().ToArray(), GetIndicesTruth().ToArray(), mesh.Error)];
    }

    public override string[] GetPartNameTriggerKeywords()
    {
        return [ "Test A" ];
    }
}

public class ScaffoldPartOptimizerTestPartB : ScaffoldPartOptimizerTest
{
    public override List<Vector3> GetVerticesTruth()
    {
        return
        [
            new Vector3(3.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 4.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 5.0f)
        ];
    }

    public override List<uint> GetIndicesTruth()
    {
        return [2, 0, 1];
    }

    public override string GetName()
    {
        return "Part A test optimizer";
    }
    public override Mesh[] Optimize(Mesh mesh)
    {
        return [new Mesh(GetVerticesTruth().ToArray(), GetIndicesTruth().ToArray(), mesh.Error)];
    }

    public override string[] GetPartNameTriggerKeywords()
    {
        return [ "Test B", "Another Test" ];
    }
}

public static class ScaffoldOptimizer
{
    public static void AddPartOptimizer(ScaffoldPartOptimizer optimizer)
    {
        PartOptimizers.Add(optimizer);
    }

    public static void OptimizeNode(CadRevealNode node)
    {
        var name = node.Name;

        var newGeometries = new List<APrimitive>();
        foreach (APrimitive primitive in node.Geometries)
        {
            newGeometries.AddRange(OptimizePrimitive(primitive, name));
        }

        node.Geometries = newGeometries.ToArray();
    }

    private static APrimitive[] OptimizePrimitive(APrimitive primitive, string name)
    {
        // Handle only primitives that have their own Mesh objects, then pull out the mesh and optimize. These are the ones that need to be optimized.
        var primitiveList = new List<APrimitive>();
        switch (primitive)
        {
            case InstancedMesh instancedMesh:
                {
                    Mesh[] meshes = OptimizeMesh(instancedMesh.TemplateMesh, name);
                    primitiveList.AddRange(meshes.Select(mesh => new InstancedMesh(instancedMesh.InstanceId, mesh,
                        instancedMesh.InstanceMatrix, instancedMesh.TreeIndex, instancedMesh.Color, instancedMesh.AxisAlignedBoundingBox)).Cast<APrimitive>());
                    break;
                }
            case TriangleMesh triangleMesh:
                {
                    Mesh[] meshes = OptimizeMesh(triangleMesh.Mesh, name);
                    primitiveList.AddRange(meshes.Select(mesh => new TriangleMesh(mesh, triangleMesh.TreeIndex, triangleMesh.Color,
                        triangleMesh.AxisAlignedBoundingBox)));
                    break;
                }
        }

        return primitiveList.ToArray();
    }

    private static Mesh[] OptimizeMesh(Mesh mesh, string name)
    {
        Mesh[] optimizedMesh = [mesh];
        var triggeredOptimizers = new List<ScaffoldPartOptimizer>();
        foreach (ScaffoldPartOptimizer partOptimizer in PartOptimizers)
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
                if (triggeredOptimizers.Count == 0) optimizedMesh = partOptimizer.Optimize(mesh);
                triggeredOptimizers.Add(partOptimizer);
            }
        }

        if (triggeredOptimizers.Count > 1)
        {
            Console.WriteLine($"Warning, the '{name}' scaffold part triggered {triggeredOptimizers.Count} optimizers, where only the first was applied:");
            foreach (ScaffoldPartOptimizer partOptimizer in triggeredOptimizers)
            {
                Console.WriteLine($"    * Optimizer named '{partOptimizer.GetName()}' which triggers on:");
                foreach (string partNameTriggerKeyword in partOptimizer.GetPartNameTriggerKeywords())
                {
                    Console.WriteLine($"        - String '{partNameTriggerKeyword}'");
                }
            }
        }

        return optimizedMesh;
    }

    private static readonly List<ScaffoldPartOptimizer> PartOptimizers =
        [
            // :TODO: Fill in the available part optimizers here
        ];
}
