namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Diagnostics;
using System.Drawing;
using Utils;

public static class RvmCircularTorusConverter
{
    public static APrimitive ConvertToRevealPrimitive(
        this RvmCircularTorus rvmCircularTorus,
        ulong treeIndex,
        Color color)
    {
        if (!rvmCircularTorus.Matrix.DecomposeAndNormalize(out var scale, out _, out _))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmCircularTorus.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        var tubeRadius = rvmCircularTorus.Radius * scale.X;
        var radius = rvmCircularTorus.Offset * scale.X;
        return new TorusSegment(
            rvmCircularTorus.Angle,
            rvmCircularTorus.Matrix,
            radius,
            tubeRadius,
            treeIndex,
            color,
            rvmCircularTorus.CalculateAxisAlignedBoundingBox()
        );
    }
}