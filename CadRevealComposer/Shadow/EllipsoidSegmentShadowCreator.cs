namespace CadRevealComposer.Shadow;

using Primitives;
using System;
using System.Numerics;
using Utils;

public static class EllipsoidSegmentShadowCreator
{
    public static APrimitive CreateShadow(this EllipsoidSegment ellipsoidSegment)
    {
        if (!ellipsoidSegment.InstanceMatrix.DecomposeAndNormalize(out _, out var rotation, out var position))
        {
            throw new Exception(
                "Failed to decompose matrix to transform. Input Matrix: " + ellipsoidSegment.InstanceMatrix
            );
        }

        var scale = new Vector3(
            ellipsoidSegment.HorizontalRadius * 2,
            ellipsoidSegment.HorizontalRadius * 2,
            ellipsoidSegment.Height
        );

        var shadowBoxMatrix =
            Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        return new Box(
            shadowBoxMatrix,
            ellipsoidSegment.TreeIndex,
            ellipsoidSegment.Color,
            ellipsoidSegment.AxisAlignedBoundingBox
        );
    }
}
