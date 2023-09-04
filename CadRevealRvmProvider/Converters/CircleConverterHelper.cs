namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

public static class CircleConverterHelper
{
    public static Circle ConvertCircle(Matrix4x4 matrix, Vector3 normal, ulong treeIndex, Color color)
    {
        var localBounds = new RvmBoundingBox(new Vector3(-0.5f, -0.5f, -0.001f), new Vector3(0.5f, 0.5f, 0.001f));
        var bb = RvmBoundingBox.CalculateAxisAlignedBoundingBox(localBounds, matrix).ToCadRevealBoundingBox();
        return new Circle(matrix, normal, treeIndex, color, bb);
    }
}
