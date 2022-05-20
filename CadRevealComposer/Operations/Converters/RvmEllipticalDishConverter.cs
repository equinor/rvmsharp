namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Utils;

public static class RvmEllipticalDishConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmEllipticalDish rvmEllipticalDish,
        ulong treeIndex,
        Color color)
    {
        if (!rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmEllipticalDish.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        var (normal, _) = rotation.DecomposeQuaternion();

        var verticalRadius = rvmEllipticalDish.Height * scale.X;
        var horizontalRadius = rvmEllipticalDish.BaseRadius * scale.X;

        yield return new EllipsoidSegment(
            horizontalRadius,
            verticalRadius,
            verticalRadius,
            position,
            normal,
            treeIndex,
            color,
            rvmEllipticalDish.CalculateAxisAlignedBoundingBox()
        );

        // TODO: add cap
        // TODO: add cap
        // TODO: add cap
    }
}