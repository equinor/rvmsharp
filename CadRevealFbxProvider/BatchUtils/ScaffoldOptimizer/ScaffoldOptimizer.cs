namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using System.Drawing;
using System.Linq;
using System.Numerics;
using CadRevealComposer;
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

                var replacementBeam = new ReplacementLedgerBeam(ledgerBeamMesh);
                List<Mesh> partsOfLedgerBeam = replacementBeam.MakeReplacement();
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
}
