namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

public static class RvmPyramidConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmPyramid rvmPyramid,
        ulong treeIndex,
        Color color)
    {
        if (IsBoxShaped(rvmPyramid))
        {
            if (!rvmPyramid.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
            {
                throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmPyramid.Matrix);
            }

            var unitBoxScale = Vector3.Multiply(
                scale,
                new Vector3(rvmPyramid.BottomX, rvmPyramid.BottomY, rvmPyramid.Height));

            var matrix = Matrix4x4Helpers.CalculateTransformMatrix(position, rotation, unitBoxScale);

            yield return new Box(
                matrix,
                treeIndex,
                color,
                rvmPyramid.CalculateAxisAlignedBoundingBox().ToCadRevealBoundingBox());
        }
        else
        {
            yield return new ProtoMeshFromRvmPyramid(
                rvmPyramid,
                treeIndex,
                color,
                rvmPyramid.CalculateAxisAlignedBoundingBox().ToCadRevealBoundingBox());
        }
    }

    /// <summary>
    /// Q: What is "Pyramid" that has an equal Top plane size to its bottom plane, and has no offset...
    /// A: It is a box.
    /// </summary>
    private static bool IsBoxShaped(RvmPyramid rvmPyramid)
    {
        const double tolerance = 0.001f; // Arbitrary picked value

        // If it has no height, it cannot "Taper", and can be rendered as a box (or Plane, but we do not have a plane primitive).
        // FIXME: GUSH - we must ensure here 0 offset of offset less than half of top/bottom plate as some other weird shapes are possible
        if (rvmPyramid.Height.ApproximatelyEquals(0))
            return false;

        return rvmPyramid.BottomX.ApproximatelyEquals(rvmPyramid.TopX, tolerance)
               && rvmPyramid.TopY.ApproximatelyEquals(rvmPyramid.BottomY, tolerance)
               && rvmPyramid.OffsetX.ApproximatelyEquals(rvmPyramid.OffsetY, tolerance)
               && rvmPyramid.OffsetX.ApproximatelyEquals(0, tolerance);
    }
}