namespace CadRevealRvmProvider.Converters;

using System.Drawing;
using CadRevealComposer.Primitives;
using RvmSharp.Primitives;

public static class RvmFacetGroupConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmFacetGroup rvmFacetGroup,
        ulong treeIndex,
        Color color,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        yield return new ProtoMeshFromFacetGroup(
            rvmFacetGroup,
            treeIndex,
            color,
            rvmFacetGroup.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox()
        );
    }
}
