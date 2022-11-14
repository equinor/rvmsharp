namespace CadRevealRvmProvider.Converters;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Utils;
using RvmSharp.Containers;
using System.Diagnostics;

static internal class RvmStoreToCadRevealNodesConverter
{
    public static CadRevealNode[] RvmStoreToCadRevealNodes(RvmStore rvmStore,
        TreeIndexGenerator treeIndexGenerator)
    {
        var cadRevealRootNodes = rvmStore.RvmFiles
            .SelectMany(f => f.Model.Children)
            .Select(root =>
                RvmNodeToCadRevealNodeConverter.CollectGeometryNodesRecursive(root, parent: null,
                    treeIndexGenerator))
            .ToArray();

        var subBoundingBox = cadRevealRootNodes
            .Select(x => x.BoundingBoxAxisAligned)
            .WhereNotNull()
            .ToArray().Aggregate((a, b) => a.Encapsulate(b));

        Debug.Assert(subBoundingBox != null,
            "Root node has no bounding box. Are there any meshes in the input?");

        var allNodes = cadRevealRootNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();
        return allNodes;
    }

   
}