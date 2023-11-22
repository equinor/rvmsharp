namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

public static class CircleConverterHelper
{
    /// <summary>
    /// Creates a circle based on a matrix and creates a bounding box
    /// </summary>
    /// <param name="matrix"></param>
    /// <param name="normal"></param>
    /// <param name="treeIndex"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public static Circle ConvertCircle(Matrix4x4 matrix, Vector3 normal, ulong treeIndex, Color color, string area)
    {
        // Circles don't have bounding boxes in RVM, since they are only used as caps
        // This means that we have to calculate a new bounding box for the circle
        // A bounding box is created in the circle's local space and then transformed to world space
        var localBounds = new RvmBoundingBox(new Vector3(-0.5f, -0.5f, -0.001f), new Vector3(0.5f, 0.5f, 0.001f));
        var bb = RvmBoundingBox.CalculateAxisAlignedBoundingBox(localBounds, matrix).ToCadRevealBoundingBox();

        return new Circle(matrix, normal, treeIndex, color, bb, area);
    }
}
