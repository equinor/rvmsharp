namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Utils;

public static class RvmRectangularTorusConverter
{
    public static int count = 0;

    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmRectangularTorus rvmRectangularTorus,
        ulong treeIndex,
        Color color)
    {
        if (!rvmRectangularTorus.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmRectangularTorus.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        var radiusInner = rvmRectangularTorus.RadiusInner * scale.X;
        var radiusOuter = rvmRectangularTorus.RadiusOuter * scale.X;
        var thickness = (radiusOuter - radiusInner) / radiusOuter;
        var outerDiameter = radiusOuter * 2;

        (Vector3 normal, float rotationAngle) = rotation.DecomposeQuaternion();

        var halfHeight = rvmRectangularTorus.Height / 2.0f * scale.Y;
        var centerA = position - normal * halfHeight;
        var centerB = position + normal * halfHeight;

        var localXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        color = Color.Red;

        var arcAngle = rvmRectangularTorus.Angle;
        var transformedRotationAngle = rotationAngle - (1 + rotationAngle / arcAngle) * arcAngle;
        var normalizedRotationAngle = AlgebraUtils.NormalizeRadians(transformedRotationAngle);

        yield return new Cone(
            normalizedRotationAngle,
            rvmRectangularTorus.Angle,
            centerA,
            centerB,
            localXAxis,
            radiusOuter,
            radiusOuter,
            treeIndex,
            color,
            rvmRectangularTorus.CalculateAxisAlignedBoundingBox()
        );

        if (radiusInner > 0)
        {
            yield return new Cone(
                normalizedRotationAngle,
                rvmRectangularTorus.Angle,
                centerA,
                centerB,
                localXAxis,
                radiusInner,
                radiusInner,
                treeIndex,
                color,
                rvmRectangularTorus.CalculateAxisAlignedBoundingBox()
            );
        }

        var matrixRingA =
            Matrix4x4.CreateScale(outerDiameter)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerA);

        var matrixRingB =
            Matrix4x4.CreateScale(outerDiameter)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerB);


        yield return new GeneralRing(
            0f,
            rvmRectangularTorus.Angle,
            matrixRingA,
            -normal,
            thickness,
            treeIndex,
            color,
            rvmRectangularTorus.CalculateAxisAlignedBoundingBox()
        );

        yield return new GeneralRing(
            0f,
            rvmRectangularTorus.Angle,
            matrixRingB,
            normal,
            thickness,
            treeIndex,
            color,
            rvmRectangularTorus.CalculateAxisAlignedBoundingBox()
        );
    }
}