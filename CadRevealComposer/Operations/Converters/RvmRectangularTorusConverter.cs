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
        if (!rvmRectangularTorus.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out _))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmRectangularTorus.Matrix);
        }
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        var radiusInner = rvmRectangularTorus.RadiusInner * scale.X;
        var radiusOuter = rvmRectangularTorus.RadiusOuter * scale.X;
        var thickness = radiusOuter - radiusInner;

        (Vector3 normal, float rotationAngle) = rotation.DecomposeQuaternion();

        yield return new GeneralRing(
            rotationAngle,
            rvmRectangularTorus.Angle,
            rvmRectangularTorus.Matrix,
            normal,
            thickness,
            treeIndex,
            color,
            rvmRectangularTorus.CalculateAxisAlignedBoundingBox()
            );

        // TODO: was ExtrudedRing or ClosedExtrudedRingSegment which translates to 2x GeneralRing, 2x Cone (see ring.rs)
        // TODO: was ExtrudedRing or ClosedExtrudedRingSegment which translates to 2x GeneralRing, 2x Cone (see ring.rs)
        // TODO: was ExtrudedRing or ClosedExtrudedRingSegment which translates to 2x GeneralRing, 2x Cone (see ring.rs)
    }
}