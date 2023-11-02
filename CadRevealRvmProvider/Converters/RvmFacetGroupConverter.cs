namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System.Drawing;

public static class RvmFacetGroupConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmFacetGroup rvmFacetGroup,
        ulong treeIndex,
        Color color,
        RvmNode rvmNode
    )
    {
        var shouldNotSimplify = false;

        if (rvmNode.Attributes.TryGetValue("Discipline", out var val2))
        {
            shouldNotSimplify = val2 == "STRU";
        }

        yield return new ProtoMeshFromFacetGroup(
            rvmFacetGroup,
            treeIndex,
            color,
            rvmFacetGroup.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox(),
            val2 ?? "Unknown"
        );
    }
}
