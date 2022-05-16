namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Diagnostics;
using System.Drawing;
using Utils;

public static class RvmSphereConverter
{
    public static APrimitive ConvertToRevealPrimitive(
        this RvmSphere rvmSphere,
        ulong treeIndex,
        Color color)
    {
        if (!rvmSphere.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSphere.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");
        if (!rotation.IsIdentity)
        {
            throw new Exception("Cognite Reveal does not support spheres with rotation.");
        }

        var radius = rvmSphere.Radius * scale.X;
        var diameter = radius * 2f;
        return new Ellipsoid(
            radius,
            radius,
            diameter,
            position,
            treeIndex,
            color,
            rvmSphere.CalculateAxisAlignedBoundingBox());
    }
}