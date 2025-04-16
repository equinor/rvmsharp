namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using System.Numerics;
using CadRevealComposer;
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

    public ScaffoldOptimizerResult(APrimitive basePrimitive, APrimitive optimizedPrimitive)
    {
        _optimizedPrimitive = optimizedPrimitive;
        var instanceMatrix = (basePrimitive as InstancedMesh)?.InstanceMatrix ?? Matrix4x4.Identity;

        switch (optimizedPrimitive)
        {
            case EccentricCone eccentricCone:
                Vector3 centerA = Vector3.Transform(eccentricCone.CenterA, instanceMatrix);
                Vector3 centerB = Vector3.Transform(eccentricCone.CenterB, instanceMatrix);
                var newEccentricCone = new EccentricCone(
                    centerA,
                    centerB,
                    TransformNormalVector(eccentricCone.Normal, instanceMatrix),
                    TransformCylinderRadius(
                        eccentricCone.RadiusA,
                        instanceMatrix,
                        eccentricCone.CenterB,
                        eccentricCone.CenterA
                    ),
                    TransformCylinderRadius(
                        eccentricCone.RadiusB,
                        instanceMatrix,
                        eccentricCone.CenterB,
                        eccentricCone.CenterA
                    ),
                    eccentricCone.TreeIndex,
                    eccentricCone.Color,
                    TransformBoundingBox(eccentricCone.AxisAlignedBoundingBox, instanceMatrix)
                );
                _optimizedPrimitive = newEccentricCone;
                break;
            case Circle circle:
                var newCircle = new Circle(
                    circle.InstanceMatrix * instanceMatrix,
                    TransformNormalVector(circle.Normal, instanceMatrix),
                    circle.TreeIndex,
                    circle.Color,
                    TransformBoundingBox(circle.AxisAlignedBoundingBox, instanceMatrix)
                );
                _optimizedPrimitive = newCircle;
                break;
        }
    }

    private static Vector3 TransformNormalVector(Vector3 normal, Matrix4x4 transform)
    {
        return Vector3.Normalize(
            Vector3.Transform(normal, transform) - Vector3.Transform(new Vector3(0, 0, 0), transform)
        );
    }

    private static float TransformCylinderRadius(
        float r,
        Matrix4x4 transform,
        Vector3 cylinderBottom,
        Vector3 cylinderTop
    )
    {
        var cylAxis = cylinderTop - cylinderBottom;
        var x = new Vector3(1, 0, 0);
        var y = new Vector3(0, 1, 0);
        var z = new Vector3(0, 0, 1);
        var n = Vector3.Cross(x, cylAxis);
        if (n.Length() == 0.0)
            n = Vector3.Cross(y, cylAxis);
        if (n.Length() == 0.0)
            n = Vector3.Cross(z, cylAxis);

        var rVec = Vector3.Normalize(n) * r;
        var p1 = Vector3.Transform(cylinderTop, transform);
        var p2 = Vector3.Transform(cylinderTop + rVec, transform);
        var rNew = p2 - p1;
        return rNew.Length();
    }

    private static BoundingBox TransformBoundingBox(BoundingBox boundingBox, Matrix4x4 transform)
    {
        var unorderedBoundingBox = new BoundingBox(
            Vector3.Transform(boundingBox.Min, transform),
            Vector3.Transform(boundingBox.Max, transform)
        );

        return new BoundingBox(
            new Vector3(
                Math.Min(unorderedBoundingBox.Min.X, unorderedBoundingBox.Max.X),
                Math.Min(unorderedBoundingBox.Min.Y, unorderedBoundingBox.Max.Y),
                Math.Min(unorderedBoundingBox.Min.Z, unorderedBoundingBox.Max.Z)
            ),
            new Vector3(
                Math.Max(unorderedBoundingBox.Min.X, unorderedBoundingBox.Max.X),
                Math.Max(unorderedBoundingBox.Min.Y, unorderedBoundingBox.Max.Y),
                Math.Max(unorderedBoundingBox.Min.Z, unorderedBoundingBox.Max.Z)
            )
        );
    }

    public APrimitive Get()
    {
        return _optimizedPrimitive;
    }

    private readonly APrimitive _optimizedPrimitive;
}
