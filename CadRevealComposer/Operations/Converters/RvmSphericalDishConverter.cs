namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Diagnostics;
using System.Drawing;
using Utils;

public static class RvmSphericalDishConverter
{
    public static APrimitive ConvertToRevealPrimitive(
        this RvmSphericalDish rvmSphericalDish,
        ulong treeIndex,
        Color color)
    {
        if (!rvmSphericalDish.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSphericalDish.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");
        if (!rotation.IsIdentity)
        {
            throw new Exception("Cognite Reveal does not support spheres with rotation.");
        }

        var height = rvmSphericalDish.Height * scale.X;
        var baseRadius = rvmSphericalDish.BaseRadius * scale.X;
        // radius R = h / 2 + c^2 / (8 * h), where c is the cord length or 2 * baseRadius
        var sphereRadius = height / 2 + baseRadius * baseRadius / (2 * height);

        return new Ellipsoid(
            sphereRadius,
            sphereRadius,
            height,
            position,
            treeIndex,
            color,
            rvmSphericalDish.BoundingBoxLocal);
    }
}