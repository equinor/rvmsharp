namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Utils;

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

        var bbox = rvmSnout.CalculateAxisAlignedBoundingBox();

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
                return CylinderWithShear(rvmSnout, rotation, centerA, centerB, normal, radiusA, height, treeIndex,
                    Color.Red, bbox);
            }

            return ConeWithShear(rvmSnout, rotation, centerA, centerB, normal, radiusA, radiusB, treeIndex, Color.Blue,
                bbox);
        }

        if (IsEccentric(rvmSnout))
        {
            return EccentricCone(rvmSnout, scale, rotation, position, normal, radiusA, radiusB, height, treeIndex,
                Color.White, bbox);
        }

        return Cone(rvmSnout, rotation, centerA, centerB, normal, radiusA, radiusB, treeIndex, Color.White, bbox);
    }

    private static IEnumerable<APrimitive> Cone(
        RvmSnout rvmSnout,
        Quaternion rotation,
        Vector3 centerA,
        Vector3 centerB,
        Vector3 normal,
        float radiusA,
        float radiusB,
        ulong treeIndex,
        Color color,
        RvmBoundingBox bbox)
    {
        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;
        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

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

        var (showCapA, showCapB) = PrimitiveCapHelper.CalculateCapVisibility(rvmSnout, centerA, centerB);

        if (showCapA)
        {
            var matrixCapA =
                Matrix4x4.CreateScale(diameterA)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerA);

            yield return new Circle(
                matrixCapA,
                normal,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );
        }

        if (showCapB)
        {
            var matrixCapB =
                Matrix4x4.CreateScale(diameterB)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerB);

            yield return new Circle(
                matrixCapB,
                -normal,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );
        }
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
        RvmBoundingBox bbox)
    {
        var halfHeight = height / 2f;
        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;

        var eccentricNormal = Vector3.Transform(
            Vector3.Normalize(new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height) * scale.X),
            rotation);

        var eccentricCenterA = position + eccentricNormal * halfHeight;
        var eccentricCenterB = position - eccentricNormal * halfHeight;

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

        var (showCapA, showCapB) =
            PrimitiveCapHelper.CalculateCapVisibility(rvmSnout, eccentricCenterA, eccentricCenterB);

        if (showCapA)
        {
            var matrixEccentricCapA =
                Matrix4x4.CreateScale(diameterA)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(eccentricCenterA);

            yield return new Circle(
                matrixEccentricCapA,
                normal,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );
        }

        if (showCapB)
        {
            var matrixEccentricCapB =
                Matrix4x4.CreateScale(diameterB)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(eccentricCenterB);

            yield return new Circle(
                matrixEccentricCapB,
                -normal,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );
        }
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
        RvmBoundingBox bbox)
    {
        var diameter = 2f * radius;
        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var (planeRotationA, planeNormalA, planeSlopeA) = TranslateShearToSlope(rvmSnout.TopShearX, rvmSnout.TopShearY);
        var (planeRotationB, planeNormalB, planeSlopeB) =
            TranslateShearToSlope(rvmSnout.BottomShearX, rvmSnout.BottomShearY);

        var capAShortestSide = diameter;
        var capALongestSide = planeSlopeA != 0 ? diameter / MathF.Cos(planeSlopeA) : diameter;

        var capBShortestSide = diameter;
        var capBLongestSide = planeSlopeB != 0 ? diameter / MathF.Cos(planeSlopeB) : diameter;


        // the slopes will extend the height of the cylinder with radius * tan(slope) (at top and bottom)
        var extendedHeightA = MathF.Tan(planeSlopeA) * radius;
        var extendedHeightB = MathF.Tan(planeSlopeB) * radius;

        var extendedCenterA = centerA + normal * extendedHeightA;
        var extendedCenterB = centerB - normal * extendedHeightB;

        var planeA = new Vector4(planeNormalA, 1 + extendedHeightB + height);
        var planeB = new Vector4(-planeNormalB, 1 + extendedHeightB);

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

        var (showCapA, showCapB) = PrimitiveCapHelper.CalculateCapVisibility(rvmSnout, centerA, centerB);

        if (showCapA)
        {
            var matrixCapA =
                // Matrix4x4.CreateScale(diameter)
                Matrix4x4.CreateScale(new Vector3(capAShortestSide, capALongestSide, 0))
                * Matrix4x4.CreateFromQuaternion(rotation * planeRotationA)
                * Matrix4x4.CreateTranslation(centerA);

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
        }

        if (showCapB)
        {
            var matrixCapB =
                // Matrix4x4.CreateScale(diameter)
                Matrix4x4.CreateScale(new Vector3(capBShortestSide, capBLongestSide, 0))
                * Matrix4x4.CreateFromQuaternion(rotation * planeRotationB)
                * Matrix4x4.CreateTranslation(centerB);

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
    }

    private static IEnumerable<APrimitive> ConeWithShear(
        RvmSnout rvmSnout,
        Quaternion rotation,
        Vector3 centerA,
        Vector3 centerB,
        Vector3 normal,
        float radiusA,
        float radiusB,
        ulong treeIndex,
        Color color,
        RvmBoundingBox bbox)
    {
        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;
        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

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

        var (showCapA, showCapB) = PrimitiveCapHelper.CalculateCapVisibility(rvmSnout, centerA, centerB);

        if (showCapA)
        {
            var matrixCapA =
                Matrix4x4.CreateScale(diameterA)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerA);

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
        }

        if (showCapB)
        {
            var matrixCapB =
                Matrix4x4.CreateScale(diameterB)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerB);

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
    }

    private static bool IsEccentric(RvmSnout rvmSnout)
    {
        return rvmSnout.OffsetX != 0 ||
               rvmSnout.OffsetY != 0;
    }

    private static bool HasShear(RvmSnout rvmSnout)
    {
        return rvmSnout.BottomShearX != 0 ||
               rvmSnout.BottomShearY != 0 ||
               rvmSnout.TopShearX != 0 ||
               rvmSnout.TopShearY != 0;
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