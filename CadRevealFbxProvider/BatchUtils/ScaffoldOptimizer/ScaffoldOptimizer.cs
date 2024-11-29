namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using System.Drawing;
using System.Linq;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using CadRevealComposer.Utils.MeshOptimization;

public class ScaffoldOptimizer : ScaffoldOptimizerBase
{
    protected override List<ScaffoldOptimizerResult>? OptimizeNode(
        string nodeName,
        Mesh?[] meshes,
        APrimitive[] nodeGeometries,
        Func<ulong, int, ulong> requestChildMeshInstanceId
    )
    {
        //
        // The meshes variable contains all meshes from a single node. It has the same length as nodeGeometries and will
        // contain null if the primitive did not contain a mesh. nodeGeometries contains all primitives of the node
        // to be optimized, also the non-mesh ones. To optimize meshes,
        //      1) select an appropriate set of trigger keywords from the scaffold part names using the string.ContainsAny()
        //         extension method,
        //      2) perform the optimization, which includes using meshes and nodeGeometries as basis for creating a
        //         completely new combination of InstancedMesh, TriangleMesh, and available non-mesh primitives. It
        //         is also possible to keep meshes or nodePrimitives the same by passing them on into the new
        //         combination.
        //      3) Add everything in the combination into the results list through ScaffoldOptimizerResult entries,
        //         which can take InstancesMesh, TriangleMesh, or non-mesh primitives as input.
        //
        var results = new List<ScaffoldOptimizerResult>();
        if (nodeGeometries.Length != meshes.Length)
        {
            throw new Exception(
                $"Found {nodeGeometries.Length} node geometries but a mesh array of size {meshes.Length}, but they should be equal!"
            );
        }

        if (nodeName.ContainsAny(["Plank"]) && !nodeName.Contains("0,17"))
        {
            // For each separate mesh found, convert it to a separate box primitive
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                var mesh = meshes[i];
                if (mesh == null)
                    continue;

                Mesh maxMesh = LoosePiecesMeshTools
                    .SplitMeshByLoosePieces(mesh)
                    .MaxBy(x => x.CalculateAxisAlignedBoundingBox().Diagonal)!;

                results.Add(new ScaffoldOptimizerResult(ToBoxPrimitive(nodeGeometries[i], maxMesh)));
            }
        }
        else if (nodeName.ContainsAny(["Kick Board", "BRM"]))
        {
            // For each separate mesh found, convert it to a separate convex hull mesh
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? mesh = meshes[i];
                if (mesh == null)
                    continue;
                results.Add(
                    new ScaffoldOptimizerResult(
                        nodeGeometries[i],
                        Simplify.ConvertToConvexHull(mesh, 0.01f),
                        i,
                        requestChildMeshInstanceId
                    )
                );
            }
        }
        else if (nodeName.ContainsAny(["StairwayGuard"]))
        {
            // For each separate mesh found, convert it to a separate decimated mesh
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? mesh = meshes[i];
                if (mesh == null)
                    continue;
                results.Add(
                    new ScaffoldOptimizerResult(
                        nodeGeometries[i],
                        Simplify.SimplifyMeshLossy(mesh, new SimplificationLogObject()),
                        i,
                        requestChildMeshInstanceId
                    )
                );
            }
        }
        else if (nodeName.ContainsAny(["Stair UTV"]))
        {
            // For each separate mesh found, split its disjoint (non-manifold) parts into separate pieces and optimize separately
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? mesh = meshes[i];
                if (mesh == null)
                    continue;

                Mesh[] splitMesh = LoosePiecesMeshTools.SplitMeshByLoosePieces(mesh);

                var indexMaxVol = splitMesh
                    .Select((m, idx) => new { m, i = idx })
                    .OrderByDescending(v =>
                    {
                        Vector3 ext = v.m.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity).Extents;
                        return ext.X * ext.Y * ext.Z; // Volume
                    })
                    .First()
                    .i;

                for (int j = 0; j < splitMesh.Length; j++)
                {
                    var disjointMesh = splitMesh[j];
                    if (j == indexMaxVol)
                    {
                        // The support for the stairs (assumed largest volume) is only subjected to light geometry optimization
                        results.Add(
                            new ScaffoldOptimizerResult(
                                nodeGeometries[i],
                                Simplify.SimplifyMeshLossy(disjointMesh, new SimplificationLogObject()),
                                j,
                                requestChildMeshInstanceId
                            )
                        );
                    }
                    else
                    {
                        // The smaller parts, assumed to be steps and similar, will be converted to boxes
                        results.Add(new ScaffoldOptimizerResult(ToBoxPrimitive(nodeGeometries[i], disjointMesh)));
                    }
                }
            }
        }
        else if (nodeName.ContainsAny(["Beam"]))
        {
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? ledgerBeamMesh = meshes[i];
                if (ledgerBeamMesh == null)
                    continue;

                List<Mesh> partsOfLedgerBeam = ConstructArtificialLedgerBeam(ledgerBeamMesh);
                for (int j = 0; j < partsOfLedgerBeam.Count; j++)
                {
                    Mesh mesh = partsOfLedgerBeam[j];
                    results.Add(new ScaffoldOptimizerResult(nodeGeometries[i], mesh, j, requestChildMeshInstanceId));
                }
            }
        }
        else
        {
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? mesh = meshes[i];
                if (mesh == null)
                    continue;
                results.Add(
                    new ScaffoldOptimizerResult(
                        nodeGeometries[i],
                        Simplify.SimplifyMeshLossy(mesh, new SimplificationLogObject()),
                        i,
                        requestChildMeshInstanceId
                    )
                );
            }
        }

        return results;
    }

    private static Box ToBoxPrimitive(APrimitive geometry, Mesh mesh)
    {
        // :TODO: Try to move this, or part of this, into BoundingBox::ToBoxPrimitive()
        var matrix = (geometry as InstancedMesh)?.InstanceMatrix ?? Matrix4x4.Identity;

        var scale = new Vector3();
        var rot = new Quaternion();
        var trans = new Vector3();
        matrix.DecomposeAndNormalize(out scale, out rot, out trans);

        BoundingBox boundingBoxTransformed = mesh.CalculateAxisAlignedBoundingBox(matrix);
        BoundingBox boundingBox = mesh.CalculateAxisAlignedBoundingBox(Matrix4x4.CreateScale(scale));

        var matrix2 =
            Matrix4x4.CreateScale(boundingBox.Extents)
            * Matrix4x4.CreateFromQuaternion(rot)
            * Matrix4x4.CreateTranslation(boundingBoxTransformed.Center);
        return new Box(matrix2, geometry.TreeIndex, geometry.Color, boundingBoxTransformed);
    }

    private List<Mesh> ConstructArtificialLedgerBeam(Mesh inputMesh)
    {
        Mesh maxMesh = LoosePiecesMeshTools
            .SplitMeshByLoosePieces(inputMesh)
            .MaxBy(x => x.CalculateAxisAlignedBoundingBox().Diagonal)!;

        BoundingBox bbox = maxMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);

        float lx = bbox.Max.X - bbox.Min.X;
        float ly = bbox.Max.Y - bbox.Min.Y;
        float lz = bbox.Max.Z - bbox.Min.Z;

        // Find largest, smallest, and the middle side lengths
        (float l, int i) maxLen = (lx > ly) ? ((lx > lz) ? (lx, 0) : (lz, 2)) : ((ly > lz) ? (ly, 1) : (lz, 2));
        (float l, int i) minLen = (lx < ly) ? ((lx < lz) ? (lx, 0) : (lz, 2)) : ((ly < lz) ? (ly, 1) : (lz, 2));
        (float l, int i) middleLen = (
            (lx > minLen.l && lx < maxLen.l) ? (lx, 0) : ((ly > minLen.l && ly < maxLen.l) ? (ly, 1) : (lz, 2))
        );

        // Find radius of cylinder
        float cylinderRadius = minLen.l / 1.5f; // Make the cylinder slightly thicker by dividing by 1.5 instead of 2.0

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

        // Find displacement vector needed to displace the top cylinder to the bottom cylinder
        Vector3 displacement = new Vector3(0.0f, 0.0f, 0.0f);
        displacement[middleLen.i] = middleLen.l - 2.0f * cylinderRadius;

        // Find the start and end vector of the lower cylinder
        var centerBottomMin = centerTopMin - displacement;
        var centerBottomMax = centerTopMax - displacement;

        // Tessellate the cylinders
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
        var cylinder2B = EccentricConeTessellator.Tessellate(cone2B);

        // Create a thin box below upper cylinder
        var depthOfBoxVec = new Vector3();
        depthOfBoxVec[middleLen.i] = 0.05f; // [m]
        var thicknessOfBoxVec = new Vector3();
        thicknessOfBoxVec[minLen.i] = 0.005f; // [m]
        displacement[middleLen.i] = cylinderRadius - 0.01f;
        BoundingBox bboxUpper = new BoundingBox(
            centerTopMin - displacement,
            centerTopMax - displacement - depthOfBoxVec + thicknessOfBoxVec
        );
        var boxUpper = CreateBoundingBoxMesh(bboxUpper, inputMesh.Error);

        // Create a thin box above lower cylinder
        BoundingBox bboxLower = new BoundingBox(
            centerBottomMin + displacement,
            centerBottomMax + displacement + depthOfBoxVec + thicknessOfBoxVec
        );
        var boxLower = CreateBoundingBoxMesh(bboxLower, inputMesh.Error);

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
        boxes.Add(CreateBoundingBoxMesh(bboxBox, inputMesh.Error));
        for (int i = 0; i < numNonEndSupports; i++)
        {
            var startVec = startPosCenterFirstNonEndSupportVec + (float)(i + 1) * supportPlusOpeningWidthVec;
            bboxBox = new BoundingBox(
                startVec - supportWidthVec * 0.5f,
                startVec + supportWidthVec * 0.5f + thicknessOfBoxVec - heightOfSupportVec
            );
            boxes.Add(CreateBoundingBoxMesh(bboxBox, inputMesh.Error));
        }
        bboxBox = new BoundingBox(startPosRightEndSupport, endPosRightEndSupport);
        boxes.Add(CreateBoundingBoxMesh(bboxBox, inputMesh.Error));

        // Combine meshed parts
        List<Mesh> combinedMeshes =
            new([cylinder1A.Mesh, cylinder1B.Mesh, cylinder2A.Mesh, cylinder2B.Mesh, boxUpper, boxLower]);
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
}
