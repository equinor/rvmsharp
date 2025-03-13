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
    }

    private enum EdgeLocation
    {
        Left = 0,
        Right = 1
    }

    public List<Mesh?> MakeReplacement()
    {
        // :TODO:: In the below procedure we are assuming a ledger beam mesh that is axis aligned.
        // Hence, at the moment we do not support non-axis aligned ledger beams as input.
        // We will attempt to return an empty list if a non-axis aligned mesh is given as input,
        // resulting in an unnaturally tall ledger beam.

        // Assign parameters that can be used to adjust the details of the ledger beam
        const float tubeRadiusFactor = 1.5f; // Make the tube slightly thicker by dividing by 1.5 instead of 2.0
        const float heightTubeSupport = 0.05f; // [m]
        const float tubeSupportThickness = 0.005f; // [m]
        const float couplerWidth = 0.20f; // 20 cm
        const float couplerSpacingWidth = 0.48f; // 48 cm

        // Calculate essential parameters used to correctly place each part of the ledger beam
        BoundingBox ledgerBoundingBox = ObtainBoundingBoxOfLargestDisconnectedMeshPiece(ledgerBeam);
        var ledgerExtent = new SortedBoundingBoxExtent(ledgerBoundingBox);
        if (ledgerExtent.ValueOfMiddle > 0.7f)
            return []; // If the height of the beam exceeds 70 cm, it is probably not correct
        float tubeRadius = ledgerExtent.ValueOfSmallest / tubeRadiusFactor;
        (Vector3 centerLedgerLeft, Vector3 centerLedgerRight) = ledgerExtent.CalcPointsAtEndOfABeamShapedBox(
            SortedBoundingBoxExtent.DisplacementOrigin.BeamTop,
            ledgerExtent.ValueOfMiddle / 2.0f
        );
        float couplerHeight = ledgerExtent.ValueOfMiddle - 4.0f * tubeRadius;

        // Tessellate the ledger beam tubes
        var tubeUpper = TessellateLegerBeamTube(ledgerExtent, Placement.Top, tubeRadius);
        var tubeLower = TessellateLegerBeamTube(ledgerExtent, Placement.Bottom, tubeRadius);

        // Tessellate the supports for each of the
        var tubeSupportUpper = TessellateLedgerBeamTubeSupport(
            ledgerExtent,
            tubeRadius,
            Placement.Top,
            heightTubeSupport,
            tubeSupportThickness
        )?.Mesh;
        var tubeSupportLower = TessellateLedgerBeamTubeSupport(
            ledgerExtent,
            tubeRadius,
            Placement.Bottom,
            heightTubeSupport,
            tubeSupportThickness
        )?.Mesh;

        // Tessellate the couplers at each end of the ledger beam
        var couplers = new List<Mesh?>();
        couplers.Add(
            TessellateEdgeCoupler(
                ledgerExtent,
                EdgeLocation.Left,
                centerLedgerLeft,
                couplerWidth,
                couplerHeight,
                tubeSupportThickness,
                couplerSpacingWidth
            )
        );
        couplers.Add(
            TessellateEdgeCoupler(
                ledgerExtent,
                EdgeLocation.Right,
                centerLedgerRight,
                couplerWidth,
                couplerHeight,
                tubeSupportThickness,
                couplerSpacingWidth
            )
        );

        // Tessellate the remaining couplers of the ledger beam
        couplers.AddRange(
            TessellateCouplers(
                ledgerExtent,
                centerLedgerLeft,
                couplerWidth,
                couplerHeight,
                tubeSupportThickness,
                couplerSpacingWidth
            )
        );

        // Combine meshed parts
        List<Mesh?> combinedMeshes = [tubeUpper?.Mesh, tubeLower?.Mesh, tubeSupportUpper, tubeSupportLower];
        combinedMeshes.AddRange(couplers);

        return combinedMeshes;
    }

    static float CalculateEndCouplerWidth(
        SortedBoundingBoxExtent ledgerExtent,
        float mainCouplerWidth,
        float couplerSpacingWidth
    )
    {
        // Coupler plus spacing width can be thought of as spacing with half a coupling on each side
        float couplerPlusSpacingWidth = mainCouplerWidth + couplerSpacingWidth;

        // Calculate the number of "holes" in the ledger beam, in-between the couplers
        int numCouplerSpaces = (int)(ledgerExtent.ValueOfLargest / couplerPlusSpacingWidth);

        // End supports are calculated to account for ledger beams not being an exact integer multiple of supportPlusOpeningWidth
        return (ledgerExtent.ValueOfLargest - (float)numCouplerSpaces * couplerPlusSpacingWidth) / 2.0f;
    }

    static List<Mesh?> TessellateCouplers(
        SortedBoundingBoxExtent ledgerExtent,
        Vector3 ledgerEdgeCenterLeft,
        float width,
        float height,
        float thickness,
        float spacingWidth
    )
    {
        float couplerPlusSpacingWidth = width + spacingWidth;
        int numSpaces = (int)(ledgerExtent.ValueOfLargest / couplerPlusSpacingWidth);
        int numNonEndCouplers = numSpaces - 1;
        float endCouplerWidth = CalculateEndCouplerWidth(ledgerExtent, width, spacingWidth);
        Vector3 startPosCenterFirstNonEndCouplerVec =
            ledgerEdgeCenterLeft + new Vector3 { [ledgerExtent.AxisIndexOfLargest] = endCouplerWidth };

        var couplers = new List<Mesh?>();
        for (int i = 0; i < numNonEndCouplers; i++)
        {
            var centerVec =
                startPosCenterFirstNonEndCouplerVec
                + new Vector3 { [ledgerExtent.AxisIndexOfLargest] = (float)(i + 1) * couplerPlusSpacingWidth };
            var startVec = centerVec - new Vector3 { [ledgerExtent.AxisIndexOfLargest] = 0.5f * width };
            var endVec = centerVec + new Vector3 { [ledgerExtent.AxisIndexOfLargest] = 0.5f * width };
            couplers.Add(
                PartReplacementUtils
                    .TessellateBoxPart(
                        startVec,
                        endVec,
                        new Vector3 { [ledgerExtent.AxisIndexOfSmallest] = 1.0f },
                        thickness,
                        height
                    )
                    ?.Mesh
            );
        }

        return couplers;
    }

    static Mesh? TessellateEdgeCoupler(
        SortedBoundingBoxExtent ledgerExtent,
        EdgeLocation edgeLocation,
        Vector3 ledgerEdgeCenter,
        float mainCouplerWidth,
        float height,
        float thickness,
        float spacingWidth
    )
    {
        float width = CalculateEndCouplerWidth(ledgerExtent, mainCouplerWidth, spacingWidth);

        var endDisplacement = new Vector3
        {
            [ledgerExtent.AxisIndexOfLargest] = (edgeLocation == EdgeLocation.Left) ? width : -width
        };

        return PartReplacementUtils
            .TessellateBoxPart(
                ledgerEdgeCenter,
                ledgerEdgeCenter + endDisplacement,
                new Vector3 { [ledgerExtent.AxisIndexOfSmallest] = 1.0f },
                thickness,
                height
            )
            ?.Mesh;
    }

    static BoundingBox ObtainBoundingBoxOfLargestDisconnectedMeshPiece(Mesh mesh)
    {
        Mesh maxMesh = LoosePiecesMeshTools
            .SplitMeshByLoosePieces(mesh)
            .MaxBy(x => x.CalculateAxisAlignedBoundingBox().Diagonal)!;
        return maxMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
    }

    static TriangleMesh? TessellateLegerBeamTube(
        SortedBoundingBoxExtent ledgerBeamExtents,
        Placement placement,
        float radius
    )
    {
        (Vector3 centerMin, Vector3 centerMax) = ledgerBeamExtents.CalcPointsAtEndOfABeamShapedBox(
            (placement == Placement.Top)
                ? SortedBoundingBoxExtent.DisplacementOrigin.BeamTop
                : SortedBoundingBoxExtent.DisplacementOrigin.BeamBottom,
            radius
        );

        TriangleMesh? mesh = PartReplacementUtils.TessellateCylinderPart(centerMin, centerMax, radius);

        return mesh;
    }

    static TriangleMesh? TessellateLedgerBeamTubeSupport(
        SortedBoundingBoxExtent ledgerBeamExtents,
        float ledgerBeamTubeRadius,
        Placement placement,
        float height,
        float thickness
    )
    {
        Vector3 displacement = new Vector3
        {
            [ledgerBeamExtents.AxisIndexOfMiddle] = ledgerBeamTubeRadius + height / 2.0f - 0.01f // Subtract 0.01 m to ensure overlap
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
