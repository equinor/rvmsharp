namespace CadRevealRvmProvider.Converters;

using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using Commons.Utils;
using MathNet.Numerics.Distributions;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

public static class RvmSnoutConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(this RvmSnout rvmSnout, ulong treeIndex, Color color)
    {
        if (!rvmSnout.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSnout.Matrix);
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        var (normal, _) = rotation.DecomposeQuaternion();

        var bbox = rvmSnout.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

        var length =
            scale.Z
            * MathF.Sqrt(
                rvmSnout.Height * rvmSnout.Height
                    + rvmSnout.OffsetX * rvmSnout.OffsetX
                    + rvmSnout.OffsetY * rvmSnout.OffsetY
            );
        var halfLength = 0.5f * length;

        var radiusA = rvmSnout.RadiusTop * scale.X;
        var radiusB = rvmSnout.RadiusBottom * scale.X;

        if (radiusA <= 0 && radiusB <= 0)
        {
            Console.WriteLine($"Snout was removed, because the radii were: {radiusA} and {radiusB}");
            return Array.Empty<APrimitive>();
        }

        var centerA = position + normal * halfLength;
        var centerB = position - normal * halfLength;

        if (rvmSnout.HasShear())
        {
            if (rvmSnout.IsEccentric())
            {
                throw new NotImplementedException("Eccentric snout with shear primitive is missing from CadReveal");
            }

            var isCylinderShaped = rvmSnout.RadiusTop.ApproximatelyEquals(rvmSnout.RadiusBottom);
            if (isCylinderShaped)
            {
                return CreateCylinderWithShear(
                    rvmSnout,
                    rotation,
                    centerA,
                    centerB,
                    normal,
                    length,
                    scale,
                    treeIndex,
                    color,
                    bbox
                );
            }

            throw new NotImplementedException("Cone with shear primitive is missing from CadReveal");
        }

        if (rvmSnout.IsEccentric())
        {
            return CreateEccentricCone(
                rvmSnout,
                scale,
                rotation,
                position,
                normal,
                radiusA,
                radiusB,
                length,
                treeIndex,
                color,
                bbox
            );
        }

        return CreateCone(rvmSnout, rotation, centerA, centerB, normal, radiusA, radiusB, treeIndex, color, bbox);
    }

    private static IEnumerable<APrimitive> CreateCone(
        RvmSnout rvmSnout,
        Quaternion rotation,
        Vector3 centerA,
        Vector3 centerB,
        Vector3 normal,
        float radiusA,
        float radiusB,
        ulong treeIndex,
        Color color,
        BoundingBox bbox
    )
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

            yield return CircleConverterHelper.ConvertCircle(matrixCapA, normal, treeIndex, color);
        }

        if (showCapB)
        {
            var matrixCapB =
                Matrix4x4.CreateScale(diameterB)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerB);

            yield return CircleConverterHelper.ConvertCircle(matrixCapB, -normal, treeIndex, color);
        }
    }

    private static IEnumerable<APrimitive> CreateEccentricCone(
        RvmSnout rvmSnout,
        Vector3 scale, // TODO: Scale is unused, why?
        Quaternion rotation,
        Vector3 position,
        Vector3 normal,
        float radiusA,
        float radiusB,
        float length,
        ulong treeIndex,
        Color color,
        BoundingBox bbox
    )
    {
        var halfLength = length / 2f;
        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;

        var eccentricNormal = Vector3.Transform(
            Vector3.Normalize(new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height)),
            rotation
        );

        var eccentricCenterA = position + eccentricNormal * halfLength;
        var eccentricCenterB = position - eccentricNormal * halfLength;

        yield return new EccentricCone(
            eccentricCenterA,
            eccentricCenterB,
            normal, // TODO CHECK WHY NOT eccentricNormal
            radiusA,
            radiusB,
            treeIndex,
            color,
            bbox
        );

        var (showCapA, showCapB) = PrimitiveCapHelper.CalculateCapVisibility(
            rvmSnout,
            eccentricCenterA,
            eccentricCenterB
        );

        if (showCapA)
        {
            var matrixEccentricCapA =
                Matrix4x4.CreateScale(diameterA)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(eccentricCenterA);

            yield return CircleConverterHelper.ConvertCircle(matrixEccentricCapA, normal, treeIndex, color);
        }

        if (showCapB)
        {
            var matrixEccentricCapB =
                Matrix4x4.CreateScale(diameterB)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(eccentricCenterB);

            yield return CircleConverterHelper.ConvertCircle(matrixEccentricCapB, -normal, treeIndex, color);
        }
    }

    private static IEnumerable<APrimitive> CreateCylinderWithShear(
        RvmSnout rvmSnout,
        Quaternion rotation,
        Vector3 centerA,
        Vector3 centerB,
        Vector3 normal,
        float height,
        Vector3 scale,
        ulong treeIndex,
        Color color,
        BoundingBox bbox
    )
    {
        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var (planeRotationA, planeNormalA, planeSlopeA) = rvmSnout.GetTopSlope();
        var (planeRotationB, planeNormalB, planeSlopeB) = rvmSnout.GetBottomSlope();

        (var ellipsePolarA, _, _) = rvmSnout.GetTopCapEllipse();
        (var ellipsePolarB, _, _) = rvmSnout.GetBottomCapEllipse();

        var semiMinorAxisA = ellipsePolarA.semiMinorAxis * scale.X;
        var semiMajorAxisA = ellipsePolarA.semiMajorAxis * scale.X;
        var semiMinorAxisB = ellipsePolarB.semiMinorAxis * scale.X;
        var semiMajorAxisB = ellipsePolarB.semiMajorAxis * scale.X;

        // the slopes will extend the height of the cylinder with radius * tan(slope) (at top and bottom)
        var extendedHeightA = MathF.Tan(planeSlopeA) * (float)semiMinorAxisA;
        var extendedHeightB = MathF.Tan(planeSlopeB) * (float)semiMinorAxisB;

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
            (float)semiMinorAxisA,
            treeIndex,
            color,
            bbox
        );

        var (showCapA, showCapB) = PrimitiveCapHelper.CalculateCapVisibility(rvmSnout, centerA, centerB);

        if (showCapA)
        {
            var matrixCapA =
                Matrix4x4.CreateScale(new Vector3((float)semiMinorAxisA, (float)semiMajorAxisA, 0) * 2.0f)
                * Matrix4x4.CreateFromQuaternion(rotation * planeRotationA)
                * Matrix4x4.CreateTranslation(centerA);

            if (matrixCapA.IsDecomposable())
            {
                yield return new GeneralRing(
                    Angle: 0f,
                    ArcAngle: 2f * MathF.PI,
                    matrixCapA,
                    normal,
                    Thickness: 1f,
                    treeIndex,
                    color,
                    bbox // Why we use the same bbox as RVM source
                );
            }
            else
            {
                // This should not happen, but happens in so few models as of now that we think we can ignore it.
                Console.WriteLine(
                    $"Failed to decompose matrix for {nameof(matrixCapA)} of node {treeIndex} geometry: {rvmSnout}"
                );
            }
        }

        if (showCapB)
        {
            var matrixCapB =
                Matrix4x4.CreateScale(new Vector3((float)semiMinorAxisB, (float)semiMajorAxisB, 0) * 2.0f)
                * Matrix4x4.CreateFromQuaternion(rotation * planeRotationB)
                * Matrix4x4.CreateTranslation(centerB);

            if (matrixCapB.IsDecomposable())
            {
                yield return new GeneralRing(
                    Angle: 0f,
                    ArcAngle: 2f * MathF.PI,
                    matrixCapB,
                    -normal,
                    Thickness: 1f,
                    treeIndex,
                    color,
                    bbox // Why we use the same bbox as RVM source
                );
            }
            else
            {
                // This should not happen, but happens in so few models as of now that we think we can ignore it.
                Console.WriteLine(
                    $"Failed to decompose matrix for {nameof(matrixCapB)} of node {treeIndex} geometry: {rvmSnout}"
                );
            }
        }
    }
}
