using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils.MeshOptimization;

namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

public static class ReplacementLedgerBeam
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

    public static List<Mesh?> ToReplacementLedgerBeam(this Mesh ledgerBeam, uint treeIndex)
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
        var tubeUpper = TessellateLegerBeamTube(ledgerExtent, Placement.Top, tubeRadius, treeIndex);
        var tubeLower = TessellateLegerBeamTube(ledgerExtent, Placement.Bottom, tubeRadius, treeIndex);
        var endCapsUpper = TessellateLegerBeamTubeEndCaps(ledgerExtent, Placement.Top, tubeRadius, treeIndex);
        var endCapsLower = TessellateLegerBeamTubeEndCaps(ledgerExtent, Placement.Bottom, tubeRadius, treeIndex);

        // Tessellate the supports for each of the
        var tubeSupportUpper = TessellateLedgerBeamTubeSupport(
            ledgerExtent,
            tubeRadius,
            Placement.Top,
            heightTubeSupport,
            tubeSupportThickness,
            treeIndex
        )?.Mesh;
        var tubeSupportLower = TessellateLedgerBeamTubeSupport(
            ledgerExtent,
            tubeRadius,
            Placement.Bottom,
            heightTubeSupport,
            tubeSupportThickness,
            treeIndex
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
                couplerSpacingWidth,
                treeIndex
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
                couplerSpacingWidth,
                treeIndex
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
                couplerSpacingWidth,
                treeIndex
            )
        );

        // Combine meshed parts
        List<Mesh?> combinedMeshes =
        [
            tubeUpper?.Mesh,
            tubeLower?.Mesh,
            tubeSupportUpper,
            tubeSupportLower,
            endCapsUpper.left?.Mesh,
            endCapsUpper.right?.Mesh,
            endCapsLower.left?.Mesh,
            endCapsLower.right?.Mesh
        ];
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
        float spacingWidth,
        uint treeIndex
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
                    .CreateTessellatedBoxPrimitive(
                        startVec,
                        endVec,
                        new Vector3 { [ledgerExtent.AxisIndexOfSmallest] = 1.0f },
                        thickness,
                        height,
                        treeIndex
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
        float spacingWidth,
        uint treeIndex
    )
    {
        float width = CalculateEndCouplerWidth(ledgerExtent, mainCouplerWidth, spacingWidth);

        var endDisplacement = new Vector3
        {
            [ledgerExtent.AxisIndexOfLargest] = (edgeLocation == EdgeLocation.Left) ? width : -width
        };

        return PartReplacementUtils
            .CreateTessellatedBoxPrimitive(
                ledgerEdgeCenter,
                ledgerEdgeCenter + endDisplacement,
                new Vector3 { [ledgerExtent.AxisIndexOfSmallest] = 1.0f },
                thickness,
                height,
                treeIndex
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
        float radius,
        uint treeIndex
    )
    {
        (Vector3 centerMin, Vector3 centerMax) = ledgerBeamExtents.CalcPointsAtEndOfABeamShapedBox(
            (placement == Placement.Top)
                ? SortedBoundingBoxExtent.DisplacementOrigin.BeamTop
                : SortedBoundingBoxExtent.DisplacementOrigin.BeamBottom,
            radius
        );

        TriangleMesh? mesh = PartReplacementUtils
            .CreateTessellatedCylinderPrimitive(centerMin, centerMax, radius, treeIndex)
            .cylinder;

        return mesh;
    }

    static (TriangleMesh? left, TriangleMesh? right) TessellateLegerBeamTubeEndCaps(
        SortedBoundingBoxExtent ledgerBeamExtents,
        Placement placement,
        float radius,
        uint treeIndex
    )
    {
        (Vector3 centerMin, Vector3 centerMax) = ledgerBeamExtents.CalcPointsAtEndOfABeamShapedBox(
            (placement == Placement.Top)
                ? SortedBoundingBoxExtent.DisplacementOrigin.BeamTop
                : SortedBoundingBoxExtent.DisplacementOrigin.BeamBottom,
            (placement == Placement.Top) ? 0.0f : 2.0f * radius
        );

        var tubeVerticalVec = new Vector3() { [ledgerBeamExtents.AxisIndexOfMiddle] = 1.0f };
        var tubeDirVec = Vector3.Normalize(centerMin - centerMax);
        centerMin += 0.015f * tubeDirVec;
        TriangleMesh? meshL = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            centerMin,
            centerMin - 2.5f * radius * tubeVerticalVec,
            tubeDirVec,
            0.6f * radius,
            2.0f * radius,
            treeIndex
        );
        centerMax -= 0.015f * tubeDirVec;
        TriangleMesh? meshR = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            centerMax,
            centerMax - 2.5f * radius * tubeVerticalVec,
            tubeDirVec,
            0.6f * radius,
            2.0f * radius,
            treeIndex
        );

        return (meshL, meshR);
    }

    static TriangleMesh? TessellateLedgerBeamTubeSupport(
        SortedBoundingBoxExtent ledgerBeamExtents,
        float ledgerBeamTubeRadius,
        Placement placement,
        float height,
        float thickness,
        uint treeIndex
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

        return PartReplacementUtils.CreateTessellatedBoxPrimitive(
            (placement == Placement.Top) ? centerMin - displacement : centerMin + displacement,
            (placement == Placement.Top) ? centerMax - displacement : centerMax + displacement,
            new Vector3 { [ledgerBeamExtents.AxisIndexOfSmallest] = 1.0f },
            thickness,
            height,
            treeIndex
        );
    }
}
