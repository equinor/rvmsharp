﻿namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

public static class RvmBoxConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmBox rvmBox,
        ulong treeIndex,
        Color color,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (!rvmBox.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmBox.Matrix);
        }

        if (!rvmBox.CanBeConverted(scale, rotation, failedPrimitivesLogObject))
            yield break;

        var unitBoxScale = Vector3.Multiply(scale, new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

        var matrix =
            Matrix4x4.CreateScale(unitBoxScale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        yield return new Box(
            matrix,
            treeIndex,
            color,
            rvmBox.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox()
        );
    }
}
