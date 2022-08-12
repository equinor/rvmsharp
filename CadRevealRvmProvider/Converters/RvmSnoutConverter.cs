namespace CadRevealRvmProvider.Converters;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

public static class RvmSnoutConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmSnout rvmSnout,
        ulong treeIndex,
        Color color)
    {
        if (!rvmSnout.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSnout.Matrix);
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        var (normal, _) = rotation.DecomposeQuaternion();

        var bbox = rvmSnout.CalculateAxisAlignedBoundingBox().ToCadRevealBoundingBox();

        var height = scale.Z * MathF.Sqrt(
            rvmSnout.Height * rvmSnout.Height +
            rvmSnout.OffsetX * rvmSnout.OffsetX +
            rvmSnout.OffsetY * rvmSnout.OffsetY);
        var halfHeight = 0.5f * height;

        var radiusA = rvmSnout.RadiusTop * scale.X;
        var radiusB = rvmSnout.RadiusBottom * scale.X;

        var centerA = position + normal * halfHeight;
        var centerB = position - normal * halfHeight;

        if (HasShear(rvmSnout))
        {
            if (IsEccentric(rvmSnout))
            {
                throw new NotImplementedException(
                    "This type of primitive is missing from CadReveal, should convert to mesh?");
            }

            var isCylinderShaped = rvmSnout.RadiusTop.ApproximatelyEquals(rvmSnout.RadiusBottom);
            if (isCylinderShaped)
            {
                return CylinderWithShear(rvmSnout, rotation, centerA, centerB, normal, radiusA, height, treeIndex, color, bbox);
            }

            return ConeWithShear(rotation, centerA, centerB, normal, radiusA, radiusB, treeIndex, color, bbox);
        }

        if (IsEccentric(rvmSnout))
        {
            return EccentricCone(rvmSnout, scale, rotation, position, normal, radiusA, radiusB, height, treeIndex, color, bbox);
        }

        return Cone(rotation, centerA, centerB, normal, radiusA, radiusB, treeIndex, color, bbox);
    }

    private static IEnumerable<APrimitive> Cone(
        Quaternion rotation,
        Vector3 centerA,
        Vector3 centerB,
        Vector3 normal,
        float radiusA,
        float radiusB,
        ulong treeIndex,
        Color color,
        BoundingBox bbox)
    {
        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;
        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var matrixCapA =
            Matrix4x4.CreateScale(diameterA)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerA);

        var matrixCapB =
            Matrix4x4.CreateScale(diameterB)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerB);

        yield return new Cone(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            centerA,
            centerB,
            localToWorldXAxis,
            radiusA,
            radiusB,
            treeIndex,
            color,
            bbox
        );

        yield return new Circle(
            matrixCapA,
            normal,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );

        yield return new Circle(
            matrixCapB,
            -normal,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );
    }

    private static IEnumerable<APrimitive> EccentricCone(
        RvmSnout rvmSnout,
        Vector3 scale,
        Quaternion rotation,
        Vector3 position,
        Vector3 normal,
        float radiusA,
        float radiusB,
        float height,
        ulong treeIndex,
        Color color,
        BoundingBox bbox)
    {
        var halfHeight = height / 2f;
        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;

        var eccentricNormal = Vector3.Transform(
            Vector3.Normalize(new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height) * scale.X),
            rotation);

        var eccentricCenterA = position + eccentricNormal * halfHeight;
        var eccentricCenterB = position - eccentricNormal * halfHeight;

        var matrixEccentricCapA =
            Matrix4x4.CreateScale(diameterA)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(eccentricCenterA);

        var matrixEccentricCapB =
            Matrix4x4.CreateScale(diameterB)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(eccentricCenterB);

        yield return new EccentricCone(
            eccentricCenterA,
            eccentricCenterB,
            normal,
            radiusA,
            radiusB,
            treeIndex,
            color,
            bbox
        );

        yield return new Circle(
            matrixEccentricCapA,
            normal,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );

        yield return new Circle(
            matrixEccentricCapB,
            -normal,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );
    }

    private static IEnumerable<APrimitive> CylinderWithShear(
        RvmSnout rvmSnout,
        Quaternion rotation,
        Vector3 centerA,
        Vector3 centerB,
        Vector3 normal,
        float radius,
        float height,
        ulong treeIndex,
        Color color,
        BoundingBox bbox)
    {
        var diameter = 2f * radius;
        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var (planeRotationA, planeNormalA, planeSlopeA) = TranslateShearToSlope(rvmSnout.TopShearX, rvmSnout.TopShearY);
        var (planeRotationB, planeNormalB, planeSlopeB) = TranslateShearToSlope(rvmSnout.BottomShearX, rvmSnout.BottomShearY);

        // the slopes will extend the height of the cylinder with radius * tan(slope) (at top and bottom)
        var extendedHeightA = MathF.Tan(planeSlopeA) * radius;
        var extendedHeightB = MathF.Tan(planeSlopeB) * radius;

        var extendedCenterA = centerA + normal * extendedHeightA;
        var extendedCenterB = centerB - normal * extendedHeightB;

        var planeA = new Vector4(planeNormalA, 1 + extendedHeightB + height);
        var planeB = new Vector4(-planeNormalB, 1 + extendedHeightB);

        var matrixCapA =
            Matrix4x4.CreateScale(diameter)
            * Matrix4x4.CreateFromQuaternion(rotation * planeRotationA)
            * Matrix4x4.CreateTranslation(centerA);

        var matrixCapB =
            Matrix4x4.CreateScale(diameter)
            * Matrix4x4.CreateFromQuaternion(rotation * planeRotationB)
            * Matrix4x4.CreateTranslation(centerB);

        yield return new GeneralCylinder(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            extendedCenterA,
            extendedCenterB,
            localToWorldXAxis,
            planeA,
            planeB,
            radius,
            treeIndex,
            color,
            bbox
        );

        yield return new GeneralRing(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            matrixCapA,
            normal,
            Thickness: 1f,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );

        yield return new GeneralRing(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            matrixCapB,
            -normal,
            Thickness: 1f,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );
    }

    private static IEnumerable<APrimitive> ConeWithShear(
        Quaternion rotation,
        Vector3 centerA,
        Vector3 centerB,
        Vector3 normal,
        float radiusA,
        float radiusB,
        ulong treeIndex,
        Color color,
        BoundingBox bbox)
    {
        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;
        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var matrixCapA =
            Matrix4x4.CreateScale(diameterA)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerA);

        var matrixCapB =
            Matrix4x4.CreateScale(diameterB)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerB);

        yield return new Cone(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            centerA,
            centerB,
            localToWorldXAxis,
            radiusA,
            radiusB,
            treeIndex,
            color,
            bbox
        );

        yield return new GeneralRing(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            matrixCapA,
            normal,
            Thickness: 1f,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );

        yield return new GeneralRing(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            matrixCapB,
            -normal,
            Thickness: 1f,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );
    }

    private static bool IsEccentric(RvmSnout rvmSnout)
    {
        return rvmSnout.OffsetX is > 0f or < 0f ||
               rvmSnout.OffsetY is > 0f or < 0f;
    }

    private static bool HasShear(RvmSnout rvmSnout)
    {
        return rvmSnout.BottomShearX is > 0f or < 0f ||
               rvmSnout.BottomShearY is > 0f or < 0f ||
               rvmSnout.TopShearX is > 0f or < 0f ||
               rvmSnout.TopShearY is > 0f or < 0f;
    }

    private static (Quaternion rotation, Vector3 normal, float slope) TranslateShearToSlope(float shearX, float shearY)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, shearY);
        var rotation = rotationAroundX * rotationAroundY;
        var normal = Vector3.Transform(Vector3.UnitZ, rotation);
        var slope = MathF.PI / 2f - MathF.Atan2(normal.Z, MathF.Sqrt(normal.X * normal.X + normal.Y * normal.Y));

        return (rotation, normal, slope);
    }
}