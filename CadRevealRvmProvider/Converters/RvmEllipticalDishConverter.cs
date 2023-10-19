namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using CapVisibilityHelpers;
using Commons.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

public static class RvmEllipticalDishConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmEllipticalDish rvmEllipticalDish,
        ulong treeIndex,
        Color color
    )
    {
        if (!rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmEllipticalDish.Matrix);
        }

        if (!IsValid(scale, rotation, rvmEllipticalDish.BaseRadius))
        {
            Console.WriteLine(
                $"Removed EllipticalDish because of invalid data. Scale: {scale.ToString()} Rotation: {rotation.ToString()} Radius: {rvmEllipticalDish.BaseRadius}"
            );
            yield break;
        }

        var (normal, _) = rotation.DecomposeQuaternion();

        var verticalRadius = rvmEllipticalDish.Height * scale.X;
        var horizontalRadius = rvmEllipticalDish.BaseRadius * scale.X;

        var bbBox = rvmEllipticalDish.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

        var matrixCap =
            Matrix4x4.CreateScale(horizontalRadius * 2, horizontalRadius * 2, 1f)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        if (verticalRadius > 0)
        {
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
        }

        var showCap = CapVisibility.IsCapVisible(rvmEllipticalDish, position);

        if (showCap)
        {
            yield return CircleConverterHelper.ConvertCircle(matrixCap, -normal, treeIndex, color);
        }
    }

    private static bool IsValid(Vector3 scale, Quaternion rotation, float radius)
    {
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0)
            return false;

        if (QuaternionHelpers.ContainsInfiniteValue(rotation))
            return false;

        if (radius <= 0)
            return false;

        return true;
    }
}
