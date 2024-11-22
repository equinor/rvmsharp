namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;

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

        if (nodeName.ContainsAny(["Plank"]))
        {
            // For each separate mesh found, convert it to a separate box primitive
            for (int i = 0; i < nodeGeometries.Length; i++)
            {
                APrimitive? optimizedPrimitive = meshes[i]
                    ?.CalculateAxisAlignedBoundingBox()
                    .ToBoxPrimitive(nodeGeometries[i].TreeIndex, nodeGeometries[i].Color);
                if (optimizedPrimitive == null)
                    continue;
                results.Add(new ScaffoldOptimizerResult(optimizedPrimitive));
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
                        Simplify.ConvertToConvexHull(mesh),
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
        else
        {
            return null;
        }

        return results;
    }
}
