namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;
using System.Numerics;
using System.Drawing;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Operations.Tessellating;
using SharpGLTF.Schema2;

public static class PartReplacementUtils
{
    static public (TriangleMesh? front, TriangleMesh? back) TessellateCylinderPart(Vector3 startPoint, Vector3 endPoint, float radius)
    {
        // Estimate an axis aligned bounding box for the ledger beam cylinder
        var radiusVec = new Vector3(radius, radius, radius);
        Vector3 minPoint = new Vector3(
            Math.Min(startPoint.X, endPoint.X),
            Math.Min(startPoint.Y, endPoint.Y),
            Math.Min(startPoint.Z, endPoint.Z)
        );
        minPoint -= radiusVec;
        Vector3 maxPoint = new Vector3(
            Math.Max(startPoint.X, endPoint.X),
            Math.Max(startPoint.Y, endPoint.Y),
            Math.Max(startPoint.Z, endPoint.Z)
        );
        maxPoint += radiusVec;
        var estimatedCylinderAxisAlignedBoundingBox = new BoundingBox(minPoint, maxPoint);

        // Create cylinders from eccentric cones
        Vector3 lengthVec = endPoint - startPoint;
        Vector3 unitNormal = lengthVec * (1.0f / lengthVec.Length());
        EccentricCone coneFront = new EccentricCone(
            startPoint,
            endPoint,
            unitNormal,
            radius,
            radius,
            0,
            Color.Brown,
            estimatedCylinderAxisAlignedBoundingBox
        );
        EccentricCone coneBack = new EccentricCone(
            startPoint,
            endPoint,
            -unitNormal,
            radius,
            radius,
            0,
            Color.Brown,
            estimatedCylinderAxisAlignedBoundingBox
        );

        // Tessellate the eccentric cones
        var cylinderFront = EccentricConeTessellator.Tessellate(coneFront);
        var cylinderBack = EccentricConeTessellator.Tessellate(coneBack);

        return (cylinderFront, cylinderBack);
    }

    static public TriangleMesh? TessellateBoxPart(Vector3 startPoint, Vector3 endPoint, float thickness, float height)
    {
        // Calculate equivalent scale vector with length along X
        Vector3 lengthVec = endPoint - startPoint;
        float length = lengthVec.Length();
        var equivalentAxisAlignedScaleVec = new Vector3(length, height, thickness);

        // Calculate translation needed to move the object s.t., minimum X is at startPoint, while Y and Z are centered, considering
        // that the initial position, without translation, has the object center at the world origin
        Vector3 translation = new Vector3(length * 0.5f, 0.0f, 0.0f) + startPoint;

        // Create the instance matrix
        var instanceMatrix =
            Matrix4x4.CreateScale(equivalentAxisAlignedScaleVec)
//            * Matrix4x4.CreateFromQuaternion(rot)
            * Matrix4x4.CreateTranslation(translation);

        // Tessellate box, where the box is a unit box, scaled, rotated, and translated
        TriangleMesh? mesh = BoxTessellator.Tessellate
        (
            new Box
                (
                    instanceMatrix,
                    0,
                    Color.Black,
                    new BoundingBox(startPoint, endPoint)
                )
        );

        return mesh;
    }
}
