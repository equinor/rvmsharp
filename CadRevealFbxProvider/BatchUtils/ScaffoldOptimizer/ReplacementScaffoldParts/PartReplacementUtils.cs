namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

public static class PartReplacementUtils
{
    public static EccentricCone? CreatePrimitiveCylinderPart(Mesh mesh)
    {
        // :TODO: In the below procedure we are assuming a cylinder mesh that is axis aligned.
        // Hence, at the moment we do not support non-axis aligned cylinders as input.
        var boundingBox = mesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
        var sortedBoundingBoxExtent = new SortedBoundingBoxExtent(boundingBox);
        var a = new Vector3
        {
            [sortedBoundingBoxExtent.AxisIndexOfLargest] = boundingBox.Min[sortedBoundingBoxExtent.AxisIndexOfLargest],
            [sortedBoundingBoxExtent.AxisIndexOfMiddle] =
                (
                    boundingBox.Max[sortedBoundingBoxExtent.AxisIndexOfMiddle]
                    + boundingBox.Min[sortedBoundingBoxExtent.AxisIndexOfMiddle]
                ) / 2.0f,
            [sortedBoundingBoxExtent.AxisIndexOfSmallest] =
                (
                    boundingBox.Max[sortedBoundingBoxExtent.AxisIndexOfSmallest]
                    + boundingBox.Min[sortedBoundingBoxExtent.AxisIndexOfSmallest]
                ) / 2.0f
        };
        var b = new Vector3
        {
            [sortedBoundingBoxExtent.AxisIndexOfLargest] = boundingBox.Max[sortedBoundingBoxExtent.AxisIndexOfLargest],
            [sortedBoundingBoxExtent.AxisIndexOfMiddle] =
                (
                    boundingBox.Max[sortedBoundingBoxExtent.AxisIndexOfMiddle]
                    + boundingBox.Min[sortedBoundingBoxExtent.AxisIndexOfMiddle]
                ) / 2.0f,
            [sortedBoundingBoxExtent.AxisIndexOfSmallest] =
                (
                    boundingBox.Max[sortedBoundingBoxExtent.AxisIndexOfSmallest]
                    + boundingBox.Min[sortedBoundingBoxExtent.AxisIndexOfSmallest]
                ) / 2.0f
        };
        var r = (sortedBoundingBoxExtent.ValueOfSmallest + sortedBoundingBoxExtent.ValueOfMiddle) / 4.0f;
        return CreatePrimitiveCylinderPart(a, b, r);
    }

    public static EccentricCone? CreatePrimitiveCylinderPart(Vector3 startPoint, Vector3 endPoint, float radius)
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
        return new EccentricCone(
            startPoint,
            endPoint,
            -unitNormal,
            radius,
            radius,
            0,
            Color.Brown,
            estimatedCylinderAxisAlignedBoundingBox
        );
    }

    public static TriangleMesh? TessellateCylinderPart(Vector3 startPoint, Vector3 endPoint, float radius)
    {
        // Create primitive eccentric cones
        var primitiveCylinder = CreatePrimitiveCylinderPart(startPoint, endPoint, radius);

        // Tessellate the eccentric cones
        return (primitiveCylinder != null) ? EccentricConeTessellator.Tessellate(primitiveCylinder) : null;
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

    public static int FindIndexWithLargestBoundingBoxVolume(List<Mesh?> meshList)
    {
        return meshList
            .Select((m, idx) => new { m, i = idx })
            .OrderByDescending(v =>
            {
                Vector3 ext =
                    (v.m != null)
                        ? v.m.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity).Extents
                        : new Vector3(0.0f, 0.0f, 0.0f);
                return ext.X * ext.Y * ext.Z; // Volume
            })
            .First()
            .i;
    }
}
