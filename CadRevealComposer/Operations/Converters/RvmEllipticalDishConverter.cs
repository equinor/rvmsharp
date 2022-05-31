﻿namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
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

        var bbBox = rvmEllipticalDish.CalculateAxisAlignedBoundingBox();

        var matrixCap =
            Matrix4x4.CreateScale(horizontalRadius * 2, horizontalRadius * 2, 1f)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        yield return new EllipsoidSegment(
            horizontalRadius,
            verticalRadius,
            verticalRadius,
            position,
            normal,
            treeIndex,
            color,
            bbBox
        );

        yield return new Circle(
            matrixCap,
            -normal,
            treeIndex,
            color,
            bbBox
        );
    }
}