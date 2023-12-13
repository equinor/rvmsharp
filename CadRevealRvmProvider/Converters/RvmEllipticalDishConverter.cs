namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using CapVisibilityHelpers;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

public static class RvmEllipticalDishConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(this RvmEllipticalDish rvmEllipticalDish,
        ulong treeIndex,
        Color color, Dictionary<Type, Dictionary<RvmPrimitiveToAPrimitive.FailReason, uint>> failedPrimitives)
    {
        if (!rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmEllipticalDish.Matrix);
        }

        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

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
