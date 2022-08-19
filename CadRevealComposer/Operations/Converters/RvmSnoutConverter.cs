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

        if (rvmSnout.HasShear())
        {
            if (rvmSnout.IsEccentric())
            {
                throw new NotImplementedException(
                    "Eccentric snout with shear primitive is missing from CadReveal");
            }

            var isCylinderShaped = rvmSnout.RadiusTop.ApproximatelyEquals(rvmSnout.RadiusBottom);
            if (isCylinderShaped)
            {
                return CylinderWithShear(rvmSnout, rotation, centerA, centerB, normal, radiusA, height, treeIndex,
                    color, bbox);
            }

            throw new NotImplementedException(
                "Cone with shear primitive is missing from CadReveal");
        }

        if (rvmSnout.IsEccentric())
        {
            return EccentricCone(rvmSnout, scale, rotation, position, normal, radiusA, radiusB, height, treeIndex,
                color, bbox);
        }

        return Cone(rvmSnout, rotation, centerA, centerB, normal, radiusA, radiusB, treeIndex, color, bbox);
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

        var (planeRotationA, planeNormalA, planeSlopeA) = rvmSnout.GetTopSlope();
        var (planeRotationB, planeNormalB, planeSlopeB) = rvmSnout.GetBottomSlope();

        var (semiMinorAxisA, semiMajorAxisA) = rvmSnout.GetTopRadii();
        var (semiMinorAxisB, semiMajorAxisB) = rvmSnout.GetBottomRadii();

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
                Matrix4x4.CreateScale(new Vector3(semiMinorAxisA, semiMajorAxisA, 0))
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
                Matrix4x4.CreateScale(new Vector3(semiMinorAxisB, semiMajorAxisB, 0))
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
}