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

        (Vector3 normal, float rotationAngle) = rotation.DecomposeQuaternion();

        var radiusInner = rvmRectangularTorus.RadiusInner * scale.X;
        var radiusOuter = rvmRectangularTorus.RadiusOuter * scale.X;

        if (radiusOuter <= 0)
        {
            Console.WriteLine($"Rectangular Torus was removed, because outer radius was zero or less");
            yield break;
        }

        var thickness = (radiusOuter - radiusInner) / radiusOuter;

        var outerDiameter = radiusOuter * 2;
        var halfHeight = rvmRectangularTorus.Height / 2.0f * scale.Y;

        var centerA = position + normal * halfHeight;
        var centerB = position - normal * halfHeight;

        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);
        var arcAngle = rvmRectangularTorus.Angle;

        var bbBox = rvmRectangularTorus.CalculateAxisAlignedBoundingBox();

        yield return new Cone(
            0,
            arcAngle,
            centerA,
            centerB,
            localToWorldXAxis,
            radiusOuter,
            radiusOuter,
            treeIndex,
            color,
            bbBox
        );

        // If inner radius equals 0, then the geometry is basically a cylinder segment, and the inner cone is unnecessary
        if (radiusInner > 0)
        {
            yield return new Cone(
                0,
                arcAngle,
                centerA,
                centerB,
                localToWorldXAxis,
                radiusInner,
                radiusInner,
                treeIndex,
                color,
                bbBox
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
            arcAngle,
            matrixRingA,
            normal,
            thickness,
            treeIndex,
            color,
            bbBox
        );

        yield return new GeneralRing(
            0f,
            arcAngle,
            matrixRingB,
            -normal,
            thickness,
            treeIndex,
            color,
            bbBox
        );

        // Add caps to the two ends of the torus, where the segment is "cut out"
        // This is not needed if the torus goes all the way around
        var isTorusSegment = !arcAngle.ApproximatelyEquals(2 * MathF.PI);
        if (isTorusSegment)
        {
            var v1 = localToWorldXAxis;

            var q2 = Quaternion.CreateFromAxisAngle(normal, arcAngle);
            var v2 = Vector3.Transform(v1, q2);

            var vertex1InnerBottom = centerA + v1 * radiusInner;
            var vertex1InnerTop = centerB + v1 * radiusInner;
            var vertex1OuterBottom = centerA + v1 * radiusOuter;
            var vertex1OuterTop = centerB + v1 * radiusOuter;

            var vertex2InnerBottom = centerA + v2 * radiusInner;
            var vertex2InnerTop = centerB + v2 * radiusInner;
            var vertex2OuterBottom = centerA + v2 * radiusOuter;
            var vertex2OuterTop = centerB + v2 * radiusOuter;

            yield return new Trapezium(
                vertex1InnerTop,
                vertex1OuterTop,
                vertex1InnerBottom,
                vertex1OuterBottom,
                treeIndex,
                color,
                bbBox
            );

            yield return new Trapezium(
                vertex2OuterTop,
                vertex2InnerTop,
                vertex2OuterBottom,
                vertex2InnerBottom,
                treeIndex,
                color,
                bbBox
            );
        }
    }
}