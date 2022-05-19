﻿namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Diagnostics;
using System.Drawing;
using Utils;

public static class RvmEllipticalDishConverter
{
    public static APrimitive ConvertToRevealPrimitive(
        this RvmEllipticalDish rvmEllipticalDish,
        ulong treeIndex,
        Color color)
    {
        if (!rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmEllipticalDish.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");
        if (!rotation.IsIdentity)
        {
            throw new Exception("Cognite Reveal does not support spheres with rotation.");
        }

        var verticalRadius = rvmEllipticalDish.Height * scale.X;
        var horizontalRadius = rvmEllipticalDish.BaseRadius * scale.X;

        return new Ellipsoid(
            horizontalRadius,
            verticalRadius,
            rvmEllipticalDish.Height,
            position,
            treeIndex,
            color,
            rvmEllipticalDish.CalculateAxisAlignedBoundingBox()
        );
    }
}