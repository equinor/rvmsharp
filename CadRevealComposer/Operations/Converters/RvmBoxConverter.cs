namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Numerics;
using Utils;

public static class RvmBoxExtensions
{
    public static Box ConvertToRevealPrimitive(this RvmBox rvmBox, CadRevealNode revealNode, RvmNode container)
    {
        if (!rvmBox.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmBox.Matrix);
        }
        var unitBoxScale = Vector3.Multiply(
            scale,
            new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

        var matrix =
            Matrix4x4.CreateScale(unitBoxScale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        var color = RvmPrimitiveExtensions.GetColor(container);

        return new Box(matrix, color, revealNode.TreeIndex, rvmBox.BoundingBoxLocal);
    }
}