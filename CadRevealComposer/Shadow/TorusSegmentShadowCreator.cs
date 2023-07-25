namespace CadRevealComposer.Shadow;

using Primitives;
using System;
using System.Numerics;
using Utils;

public static class TorusSegmentShadowCreator
{
    public static APrimitive CreateShadow(this TorusSegment torusSegment)
    {
        if (!torusSegment.InstanceMatrix.DecomposeAndNormalize(out _, out var rotation, out var position))
        {
            throw new Exception(
                "Failed to decompose matrix to transform. Input Matrix: " + torusSegment.InstanceMatrix
            );
        }

        // TODO Control this
        var height = torusSegment.TubeRadius * 2 * 0.001f;
        var side = torusSegment.Radius * 2 * 0.001f;

        var scale = new Vector3(side, side, height);

        var shadowBoxMatrix =
            Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        return new Box(
            shadowBoxMatrix,
            torusSegment.TreeIndex,
            torusSegment.Color,
            torusSegment.AxisAlignedBoundingBox
        );
    }
}
