namespace CadRevealFbxProvider.BatchUtils.ScaffoldPartOptimizers;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

public class ScaffoldOptimizerResult : IScaffoldOptimizerResult
{
    public ScaffoldOptimizerResult(
        APrimitive basePrimitive,
        Mesh optimizedMesh,
        int indexChildMesh,
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
                    optimizedMesh.CalculateAxisAlignedBoundingBox()
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
