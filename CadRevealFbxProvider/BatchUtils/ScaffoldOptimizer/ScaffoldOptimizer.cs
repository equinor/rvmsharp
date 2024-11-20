namespace CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

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
        if (nodeName.ContainsAny(["Plank"]))
        {
            APrimitive? optimizedPrimitive = meshes[0]?.CalculateAxisAlignedBoundingBox()
                .ToBoxPrimitive(nodeGeometries[0].TreeIndex, nodeGeometries[0].Color);
            if(optimizedPrimitive != null) results.Add(new ScaffoldOptimizerResult(optimizedPrimitive));
        }
        else if (nodeName.ContainsAny(["WordC", "WordD"]))
        {
            // :TODO: Example if to be removed after the first optimization has been implemented!
        }
        else
        {
            return null;
        }

        return results;
    }
}
