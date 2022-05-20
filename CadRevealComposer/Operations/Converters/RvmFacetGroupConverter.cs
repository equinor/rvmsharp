namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System.Collections.Generic;
using System.Drawing;

public static class RvmFacetGroupConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmFacetGroup rvmFacetGroup,
        ulong treeIndex,
        Color color)
    {
        yield return new ProtoMeshFromFacetGroup(
            rvmFacetGroup,
            treeIndex,
            color,
            rvmFacetGroup.CalculateAxisAlignedBoundingBox());
    }
}