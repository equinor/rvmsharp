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
using ReplacementScaffoldParts;

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

                var boxPlank = ToBoxPrimitive(nodeGeometries[i], maxMesh, 1.0f);
                if (boxPlank != null)
                {
                    results.Add(new ScaffoldOptimizerResult(boxPlank));
                }
                else
                {
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
        }
        else if (nodeName.ContainsAny(["Kick Board"]))
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
                var indexLargestBoundingBoxVol = PartReplacementUtils.FindIndexWithLargestBoundingBoxVolume(
                    splitMesh.ToList()!
                );

                for (int j = 0; j < splitMesh.Length; j++)
                {
                    var disjointMesh = splitMesh[j];
                    if (j == indexLargestBoundingBoxVol)
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
                        var boxStep = ToBoxPrimitive(nodeGeometries[i], disjointMesh, 1000.0f);
                        if (boxStep != null)
                            results.Add(new ScaffoldOptimizerResult(boxStep));
                    }
                }
            }
        }
        else if (nodeName.ContainsAny(["Base Element"]))
        {
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? mesh = meshes[i];
                if (mesh == null)
                    continue;

                Mesh[] splitMesh = LoosePiecesMeshTools.SplitMeshByLoosePieces(mesh);

                results.AddRange(
                    splitMesh.Select(
                        (disjointMesh, j) =>
                            new ScaffoldOptimizerResult(
                                nodeGeometries[i],
                                Simplify.ConvertToConvexHull(disjointMesh, 0.01f),
                                j,
                                requestChildMeshInstanceId
                            )
                    )
                );
            }
        }
        else if (nodeName.ContainsAny(["FS"]))
        {
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? mesh = meshes[i];
                if (mesh == null)
                    continue;

                Mesh[] splitMesh = LoosePiecesMeshTools.SplitMeshByLoosePieces(mesh);
                var indexLargestBoundingBoxVol = PartReplacementUtils.FindIndexWithLargestBoundingBoxVolume(
                    splitMesh.ToList()!
                );

                int childMeshIndex = 0;
                for (int j = 0; j < splitMesh.Length; j++)
                {
                    var disjointMesh = splitMesh[j];
                    if (j == indexLargestBoundingBoxVol)
                    {
                        // The largest piece of a spear is a cylinder
                        var cylinderWithCaps = PartReplacementUtils.TessellateCylinderPart(
                            disjointMesh,
                            nodeGeometries[i].TreeIndex,
                            true
                        ); // :TODO: At some point, do not tessellate here, but rather return an EccentricCone directly. This does not yet work.
                        if (cylinderWithCaps.cylinder != null)
                        {
                            results.Add(
                                new ScaffoldOptimizerResult(
                                    nodeGeometries[i],
                                    cylinderWithCaps.cylinder.Mesh,
                                    childMeshIndex++,
                                    requestChildMeshInstanceId
                                )
                            );
                        }
                        if (cylinderWithCaps.startCap != null)
                        {
                            results.Add(
                                new ScaffoldOptimizerResult(
                                    nodeGeometries[i],
                                    cylinderWithCaps.startCap.Mesh,
                                    childMeshIndex++,
                                    requestChildMeshInstanceId
                                )
                            );
                        }
                        if (cylinderWithCaps.endCap != null)
                        {
                            results.Add(
                                new ScaffoldOptimizerResult(
                                    nodeGeometries[i],
                                    cylinderWithCaps.endCap.Mesh,
                                    childMeshIndex++,
                                    requestChildMeshInstanceId
                                )
                            );
                        }
                    }
                    else
                    {
                        // The rest of the spear should be optimized using convex hull
                        results.Add(
                            new ScaffoldOptimizerResult(
                                nodeGeometries[i],
                                Simplify.ConvertToConvexHull(disjointMesh, 0.01f),
                                childMeshIndex++,
                                requestChildMeshInstanceId
                            )
                        );
                    }
                }
            }
        }
        else if (nodeName.ContainsAny(["Ledger Beam"]))
        {
            bool failed = false;
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                Mesh? ledgerBeamMesh = meshes[i];
                if (ledgerBeamMesh == null)
                    continue;

                var replacementBeam = new ReplacementLedgerBeam(ledgerBeamMesh);
                List<Mesh?> partsOfLedgerBeam = replacementBeam.MakeReplacement(nodeGeometries[i].TreeIndex);
                if (partsOfLedgerBeam.Count == 0)
                {
                    failed = true;
                    break;
                }

                for (int j = 0; j < partsOfLedgerBeam.Count; j++)
                {
                    Mesh? mesh = partsOfLedgerBeam[j];
                    if (mesh != null)
                    {
                        results.Add(
                            new ScaffoldOptimizerResult(nodeGeometries[i], mesh, j, requestChildMeshInstanceId)
                        );
                    }
                }
            }

            if (failed)
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

    private static Box? ToBoxPrimitive(APrimitive geometryWithTransform, Mesh meshToBound, float heightThreshold)
    {
        var matrix = (geometryWithTransform as InstancedMesh)?.InstanceMatrix ?? Matrix4x4.Identity;
        Box box = BoundingBox.ToBoxPrimitive(
            meshToBound,
            matrix,
            geometryWithTransform.TreeIndex,
            geometryWithTransform.Color
        );

        // :TODO: In the above procedure we are assuming a meshToBound that is axis aligned.
        // Hence, at the moment we do not support non-axis aligned cylinders as input. To detect
        // if the mesh may have been non-axis aligned we will set a threshold on the mesh
        // height to see if it is unnaturally tall. If so, we return null as failure. Remove
        // this test once we support non-axis aligned input.
        var sortedBoundingBoxExtent = new SortedBoundingBoxExtent(meshToBound.CalculateAxisAlignedBoundingBox());
        return sortedBoundingBoxExtent.ValueOfMiddle > heightThreshold ? null : box;
    }
}
