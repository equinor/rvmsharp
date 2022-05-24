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
        color = Color.White;


        if (!rvmSnout.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSnout.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform scale, was {scale}");

        var (normal, _) = rotation.DecomposeQuaternion();

        var bbox = rvmSnout.CalculateAxisAlignedBoundingBox();

        var height = scale.Z * MathF.Sqrt(
            rvmSnout.Height * rvmSnout.Height +
            rvmSnout.OffsetX * rvmSnout.OffsetX +
            rvmSnout.OffsetY * rvmSnout.OffsetY);
        var halfHeight = 0.5f * height;
        var localXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var radiusA = rvmSnout.RadiusTop * scale.X;
        var radiusB = rvmSnout.RadiusBottom * scale.X;

        var diameterA = 2f * radiusA;
        var diameterB = 2f * radiusB;

        var normalA = normal;
        var normalB = -normal;

        var centerA = position + normalA * halfHeight;
        var centerB = position + normalB * halfHeight;

        var matrixCapA =
            Matrix4x4.CreateScale(diameterA)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerA);

        var matrixCapB =
            Matrix4x4.CreateScale(diameterB)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerB);

        var isConeShaped = !HasShear(rvmSnout);
        var isCylinderShaped = rvmSnout.RadiusTop.ApproximatelyEquals(rvmSnout.RadiusBottom);

        if (isConeShaped)
        {
            if (IsEccentric(rvmSnout))
            {
                var eccentricNormal = Vector3.Transform(
                    Vector3.Normalize(new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height) * scale.X),
                    rotation);

                var matrixEccentricCapA =
                    Matrix4x4.CreateScale(diameterA)
                    * Matrix4x4.CreateFromQuaternion(rotation)
                    * Matrix4x4.CreateTranslation(position + eccentricNormal * halfHeight);

                var matrixEccentricCapB =
                    Matrix4x4.CreateScale(diameterB)
                    * Matrix4x4.CreateFromQuaternion(rotation)
                    * Matrix4x4.CreateTranslation(position - eccentricNormal * halfHeight);

                yield return new EccentricCone(
                    position + eccentricNormal * halfHeight,
                    position - eccentricNormal * halfHeight,
                    normal,
                    radiusA,
                    radiusB,
                    treeIndex,
                    color,
                    bbox
                    );

                yield return new Circle(
                    matrixEccentricCapA,
                    normalA,
                    treeIndex,
                    color,
                    bbox // use same bbox as RVM source
                );

                yield return new Circle(
                    matrixEccentricCapB,
                    normalB,
                    treeIndex,
                    color,
                    bbox // use same bbox as RVM source
                );

                yield break;
            }

            yield return new Cone(
                Angle: 0f,
                ArcAngle: 2f * MathF.PI,
                centerA,
                centerB,
                localXAxis,
                radiusA,
                radiusB,
                treeIndex,
                color,
                bbox
            );

            yield return new Circle(
                matrixCapA,
                normalA,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );

            yield return new Circle(
                matrixCapB,
                normalB,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );

            yield break;
        }

        if (IsEccentric(rvmSnout))
        {
            throw new NotImplementedException(
                "This type of primitive is missing from CadReveal, should convert to mesh?");
        }

        if (isCylinderShaped)
        {
            // TODO: was ClosedGeneralCylinder which translates to 1x GeneralCylinder, 2x GeneralRing (see cylinder.rs)
            // TODO: was ClosedGeneralCylinder which translates to 1x GeneralCylinder, 2x GeneralRing (see cylinder.rs)
            // TODO: was ClosedGeneralCylinder which translates to 1x GeneralCylinder, 2x GeneralRing (see cylinder.rs)

            var planeNormalA = ShearToNormal((rvmSnout.TopShearX, rvmSnout.TopShearY));
            var planeNormalB = ShearToNormal((rvmSnout.BottomShearX, rvmSnout.BottomShearY));

            var planeA = new Vector4(planeNormalA, -Vector3.Dot(planeNormalA, centerA));
            var planeB = new Vector4(planeNormalB, -Vector3.Dot(planeNormalB, centerB));

            yield return new GeneralCylinder(
                Angle: 0f,
                ArcAngle: 2f * MathF.PI,
                centerA,
                centerB,
                localXAxis,
                planeA,
                planeB,
                radiusA,
                treeIndex,
                color,
                bbox
            );

            yield return new GeneralRing(
                Angle: 0f,
                ArcAngle: 2f * MathF.PI,
                InstanceMatrix: matrixCapA,
                Normal: normalA,
                Thickness: 1f,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );

            yield return new GeneralRing(
                Angle: 0f,
                ArcAngle: 2f * MathF.PI,
                InstanceMatrix: matrixCapB,
                Normal: normalB,
                Thickness: 1f,
                treeIndex,
                color,
                bbox // use same bbox as RVM source
            );

            yield break;
        }

        // TODO: was ClosedGeneralCone which translates to 1x Cone, 2x GeneralRing (see cone.rs)
        // TODO: was ClosedGeneralCone which translates to 1x Cone, 2x GeneralRing (see cone.rs)
        // TODO: was ClosedGeneralCone which translates to 1x Cone, 2x GeneralRing (see cone.rs)

        // TODO: shear
        // TODO: shear
        // TODO: shear

        yield return new Cone(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            centerA,
            centerB,
            localXAxis,
            radiusA,
            radiusB,
            treeIndex,
            color,
            bbox
        );

        yield return new GeneralRing(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            InstanceMatrix: matrixCapA,
            Normal: normal,
            Thickness: 1f,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );

        yield return new GeneralRing(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            InstanceMatrix: matrixCapB,
            Normal: normal,
            Thickness: 1f,
            treeIndex,
            color,
            bbox // use same bbox as RVM source
        );
    }

    private static bool IsEccentric(RvmSnout rvmSnout)
    {
        return rvmSnout.OffsetX > 0f ||
               rvmSnout.OffsetY > 0f;
    }

    private static bool HasShear(RvmSnout rvmSnout)
    {
        return rvmSnout.BottomShearX > 0f ||
               rvmSnout.BottomShearY > 0f ||
               rvmSnout.TopShearX > 0f ||
               rvmSnout.TopShearY > 0f;
    }

    private static Vector3 ShearToNormal((float shearX, float shearY) input)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -input.shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, input.shearY);
        var rotationTotal = rotationAroundX * rotationAroundY;
        return Vector3.Transform(Vector3.UnitZ, rotationTotal);
    }
}