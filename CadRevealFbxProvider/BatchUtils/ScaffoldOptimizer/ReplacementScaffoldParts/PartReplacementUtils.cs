namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using g3;
using SharpGLTF.Schema2;

public static class PartReplacementUtils
{
    public static (TriangleMesh? front, TriangleMesh? back) TessellateCylinderPart(
        Vector3 startPoint,
        Vector3 endPoint,
        float radius
    )
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

    public static TriangleMesh? TessellateBoxPart(
        Vector3 startPoint,
        Vector3 endPoint,
        Vector3 surfaceDirGuide,
        float thickness,
        float height
    )
    {
        // Calculate equivalent scale vector with length along X
        Vector3 lengthVec = endPoint - startPoint;
        float length = lengthVec.Length();
        var equivalentAxisAlignedScaleVec = new Vector3(length, height, thickness);

        // Create a rotation, alpha, based on rotation axis, u, that will rotate the axis aligned box, which length is along the x-axis,
        // such that the points along the x-axis is rotated to align with lengthVec. To this end, u = x cross lengthVec, where x = (1, 0, 0)
        // and cos(alpha) = x dot lengthVec / (|x||lengthVec|) = lengthVec.X/|lengthVec|.
        // If x and lengthVec are close to parallel (i.e., if n.Length is close to zero), then no rotation should be done.
        if (length == 0.0f)
            return null;
        Vector3 u = Vector3.Cross(new Vector3(1, 0, 0), lengthVec);
        float uLength = u.Length();
        Vector3 uUnit = u / uLength;
        float cosAlpha = lengthVec.X / length;
        double alpha = Math.Acos((cosAlpha > 1.0f ? 1.0f : cosAlpha) < -1.0f ? -1.0f : cosAlpha);
        Quaternion rot1 =
            (uLength > 1E-12f) ? Quaternion.CreateFromAxisAngle(uUnit, (float)alpha) : Quaternion.Identity;

        // Perform a second rotation about the central axis (i.e., lengthVec) in such a way that the surface of the beam box points
        // in the general direction of the surfaceDirGuide vector. First step is to find the current normal vector, n, of the beam
        // box. Do this by creating a vector (0, 0, 1), which is the normal vector of the beam box before rotation, subsequently
        // apply the rotation to this, which yields n. The second step is to find the unit vectors uUnit and nUnit that makes a coordinate
        // system spanning the beam box end surfaces. Project both n and surfaceDirGuide into that plane and find the angle, theta, between
        // these. This is the angle to rotate about lengthVec.
        var rot1Quaternion = Matrix4x4.CreateFromQuaternion(rot1);
        var scaleAndRot1 = Matrix4x4.CreateScale(equivalentAxisAlignedScaleVec) * rot1Quaternion;

        Vector3 nUnit = Vector3.Transform(new Vector3(0, 0, 1), rot1Quaternion);
        Vector3 v = Vector3.Cross(lengthVec, nUnit);
        Vector3 vUnit = v / v.Length();

        Vector2 nProj = new(0, 1);
        Vector2 surfaceDirGuideProj = new(Vector3.Dot(surfaceDirGuide, vUnit), Vector3.Dot(surfaceDirGuide, nUnit));

        float nProjSurfaceDirGuideProj = (nProj.Length() * surfaceDirGuideProj.Length());
        double cosTheta =
            (nProjSurfaceDirGuideProj > 1E-12f)
                ? Vector2.Dot(nProj, surfaceDirGuideProj) / nProjSurfaceDirGuideProj
                : 1.0f;
        float theta = (float)Math.Acos((cosTheta > 1.0f ? 1.0f : cosTheta) < -1.0f ? -1.0f : cosTheta);

        // Need to rotate about a rotation axis which is perpendicular to the surface spanned by nUnit and the projection of the
        // guide vector, which is nUnit cross ((guide*vUnit)vUnit + (guide*nUnit)*nUnit). This will point in opposite directions
        // depending on the rotation direction.
        Vector3 rotAxis = Vector3.Cross(
            nUnit,
            Vector3.Dot(surfaceDirGuide, vUnit) * vUnit + Vector3.Dot(surfaceDirGuide, nUnit) * nUnit
        );
        Quaternion rot2 =
            (rotAxis.LengthSquared() > 1E-12f)
                ? Quaternion.CreateFromAxisAngle(rotAxis / rotAxis.Length(), theta)
                : Quaternion.Identity;

        // Calculate translation needed to move the object s.t., minimum X is at startPoint and maximum X at endPoint, while
        // Y and Z are centered, considering that the initial position, without translation, has the object center at the world origin
        Vector3 translation = (startPoint + endPoint) * 0.5f;

        // Create the instance matrix
        var instanceMatrix =
            scaleAndRot1 * Matrix4x4.CreateFromQuaternion(rot2) * Matrix4x4.CreateTranslation(translation);

        // Tessellate box, where the box is a unit box, scaled, rotated, and translated
        TriangleMesh? mesh = BoxTessellator.Tessellate(
            new Box(instanceMatrix, 0, Color.Black, new BoundingBox(startPoint, endPoint))
        );

        return mesh;
    }
}
