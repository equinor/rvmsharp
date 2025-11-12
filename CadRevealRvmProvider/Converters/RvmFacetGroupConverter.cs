namespace CadRevealRvmProvider.Converters;

using System.Drawing;
using CadRevealComposer.Primitives;
using RvmSharp.Primitives;

public static class RvmFacetGroupConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmFacetGroup rvmFacetGroup,
        uint treeIndex,
        Color color,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        var boundingBoxFromVertexCoords = rvmFacetGroup.CalculateBoundingBoxFromVertexPositions();
        var boundingBoxFromRvmFile = rvmFacetGroup.BoundingBoxLocal;

        var bbWorldFromRvmFile = rvmFacetGroup.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();
        var bbWorldFromRvmFileCancelRotation = RvmBoundingBox
            .CalculateAxisAlignedBoundingBoxCancelRotation(rvmFacetGroup.BoundingBoxLocal, rvmFacetGroup.Matrix)!
            .ToCadRevealBoundingBox();
        var bbWorldFromVertexPosition = RvmBoundingBox
            .CalculateAxisAlignedBoundingBox(boundingBoxFromVertexCoords, rvmFacetGroup.Matrix)
            .ToCadRevealBoundingBox();

        yield return new ProtoMeshFromFacetGroup(rvmFacetGroup, treeIndex, color, bbWorldFromVertexPosition);
    }
}
