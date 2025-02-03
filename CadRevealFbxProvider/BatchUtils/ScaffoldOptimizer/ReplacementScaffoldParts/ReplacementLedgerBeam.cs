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
    public List<Mesh?> MakeReplacement()
    {
        Mesh maxMesh = LoosePiecesMeshTools
            .SplitMeshByLoosePieces(_ledgerBeam)
            .MaxBy(x => x.CalculateAxisAlignedBoundingBox().Diagonal)!;

        BoundingBox bbox = maxMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
        var sortedBoundingBoxExtent = new SortedBoundingBoxExtent(bbox);

        (float l, int i) maxLen = (sortedBoundingBoxExtent.ValueOfLargest, sortedBoundingBoxExtent.AxisIndexOfLargest);
        (float l, int i) middleLen = (sortedBoundingBoxExtent.ValueOfMiddle, sortedBoundingBoxExtent.AxisIndexOfMiddle);
        (float l, int i) minLen = (
            sortedBoundingBoxExtent.ValueOfSmallest,
            sortedBoundingBoxExtent.AxisIndexOfSmallest
        );

        /*
        float lx = bbox.Max.X - bbox.Min.X;
        float ly = bbox.Max.Y - bbox.Min.Y;
        float lz = bbox.Max.Z - bbox.Min.Z;

        // Find largest, smallest, and the middle side lengths
        (float l, int i) maxLen = (lx > ly) ? ((lx > lz) ? (lx, 0) : (lz, 2)) : ((ly > lz) ? (ly, 1) : (lz, 2));
        (float l, int i) minLen = (lx < ly) ? ((lx < lz) ? (lx, 0) : (lz, 2)) : ((ly < lz) ? (ly, 1) : (lz, 2));
        (float l, int i) middleLen = (
            (lx > minLen.l && lx < maxLen.l) ? (lx, 0) : ((ly > minLen.l && ly < maxLen.l) ? (ly, 1) : (lz, 2))
        );*/

        // Find radius of cylinder
        float cylinderRadius = minLen.l / 1.5f; // Make the cylinder slightly thicker by dividing by 1.5 instead of 2.0

        /*
        // Find the start vector of the upper cylinder
        var centerTopMin = new Vector3();
        centerTopMin[minLen.i] = (bbox.Max[minLen.i] + bbox.Min[minLen.i]) / 2.0f;
        centerTopMin[middleLen.i] = bbox.Max[middleLen.i] - 2.0f * cylinderRadius;
        centerTopMin[maxLen.i] = bbox.Min[maxLen.i];

        // Find the end vector of the upper cylinder
        var centerTopMax = new Vector3();
        centerTopMax[minLen.i] = centerTopMin[minLen.i];
        centerTopMax[middleLen.i] = centerTopMin[middleLen.i];
        centerTopMax[maxLen.i] = bbox.Max[maxLen.i];
        */

        (Vector3 centerTopMin, Vector3 centerTopMax) = sortedBoundingBoxExtent.CalcPointsAtEndOfABeamShapedBox(
            SortedBoundingBoxExtent.DisplacementOrigin.BeamTop,
            cylinderRadius
        );

        (Vector3 centerBottomMin, Vector3 centerBottomMax) = sortedBoundingBoxExtent.CalcPointsAtEndOfABeamShapedBox(
            SortedBoundingBoxExtent.DisplacementOrigin.BeamBottom,
            cylinderRadius
        );

        /*
        // Find displacement vector needed to displace the top cylinder to the bottom cylinder
        Vector3 displacement = new Vector3(0.0f, 0.0f, 0.0f);
        displacement[middleLen.i] = middleLen.l - 2.0f * cylinderRadius;

        // Find the start and end vector of the lower cylinder
        var centerBottomMin = centerTopMin - displacement;
        var centerBottomMax = centerTopMax - displacement;
        */

        // Tessellate the cylinders
        /*
        Vector3 lengthVec = centerTopMax - centerTopMin;
        Vector3 unitNormal = lengthVec * (1.0f / lengthVec.Length());
        EccentricCone cone1A = new EccentricCone(
            centerTopMin,
            centerTopMax,
            unitNormal,
            cylinderRadius,
            cylinderRadius,
            0,
            Color.Brown,
            bbox
        );
        EccentricCone cone1B = new EccentricCone(
            centerTopMin,
            centerTopMax,
            -unitNormal,
            cylinderRadius,
            cylinderRadius,
            0,
            Color.Brown,
            bbox
        );
        EccentricCone cone2A = new EccentricCone(
            centerBottomMin,
            centerBottomMax,
            unitNormal,
            cylinderRadius,
            cylinderRadius,
            0,
            Color.Brown,
            bbox
        );
        EccentricCone cone2B = new EccentricCone(
            centerBottomMin,
            centerBottomMax,
            -unitNormal,
            cylinderRadius,
            cylinderRadius,
            0,
            Color.Brown,
            bbox
        );
        var cylinder1A = EccentricConeTessellator.Tessellate(cone1A);
        var cylinder1B = EccentricConeTessellator.Tessellate(cone1B);
        var cylinder2A = EccentricConeTessellator.Tessellate(cone2A);
        var cylinder2B = EccentricConeTessellator.Tessellate(cone2B);*/
        (TriangleMesh? cylinderUpperFront, TriangleMesh? cylinderUpperBack) = PartReplacementUtils.TessellateCylinderPart(centerTopMin, centerTopMax, cylinderRadius);
        (TriangleMesh? cylinderLowerFront, TriangleMesh? cylinderLowerBack) = PartReplacementUtils.TessellateCylinderPart(centerBottomMin, centerBottomMax, cylinderRadius);

        // Create a thin box below upper cylinder
        var depthOfBoxVec = new Vector3();
        depthOfBoxVec[middleLen.i] = 0.05f; // [m]
        var thicknessOfBoxVec = new Vector3();
        thicknessOfBoxVec[minLen.i] = 0.005f; // [m]
        Vector3 displacement = new Vector3(0.0f, 0.0f, 0.0f);
        displacement[middleLen.i] = cylinderRadius - 0.01f;
        BoundingBox bboxUpper = new BoundingBox(
            centerTopMin - displacement,
            centerTopMax - displacement - depthOfBoxVec + thicknessOfBoxVec
        );
        var boxUpper = CreateBoundingBoxMesh(bboxUpper, _ledgerBeam.Error);

        // Create a thin box above lower cylinder
        BoundingBox bboxLower = new BoundingBox(
            centerBottomMin + displacement,
            centerBottomMax + displacement + depthOfBoxVec + thicknessOfBoxVec
        );
        var boxLower = CreateBoundingBoxMesh(bboxLower, _ledgerBeam.Error);

        // Create thin support boxes, 5cm wide, 12cm apart
        float supportWidth = 4.0f * 0.05f; // [m]?
        float openingWidth = 4.0f * 0.12f; // [m]?
        float supportPlusOpeningWidth = supportWidth + openingWidth; // Can be thought of as opening with half a support on each side
        int numOpeningsWith2HalfSupport = (int)(maxLen.l / supportPlusOpeningWidth);
        float missingWidthOfEndSupport =
            (maxLen.l - (float)numOpeningsWith2HalfSupport * supportPlusOpeningWidth) / 2.0f; // Need to extend end supports to account for beams not being an exact integer multiple of supportPlusOpeingWisth
        int numNonEndSupports = numOpeningsWith2HalfSupport - 1;

        var widthOfEndSupportVec = new Vector3(0.0f, 0.0f, 0.0f);
        widthOfEndSupportVec[maxLen.i] = missingWidthOfEndSupport + 0.5f * supportWidth;

        var supportWidthVec = new Vector3(0.0f, 0.0f, 0.0f);
        supportWidthVec[maxLen.i] = supportWidth;

        var heightOfSupportVec = new Vector3(0.0f, 0.0f, 0.0f);
        heightOfSupportVec[middleLen.i] = middleLen.l - 2.0f * cylinderRadius;

        var startPosLeftEndSupport = centerTopMin;
        var endPosLeftEndSupport = centerTopMin + widthOfEndSupportVec + thicknessOfBoxVec - heightOfSupportVec;
        var startPosRightEndSupport = centerTopMax - widthOfEndSupportVec;
        var endPosRightEndSupport = centerTopMax + thicknessOfBoxVec - heightOfSupportVec;

        var startPosCenterFirstNonEndSupportVec = centerTopMin + widthOfEndSupportVec - supportWidthVec * 0.5f;
        var supportPlusOpeningWidthVec = new Vector3(0.0f, 0.0f, 0.0f);
        supportPlusOpeningWidthVec[maxLen.i] = supportPlusOpeningWidth;

        var boxes = new List<Mesh>();
        var bboxBox = new BoundingBox(startPosLeftEndSupport, endPosLeftEndSupport);
        boxes.Add(CreateBoundingBoxMesh(bboxBox, _ledgerBeam.Error));
        for (int i = 0; i < numNonEndSupports; i++)
        {
            var startVec = startPosCenterFirstNonEndSupportVec + (float)(i + 1) * supportPlusOpeningWidthVec;
            bboxBox = new BoundingBox(
                startVec - supportWidthVec * 0.5f,
                startVec + supportWidthVec * 0.5f + thicknessOfBoxVec - heightOfSupportVec
            );
            boxes.Add(CreateBoundingBoxMesh(bboxBox, _ledgerBeam.Error));
        }
        bboxBox = new BoundingBox(startPosRightEndSupport, endPosRightEndSupport);
        boxes.Add(CreateBoundingBoxMesh(bboxBox, _ledgerBeam.Error));

        // Combine meshed parts
        List<Mesh?> combinedMeshes =
            new([cylinderUpperFront?.Mesh, cylinderUpperBack?.Mesh, cylinderLowerFront?.Mesh, cylinderLowerBack?.Mesh, boxUpper, boxLower]);
        foreach (var box in boxes)
        {
            combinedMeshes.Add(box);
        }

        return combinedMeshes;
    }

    private Mesh CreateBoundingBoxMesh(BoundingBox boundingBox, float error)
    {
        Vector3[] boundingBoxVertices = new Vector3[8];

        Vector3 d = boundingBox.Max - boundingBox.Min;
        boundingBoxVertices[0] = boundingBox.Min;
        boundingBoxVertices[1] = boundingBox.Min + new Vector3(d.X, 0.0f, 0.0f);
        boundingBoxVertices[2] = boundingBox.Min + new Vector3(0.0f, d.Y, 0.0f);
        boundingBoxVertices[3] = boundingBox.Min + new Vector3(d.X, d.Y, 0.0f);

        boundingBoxVertices[4] = boundingBox.Min + new Vector3(0.0f, 0.0f, d.Z);
        boundingBoxVertices[5] = boundingBox.Min + new Vector3(d.X, 0.0f, d.Z);
        boundingBoxVertices[6] = boundingBox.Min + new Vector3(0.0f, d.Y, d.Z);
        boundingBoxVertices[7] = boundingBox.Min + new Vector3(d.X, d.Y, d.Z);

        uint[] indices = new uint[36];
        // Surface X-Y
        indices[0] = 0;
        indices[1] = 2;
        indices[2] = 3;

        indices[3] = 0;
        indices[4] = 3;
        indices[5] = 1;

        // Surface X-Z-near min
        indices[6] = 0;
        indices[7] = 1;
        indices[8] = 4;

        indices[9] = 1;
        indices[10] = 5;
        indices[11] = 4;

        // Surface Y-Z-near min
        indices[12] = 0;
        indices[13] = 6;
        indices[14] = 2;

        indices[15] = 0;
        indices[16] = 4;
        indices[17] = 6;

        // Surface X-Z-far min
        indices[18] = 3;
        indices[19] = 2;
        indices[20] = 6;

        indices[21] = 3;
        indices[22] = 6;
        indices[23] = 7;

        // Surface Y-Z-far min
        indices[24] = 3;
        indices[25] = 5;
        indices[26] = 1;

        indices[27] = 3;
        indices[28] = 7;
        indices[29] = 1;

        // Surface X-Y-far min
        indices[30] = 4;
        indices[31] = 5;
        indices[32] = 7;

        indices[33] = 4;
        indices[34] = 7;
        indices[35] = 6;

        Mesh reducedMesh = new Mesh(boundingBoxVertices, indices, error);
        return reducedMesh;
    }

    private Mesh _ledgerBeam = ledgerBeam;
}
