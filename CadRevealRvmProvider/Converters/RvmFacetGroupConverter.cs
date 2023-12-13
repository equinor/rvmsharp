namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System.Drawing;

public static class RvmFacetGroupConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(this RvmFacetGroup rvmFacetGroup,
        ulong treeIndex,
        Color color, Dictionary<Type, Dictionary<RvmPrimitiveToAPrimitive.FailReason, uint>> failedPrimitives)
    {
        yield return new ProtoMeshFromFacetGroup(
            rvmFacetGroup,
            treeIndex,
            color,
            rvmFacetGroup.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox()
        );
    }
}
