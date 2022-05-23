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

        var height = scale.Z * MathF.Sqrt(
            rvmSnout.Height * rvmSnout.Height +
            rvmSnout.OffsetX * rvmSnout.OffsetX +
            rvmSnout.OffsetY * rvmSnout.OffsetY);
        var halfHeight = 0.5f * height;

        var radiusA = rvmSnout.RadiusTop * scale.X;
        var radiusB = rvmSnout.RadiusBottom * scale.X;

        var centerA = position + halfHeight * normal;
        var centerB = position - halfHeight * normal;

        var localXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var matrixCapA =
            Matrix4x4.CreateScale(2f * radiusA, 2f * radiusA, 1f)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerA);

        var matrixCapB =
            Matrix4x4.CreateScale(2f * radiusB, 2f * radiusB, 1f)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerB);

        var isConeShaped = !HasShear(rvmSnout);
        if (isConeShaped)
        {
            if (IsEccentric(rvmSnout))
            {
                var capNormal = Vector3.Transform(
                    Vector3.Normalize(new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height) * scale.X),
                    rotation);

                yield return new EccentricCone(
                    centerA,
                    centerB,
                    normal,
                    radiusA,
                    radiusB,
                    treeIndex,
                    color,
                    rvmSnout.CalculateAxisAlignedBoundingBox()
                    );

                yield return new Circle(
                    matrixCapA,
                    capNormal,
                    treeIndex,
                    color,
                    rvmSnout.CalculateAxisAlignedBoundingBox() // use same bbox as RVM source
                );

                yield return new Circle(
                    matrixCapB,
                    -capNormal,
                    treeIndex,
                    color,
                    rvmSnout.CalculateAxisAlignedBoundingBox() // use same bbox as RVM source
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
                rvmSnout.CalculateAxisAlignedBoundingBox()
            );

            yield return new Circle(
                matrixCapA,
                normal,
                treeIndex,
                color,
                rvmSnout.CalculateAxisAlignedBoundingBox() // use same bbox as RVM source
            );

            yield return new Circle(
                matrixCapB,
                -normal,
                treeIndex,
                color,
                rvmSnout.CalculateAxisAlignedBoundingBox() // use same bbox as RVM source
            );

            yield break;
        }

        if (IsEccentric(rvmSnout))
        {
            throw new NotImplementedException(
                "This type of primitive is missing from CadReveal, should convert to mesh?");
        }

        var normalA = ShearToNormal((rvmSnout.TopShearX, rvmSnout.TopShearY));
        var normalB = ShearToNormal((rvmSnout.BottomShearX, rvmSnout.BottomShearY));

        var planeA = new Vector4(normalA, halfHeight);
        var planeB = new Vector4(-normalB, -halfHeight);

        var isCylinderShaped = rvmSnout.RadiusTop.ApproximatelyEquals(rvmSnout.RadiusBottom);
        if (isCylinderShaped)
        {
            // TODO: was ClosedGeneralCylinder which translates to 1x GeneralCylinder, 2x GeneralRing (see cylinder.rs)
            // TODO: was ClosedGeneralCylinder which translates to 1x GeneralCylinder, 2x GeneralRing (see cylinder.rs)
            // TODO: was ClosedGeneralCylinder which translates to 1x GeneralCylinder, 2x GeneralRing (see cylinder.rs)

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
                rvmSnout.CalculateAxisAlignedBoundingBox()
            );

            // TODO: 2x GeneralRing
            // TODO: 2x GeneralRing
            // TODO: 2x GeneralRing

            yield break;
        }

        // TODO: shear
        // TODO: shear
        // TODO: shear

        // TODO: was ClosedGeneralCone which translates to 1x Cone, 2x GeneralRing (see cone.rs)
        // TODO: was ClosedGeneralCone which translates to 1x Cone, 2x GeneralRing (see cone.rs)
        // TODO: was ClosedGeneralCone which translates to 1x Cone, 2x GeneralRing (see cone.rs)

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
            rvmSnout.CalculateAxisAlignedBoundingBox()
        );

        // TODO: 2x GeneralRing
        // TODO: 2x GeneralRing
        // TODO: 2x GeneralRing
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

    private static Vector3 ShearToNormal((float shearX, float shearY) input)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -input.shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, input.shearY);
        var rotationTotal = rotationAroundX * rotationAroundY;
        return Vector3.Transform(Vector3.UnitZ, rotationTotal);
    }
}