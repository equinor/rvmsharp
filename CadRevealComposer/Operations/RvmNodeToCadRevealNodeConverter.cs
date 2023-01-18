namespace CadRevealComposer.Operations;

using IdProviders;
using RvmSharp.Primitives;
using System;
using System.Linq;
using Utils;

public static class RvmNodeToCadRevealNodeConverter
{
    public static CadRevealNode CollectGeometryNodesRecursive(RvmNode root, CadRevealNode parent, NodeIdProvider nodeIdProvider, TreeIndexGenerator treeIndexGenerator)
    {
        var newNode = new CadRevealNode
        {
            NodeId = nodeIdProvider.GetNodeId(null),
            TreeIndex = treeIndexGenerator.GetNextId(),
            Group = root,
            Parent = parent,
            Children = null
        };

        CadRevealNode[] childrenCadNodes;
        RvmPrimitive[] rvmGeometries = Array.Empty<RvmPrimitive>();


        if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
        {
            childrenCadNodes = root.Children.Select(child =>
            {
                switch (child)
                {
                    case RvmPrimitive rvmPrimitive:
                        return CollectGeometryNodesRecursive(
                            new RvmNode(2, "Implicit geometry", root.Translation, root.MaterialId)
                            {
                                Children = { rvmPrimitive }
                            }, newNode, nodeIdProvider, treeIndexGenerator);
                    case RvmNode rvmNode:
                        return CollectGeometryNodesRecursive(rvmNode, newNode, nodeIdProvider, treeIndexGenerator);
                    default:
                        throw new Exception();
                }
            }).ToArray();
        }
        else
        {
            childrenCadNodes = root.Children.OfType<RvmNode>()
                .Select(n => CollectGeometryNodesRecursive(n, newNode, nodeIdProvider, treeIndexGenerator))
                .ToArray();
            rvmGeometries = root.Children.OfType<RvmPrimitive>().ToArray();
        }

        newNode.RvmGeometries = rvmGeometries;
        newNode.Children = childrenCadNodes;

        var primitiveBoundingBoxes = root.Children.OfType<RvmPrimitive>()
            .Select(x => x.TryCalculateAxisAlignedBoundingBox())
            .WhereNotNull()
            .ToArray();

        var childrenBounds = newNode.Children.Select(x => x.BoundingBoxAxisAligned)
            .WhereNotNull();

        var primitiveAndChildrenBoundingBoxes = primitiveBoundingBoxes.Concat(childrenBounds).ToArray();
        newNode.BoundingBoxAxisAligned = primitiveAndChildrenBoundingBoxes.Any()
            ? primitiveAndChildrenBoundingBoxes.Aggregate((a,b) => a.Encapsulate(b))
            : null;

        return newNode;
    }
}