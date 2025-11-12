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
        // some variables are not used, but we keep them for future testing
        // code can be cleaned after the export of rvm models is properly fixed
        // for mode info see AB#255079
        var boundingBoxFromVertexCoords = rvmFacetGroup.CalculateBoundingBoxFromVertexPositions();
        var boundingBoxFromRvmFile = rvmFacetGroup.BoundingBoxLocal;

        var bbWorldFromRvmFile = rvmFacetGroup.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

        // When exporting RVM models is fixed, we should compare bbWorldFromRvmFileCancelRotation, bbWorldFromVertexPosition and bbWorldFromRvmFile
        // Right now bbWorldFromVertexPosition ~~ bbWorldFromRvmFileCancelRotation, but it should be
        // bbWorldFromVertexPosition ~~ bbWorldFromRvmFile instead
        // bbWorldFromVertexPosition will then be removed and bbWorldFromRvmFile will be used in stead (for ProtoMeshFromFacetGroup)
        // as bbWorldFromVertexPosition costs to compute and bbWorldFromRvmFile is for "free" (read from RVM file)
        var bbWorldFromRvmFileCancelRotation = RvmBoundingBox
            .CalculateAxisAlignedBoundingBoxCancelRotation(rvmFacetGroup.BoundingBoxLocal, rvmFacetGroup.Matrix)!
            .ToCadRevealBoundingBox();
        var bbWorldFromVertexPosition = RvmBoundingBox
            .CalculateAxisAlignedBoundingBox(boundingBoxFromVertexCoords, rvmFacetGroup.Matrix)
            .ToCadRevealBoundingBox();

        yield return new ProtoMeshFromFacetGroup(rvmFacetGroup, treeIndex, color, bbWorldFromVertexPosition);
    }
}
