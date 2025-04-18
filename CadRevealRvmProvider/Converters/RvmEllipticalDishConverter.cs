﻿namespace CadRevealRvmProvider.Converters;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using CapVisibilityHelpers;
using RvmSharp.Primitives;

public static class RvmEllipticalDishConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmEllipticalDish rvmEllipticalDish,
        uint treeIndex,
        Color color,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (!rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmEllipticalDish.Matrix);
        }

        if (!rvmEllipticalDish.CanBeConverted(scale, rotation, failedPrimitivesLogObject))
            yield break;

        var (normal, _) = rotation.DecomposeQuaternion();

        var verticalRadius = rvmEllipticalDish.Height * scale.X;
        var horizontalRadius = rvmEllipticalDish.BaseRadius * scale.X;

        var bbBox = rvmEllipticalDish.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

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

        var showCap = CapVisibility.IsCapVisible(rvmEllipticalDish, position);

        if (showCap)
        {
            yield return CircleConverterHelper.ConvertCircle(matrixCap, -normal, treeIndex, color);
        }
    }
}
