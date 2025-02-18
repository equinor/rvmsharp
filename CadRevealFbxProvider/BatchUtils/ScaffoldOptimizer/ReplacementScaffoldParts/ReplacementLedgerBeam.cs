using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils.MeshOptimization;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

public class ReplacementLedgerBeam(Mesh ledgerBeam)
{
    private enum Placement
    {
        Top = 0,
        Bottom = 1
    };

    public List<Mesh?> MakeReplacement()
    {
        const float tubeRadiusFactor = 1.5f; // Make the tube slightly thicker by dividing by 1.5 instead of 2.0
        const float heightTubeSupport = 0.05f; // [m]
        const float thicknessTubeSupport = 0.005f; // [m]

        BoundingBox bbox = ObtainBoundingBoxOfLargestDisconnectedMeshPiece(ledgerBeam);
        var extents = new SortedBoundingBoxExtent(bbox);
        float tubeRadius = extents.ValueOfSmallest / tubeRadiusFactor;
        var tubeUpper = TessellateLegerBeamTube(tubeRadius, Placement.Top, extents);
        var tubeLower = TessellateLegerBeamTube(tubeRadius, Placement.Bottom, extents);
        var tubeSupportUpper = TessellateLedgerBeamTubeSupport(
            thicknessTubeSupport,
            heightTubeSupport,
            tubeRadius,
            Placement.Top,
            extents
        )?.Mesh;
        var tubeSupportLower = TessellateLedgerBeamTubeSupport(
            thicknessTubeSupport,
            heightTubeSupport,
            tubeRadius,
            Placement.Bottom,
            extents
        )?.Mesh;

        // Create coupler boxes, 20cm wide, 48cm apart
        const float couplerWidth = 0.20f; // [m]?
        const float couplerSpacingWidth = 0.48f; // [m]?
        const float couplerPlusSpacingWidth = couplerWidth + couplerSpacingWidth; // Can be thought of as spacing with half a coupling on each side
        int numSpacingsWith2HalfCouplers = (int)(extents.ValueOfLargest / couplerPlusSpacingWidth);
        float endCouplerWidth =
            (extents.ValueOfLargest - (float)numSpacingsWith2HalfCouplers * couplerPlusSpacingWidth) / 2.0f; // Need to extend end supports to account for beams not being an exact integer multiple of supportPlusOpeningWidth
        int numNonEndCouplers = numSpacingsWith2HalfCouplers - 1;
        float couplerHeight = extents.ValueOfMiddle - 4.0f * tubeRadius;

        Vector3 centerLedgerMin = new Vector3
        {
            [extents.AxisIndexOfLargest] = bbox.Min[extents.AxisIndexOfLargest],
            [extents.AxisIndexOfSmallest] =
                (bbox.Max[extents.AxisIndexOfSmallest] + bbox.Min[extents.AxisIndexOfSmallest]) / 2.0f,
            [extents.AxisIndexOfMiddle] =
                (bbox.Max[extents.AxisIndexOfMiddle] + bbox.Min[extents.AxisIndexOfMiddle]) / 2.0f
        };
        Vector3 centerLedgerMax = new Vector3
        {
            [extents.AxisIndexOfLargest] = bbox.Max[extents.AxisIndexOfLargest],
            [extents.AxisIndexOfSmallest] =
                (bbox.Max[extents.AxisIndexOfSmallest] + bbox.Min[extents.AxisIndexOfSmallest]) / 2.0f,
            [extents.AxisIndexOfMiddle] =
                (bbox.Max[extents.AxisIndexOfMiddle] + bbox.Min[extents.AxisIndexOfMiddle]) / 2.0f
        };
        var boxes = new List<Mesh?>();
        boxes.Add(
            PartReplacementUtils
                .TessellateBoxPart(
                    centerLedgerMin,
                    centerLedgerMin + new Vector3 { [extents.AxisIndexOfLargest] = endCouplerWidth },
                    new Vector3 { [extents.AxisIndexOfSmallest] = 1.0f },
                    thicknessTubeSupport,
                    couplerHeight
                )
                ?.Mesh
        );
        boxes.Add(
            PartReplacementUtils
                .TessellateBoxPart(
                    centerLedgerMax - new Vector3 { [extents.AxisIndexOfLargest] = endCouplerWidth },
                    centerLedgerMax,
                    new Vector3 { [extents.AxisIndexOfSmallest] = 1.0f },
                    thicknessTubeSupport,
                    couplerHeight
                )
                ?.Mesh
        );

        Vector3 startPosCenterFirstNonEndCouplerVec =
            centerLedgerMin + new Vector3 { [extents.AxisIndexOfLargest] = endCouplerWidth };
        for (int i = 0; i < numNonEndCouplers; i++)
        {
            var centerVec =
                startPosCenterFirstNonEndCouplerVec
                + new Vector3 { [extents.AxisIndexOfLargest] = (float)(i + 1) * couplerPlusSpacingWidth };
            var startVec = centerVec - new Vector3 { [extents.AxisIndexOfLargest] = 0.5f * couplerWidth };
            var endVec = centerVec + new Vector3 { [extents.AxisIndexOfLargest] = 0.5f * couplerWidth };
            boxes.Add(
                PartReplacementUtils
                    .TessellateBoxPart(
                        startVec,
                        endVec,
                        new Vector3 { [extents.AxisIndexOfSmallest] = 1.0f },
                        thicknessTubeSupport,
                        couplerHeight
                    )
                    ?.Mesh
            );
        }

        // Combine meshed parts
        List<Mesh?> combinedMeshes =
        [
            tubeUpper.front?.Mesh,
            tubeUpper.back?.Mesh,
            tubeLower.front?.Mesh,
            tubeLower.back?.Mesh,
            tubeSupportUpper,
            tubeSupportLower
        ];
        combinedMeshes.AddRange(boxes);

        return combinedMeshes;
    }

