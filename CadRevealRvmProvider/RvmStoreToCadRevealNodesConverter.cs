namespace CadRevealRvmProvider.Converters;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Utils;
using RvmSharp.Containers;
using RvmSharp.Primitives;
using System.Diagnostics;

internal static class RvmStoreToCadRevealNodesConverter
{
    public static CadRevealNode[] RvmStoreToCadRevealNodes(
        RvmStore rvmStore,
        TreeIndexGenerator treeIndexGenerator,
        NodeNameFiltering nodeNameFiltering
    )
    {
        var cadRevealRootNodes = rvmStore.RvmFiles
            .SelectMany(f => f.Model.Children)
            .Select(root => CollectGeometryNodesRecursive(root, parent: null, treeIndexGenerator, nodeNameFiltering))
            .WhereNotNull()
            .ToArray();

        var subBoundingBox = cadRevealRootNodes
            .Select(x => x.BoundingBoxAxisAligned)
            .WhereNotNull()
            .ToArray()
            .Aggregate((a, b) => a.Encapsulate(b));

        Trace.Assert(subBoundingBox != null, "Root node has no bounding box. Are there any meshes in the input?");

        var allNodes = cadRevealRootNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();
        return allNodes;
    }

    private static CadRevealNode? CollectGeometryNodesRecursive(
        RvmNode root,
        CadRevealNode? parent,
        TreeIndexGenerator treeIndexGenerator,
        NodeNameFiltering nodeNameFiltering
    )
    {
        if (nodeNameFiltering.ShouldExcludeNode(root.Name))
            return null;

        var newNode = new CadRevealNode
        {
            TreeIndex = treeIndexGenerator.GetNextId(),
            Parent = parent,
            Children = null,
            Name = root.Name,
            Attributes = root.Attributes
        };

        CadRevealNode[] childrenCadNodes;
        RvmPrimitive[] rvmGeometries = Array.Empty<RvmPrimitive>();

        if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
        {
            childrenCadNodes = root.Children
                .Select(child =>
                {
                    switch (child)
                    {
                        case RvmPrimitive rvmPrimitive:
                            return CollectGeometryNodesRecursive(
                                new RvmNode(2, "Implicit geometry", root.Translation, root.MaterialId)
                                {
                                    Children = { rvmPrimitive }
                                },
                                newNode,
                                treeIndexGenerator,
                                nodeNameFiltering
                            );
                        case RvmNode rvmNode:
                            return CollectGeometryNodesRecursive(
                                rvmNode,
                                newNode,
                                treeIndexGenerator,
                                nodeNameFiltering
                            );
                        default:
                            throw new Exception();
                    }
                })
                .WhereNotNull()
                .ToArray();
        }
        else
        {
            childrenCadNodes = root.Children
                .OfType<RvmNode>()
                .Select(n => CollectGeometryNodesRecursive(n, newNode, treeIndexGenerator, nodeNameFiltering))
                .WhereNotNull()
                .ToArray();
            rvmGeometries = root.Children.OfType<RvmPrimitive>().ToArray();
        }

        // Hacky way to get the area
        var r = newNode;
        while (r.Parent is not null)
        {
            r = r.Parent;
        }

        newNode.Geometries = rvmGeometries
            .SelectMany(
                primitive =>
                    RvmPrimitiveToAPrimitive.FromRvmPrimitive(
                        newNode.TreeIndex,
                        primitive,
                        root,
                        r.Name.Split("-").First()
                    )
            )
            .ToArray();

        newNode.Children = childrenCadNodes;

        var primitiveBoundingBoxes = root.Children
            .OfType<RvmPrimitive>()
            .Select(x => x.CalculateAxisAlignedBoundingBox()?.ToCadRevealBoundingBox())
            .WhereNotNull()
            .ToArray();
        var childrenBounds = newNode.Children.Select(x => x.BoundingBoxAxisAligned).WhereNotNull();

        var primitiveAndChildrenBoundingBoxes = primitiveBoundingBoxes.Concat(childrenBounds).ToArray();
        newNode.BoundingBoxAxisAligned = primitiveAndChildrenBoundingBoxes.Any()
            ? primitiveAndChildrenBoundingBoxes.Aggregate((a, b) => a.Encapsulate(b))
            : null;

        return newNode;
    }
}
