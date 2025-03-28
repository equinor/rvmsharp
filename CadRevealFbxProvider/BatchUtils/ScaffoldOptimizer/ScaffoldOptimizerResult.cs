namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

public class ScaffoldOptimizerResult : IScaffoldOptimizerResult
{
    public ScaffoldOptimizerResult(
        APrimitive basePrimitive, // When we split a primitive/mesh into several pieces, this is the origin of that split (before splitting)
        Mesh optimizedMesh, // This is the optimized mesh. In case of splitting into several pieces, there will be multiple ScaffoldOptimizerResult instances with the same basePrimitive, but different indexChildMesh
        int indexChildMesh, // This is the zero based index of the meshes that are produced during a mesh splitting
        Func<ulong, int, ulong> requestChildMeshInstanceId
    )
    {
        switch (basePrimitive)
        {
            case InstancedMesh instancedMesh:
                ulong instanceId = requestChildMeshInstanceId(instancedMesh.InstanceId, indexChildMesh);
                _optimizedPrimitive = new InstancedMesh(
                    instanceId,
                    optimizedMesh,
                    instancedMesh.InstanceMatrix,
                    instancedMesh.TreeIndex,
                    instancedMesh.Color,
                    optimizedMesh.CalculateAxisAlignedBoundingBox(instancedMesh.InstanceMatrix)
                );
                return;
            case TriangleMesh triangleMesh:
                _optimizedPrimitive = new TriangleMesh(
                    optimizedMesh,
                    triangleMesh.TreeIndex,
                    triangleMesh.Color,
                    optimizedMesh.CalculateAxisAlignedBoundingBox()
                );
                return;
        }

        _optimizedPrimitive = basePrimitive;
    }

    public ScaffoldOptimizerResult(APrimitive optimizedPrimitive)
    {
        _optimizedPrimitive = optimizedPrimitive;
    }

    public APrimitive Get()
    {
        return _optimizedPrimitive;
    }

    private readonly APrimitive _optimizedPrimitive;
}
