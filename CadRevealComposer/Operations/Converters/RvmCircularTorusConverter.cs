namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System.Collections.Generic;
using System.Drawing;

public static class RvmCircularTorusConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmCircularTorus rvmCircularTorus,
        ulong treeIndex,
        Color color)
    {
        yield return new TorusSegment(
            rvmCircularTorus.Angle,
            rvmCircularTorus.Matrix,
            Radius: rvmCircularTorus.Offset,
            TubeRadius: rvmCircularTorus.Radius,
            treeIndex,
            color,
            rvmCircularTorus.CalculateAxisAlignedBoundingBox()
        );
    }
}