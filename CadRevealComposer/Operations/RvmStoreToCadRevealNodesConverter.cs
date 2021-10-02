namespace CadRevealComposer
{
    using IdProviders;
    using Operations;
    using RvmSharp.Containers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Utils;

    static internal class RvmStoreToCadRevealNodesConverter
    {
        public static CadRevealNode[] RvmStoreToCadRevealNodes(RvmStore rvmStore, NodeIdProvider nodeIdGenerator,
            TreeIndexGenerator treeIndexGenerator)
        {
            var rootNode = new CadRevealNode
            {
                NodeId = nodeIdGenerator.GetNodeId(null),
                TreeIndex = treeIndexGenerator.GetNextId(),
                Parent = null,
                Group = null,
                Children = null
            };

            rootNode.Children = rvmStore.RvmFiles
                .SelectMany(f => f.Model.Children)
                .Select(root =>
                    RvmNodeToCadRevealNodeConverter.CollectGeometryNodesRecursive(root, rootNode, nodeIdGenerator,
                        treeIndexGenerator))
                .ToArray();

            rootNode.BoundingBoxAxisAligned = rootNode.Children
                .Select(x => x.BoundingBoxAxisAligned)
                .WhereNotNull()
                .ToArray().Aggregate((a, b) => a.Encapsulate(b));

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null,
                "Root node has no bounding box. Are there any meshes in the input?");

            var allNodes = GetAllNodesFlat(rootNode).ToArray();
            return allNodes;
        }

        private static IEnumerable<CadRevealNode> GetAllNodesFlat(CadRevealNode root)
        {
            yield return root;

            if (root.Children == null)
            {
                yield break;
            }

            foreach (CadRevealNode cadRevealNode in root.Children)
            {
                foreach (CadRevealNode revealNode in GetAllNodesFlat(cadRevealNode))
                {
                    yield return revealNode;
                }
            }
        }
    }
}