    static BoundingBox ObtainBoundingBoxOfLargestDisconnectedMeshPiece(Mesh mesh)
    {
        Mesh maxMesh = LoosePiecesMeshTools
            .SplitMeshByLoosePieces(mesh)
            .MaxBy(x => x.CalculateAxisAlignedBoundingBox().Diagonal)!;
        return maxMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
    }

    static (TriangleMesh? front, TriangleMesh? back) TessellateLegerBeamTube(
        float pipeRadius,
        Placement placement,
        SortedBoundingBoxExtent ledgerBeamExtents
    )
    {
        (Vector3 centerMin, Vector3 centerMax) = ledgerBeamExtents.CalcPointsAtEndOfABeamShapedBox(
            (placement == Placement.Top)
                ? SortedBoundingBoxExtent.DisplacementOrigin.BeamTop
                : SortedBoundingBoxExtent.DisplacementOrigin.BeamBottom,
            pipeRadius
        );

        (TriangleMesh? front, TriangleMesh? back) = PartReplacementUtils.TessellateCylinderPart(
            centerMin,
            centerMax,
            pipeRadius
        );

        return (front, back);
    }

    static TriangleMesh? TessellateLedgerBeamTubeSupport(
        float thickness,
        float height,
        float ledgerBeamTubeRadius,
        Placement placement,
        SortedBoundingBoxExtent ledgerBeamExtents
    )
    {
        Vector3 displacement = new Vector3
        {
            [ledgerBeamExtents.AxisIndexOfMiddle] = ledgerBeamTubeRadius + height / 2.0f - 0.01f, // Subtract 0.01 m to ensure overlap
            [ledgerBeamExtents.AxisIndexOfSmallest] = thickness / 2.0f
        };

        (Vector3 centerMin, Vector3 centerMax) = ledgerBeamExtents.CalcPointsAtEndOfABeamShapedBox(
            (placement == Placement.Top)
                ? SortedBoundingBoxExtent.DisplacementOrigin.BeamTop
                : SortedBoundingBoxExtent.DisplacementOrigin.BeamBottom,
            ledgerBeamTubeRadius
        );

        return PartReplacementUtils.TessellateBoxPart(
            (placement == Placement.Top) ? centerMin - displacement : centerMin + displacement,
            (placement == Placement.Top) ? centerMax - displacement : centerMax + displacement,
            new Vector3 { [ledgerBeamExtents.AxisIndexOfSmallest] = 1.0f },
            thickness,
            height
        );
    }
}
