namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;

public static class PartReplacementUtils
{
    public static Box ToBoxPrimitive(this Mesh meshToBound, Matrix4x4 transform, uint treeIndex, Color color)
    {
        transform.DecomposeAndNormalize(out Vector3 scale, out Quaternion rot, out Vector3 trans);

        BoundingBox boundingBoxTransformed = meshToBound.CalculateAxisAlignedBoundingBox(transform);
        BoundingBox boundingBox = meshToBound.CalculateAxisAlignedBoundingBox(Matrix4x4.CreateScale(scale));

        var instanceMatrix =
            Matrix4x4.CreateScale(boundingBox.Extents)
            * Matrix4x4.CreateFromQuaternion(rot)
            * Matrix4x4.CreateTranslation(boundingBoxTransformed.Center);
        return new Box(instanceMatrix, treeIndex, color, boundingBoxTransformed);
    }

    public static Box? ToBoxPrimitive(this Mesh meshToBound, APrimitive geometryWithTransform, float heightThreshold)
    {
        var matrix = (geometryWithTransform as InstancedMesh)?.InstanceMatrix ?? Matrix4x4.Identity;
        Box box = meshToBound.ToBoxPrimitive(matrix, geometryWithTransform.TreeIndex, geometryWithTransform.Color);

        // :TODO: In the above procedure we are assuming a meshToBound that is axis aligned.
        // Hence, at the moment we do not support non-axis aligned cylinders as input. To detect
        // if the mesh may have been non-axis aligned we will set a threshold on the mesh
        // height to see if it is unnaturally tall. If so, we return null as failure. Remove
        // this test once we support non-axis aligned input.
        var sortedBoundingBoxExtent = new SortedBoundingBoxExtent(meshToBound.CalculateAxisAlignedBoundingBox());
        return sortedBoundingBoxExtent.ValueOfMiddle > heightThreshold ? null : box;
    }

    public static (EccentricCone? cylinder, Circle? startCap, Circle? endCap) ToCylinderPrimitive(
        this Mesh mesh,
        uint treeIndex
    )
    {
        // :TODO: In the below procedure we are assuming a cylinder mesh that is axis aligned.
        // Hence, at the moment we do not support non-axis aligned cylinders as input.
        // This also means that, if we want to support non-axis aligned meshes, we need to
        // apply any matrix transforms from instanced meshes before calculating a cylinders center axis.
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
        return CreateCylinderPrimitive(a, b, r, treeIndex);
    }

    public static (TriangleMesh? cylinder, TriangleMesh? startCap, TriangleMesh? endCap) ToTessellatedCylinderPrimitive(
        this Mesh mesh,
        uint treeIndex,
        bool createCaps = false
    )
    {
        // Create primitive eccentric cones
        var primitiveCylinder = mesh.ToCylinderPrimitive(treeIndex);

        // Tessellate the eccentric cones
        var cylinderMesh =
            (primitiveCylinder.cylinder != null)
                ? EccentricConeTessellator.Tessellate(primitiveCylinder.cylinder)
                : null;

        // Tessellate circles representing the cylinder caps
        var startCapsMesh =
            (primitiveCylinder.startCap != null && createCaps)
                ? CircleTessellator.Tessellate(primitiveCylinder.startCap)
                : null;
        var endCapsMesh =
            (primitiveCylinder.endCap != null && createCaps)
                ? CircleTessellator.Tessellate(primitiveCylinder.endCap)
                : null;

        return (cylinderMesh, startCapsMesh, endCapsMesh);
    }

    public static TriangleMesh? CreateTessellatedBoxPrimitive(
        Vector3 startPoint,
        Vector3 endPoint,
        Vector3 surfaceDirGuide,
        float thickness,
        float height,
        uint treeIndex
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
            new Box(instanceMatrix, treeIndex, Color.Black, new BoundingBox(startPoint, endPoint))
        );

        return mesh;
    }

    public static (EccentricCone? cylinder, Circle? startCap, Circle? endCap) CreateCylinderPrimitive(
        Vector3 startPoint,
        Vector3 endPoint,
        float radius,
        uint treeIndex
    )
    {
        // Estimate an axis aligned bounding box for the cylinder
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

        // Create cylinder from an eccentric cone
        Vector3 lengthVec = endPoint - startPoint;
        Vector3 unitNormal = lengthVec * (1.0f / lengthVec.Length());
        var cylinder = new EccentricCone(
            startPoint,
            endPoint,
            -unitNormal,
            radius,
            radius,
            treeIndex,
            Color.Brown,
            estimatedCylinderAxisAlignedBoundingBox
        );

        // Create caps for the cylinder
        var scale = Matrix4x4.CreateScale(2.0f * radius);
        var startCap = new Circle(
            scale * Matrix4x4.CreateTranslation(startPoint),
            -unitNormal,
            treeIndex,
            Color.Brown,
            estimatedCylinderAxisAlignedBoundingBox
        );
        var endCap = new Circle(
            scale * Matrix4x4.CreateTranslation(endPoint),
            unitNormal,
            treeIndex,
            Color.Brown,
            estimatedCylinderAxisAlignedBoundingBox
        );

        return (cylinder, startCap, endCap);
    }

    public static (
        TriangleMesh? cylinder,
        TriangleMesh? startCap,
        TriangleMesh? endCap
    ) CreateTessellatedCylinderPrimitive(
        Vector3 startPoint,
        Vector3 endPoint,
        float radius,
        uint treeIndex,
        bool createCaps = false
    )
    {
        // Create primitive eccentric cones
        var primitiveCylinder = CreateCylinderPrimitive(startPoint, endPoint, radius, treeIndex);

        // Tessellate the eccentric cones
        var cylinderMesh =
            (primitiveCylinder.cylinder != null)
                ? EccentricConeTessellator.Tessellate(primitiveCylinder.cylinder)
                : null;

        // Tessellate circles representing the cylinder caps
        var startCapsMesh =
            (primitiveCylinder.startCap != null && createCaps)
                ? CircleTessellator.Tessellate(primitiveCylinder.startCap)
                : null;
        var endCapsMesh =
            (primitiveCylinder.endCap != null && createCaps)
                ? CircleTessellator.Tessellate(primitiveCylinder.endCap)
                : null;

        return (cylinderMesh, startCapsMesh, endCapsMesh);
    }

    public static Mesh? FindMeshWithLargestBoundingBoxVolume(List<Mesh?> meshList)
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
            .m;
    }
}
