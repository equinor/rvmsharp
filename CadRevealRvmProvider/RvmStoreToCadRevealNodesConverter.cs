namespace CadRevealRvmProvider.Converters;

using System.Diagnostics;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Utils;
using RvmSharp.Containers;
using RvmSharp.Primitives;

internal static class RvmStoreToCadRevealNodesConverter
{
    public static CadRevealNode[] RvmStoreToCadRevealNodes(
        RvmStore rvmStore,
        TreeIndexGenerator treeIndexGenerator,
        NodeNameFiltering nodeNameFiltering,
        PriorityMapping priorityMapping
    )
    {
        var failedPrimitiveConversionsLogObject = new FailedPrimitivesLogObject();
        var cadRevealRootNodes = rvmStore
            .RvmFiles.SelectMany(f => f.Model.Children)
            .Select(root =>
                CollectGeometryNodesRecursive(
                    root,
                    parent: null,
                    treeIndexGenerator,
                    nodeNameFiltering,
                    priorityMapping,
                    failedPrimitiveConversionsLogObject
                )
            )
            .WhereNotNull()
            .ToArray();
        failedPrimitiveConversionsLogObject.LogFailedPrimitives();

        var subBoundingBox = cadRevealRootNodes
            .Select(x => x.BoundingBoxAxisAligned)
            .WhereNotNull()
            .ToArray()
            .Aggregate((a, b) => a.Encapsulate(b));

        Trace.Assert(subBoundingBox != null, "Root node has no bounding box. Are there any meshes in the input?");

        foreach (CadRevealNode cadRevealRootNode in cadRevealRootNodes)
        {
            var allChildren = CadRevealNode.GetAllNodesFlat(cadRevealRootNode);
            var pri = priorityMapping.GetPriority(cadRevealRootNode.Attributes.GetValueOrNull(("Discipline")) ?? "lol"); // TODO
            foreach (CadRevealNode node in allChildren)
            {
                node.Geometries = node.Geometries.Select(g => g with { NodePriority = pri }).ToArray();
            }
        }

        var allNodes = cadRevealRootNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();

        allNodes = allNodes
            .Select(node =>
            {
                var type = node.Attributes.GetValueOrNull("Type") ?? "lol";
                if (type.Equals("VALV"))
                {
                    var geometries = node.Geometries;
                    var sortedGeometries = geometries
                        .OrderByDescending(x => x.AxisAlignedBoundingBox.Diagonal)
                        .ToArray();

                    int numberOfGeometriesToPrioritize = 5; // Arbitrary number
                    if (sortedGeometries.Length < numberOfGeometriesToPrioritize)
                        numberOfGeometriesToPrioritize = sortedGeometries.Length;

                    for (int i = 0; i < numberOfGeometriesToPrioritize; i++)
                    {
                        sortedGeometries[i] = sortedGeometries[i] with { NodePriority = NodePriority.Medium };
                    }
                    node.Geometries = sortedGeometries;
                }

                return node;
            })
            .ToArray();

        return allNodes;
    }

    private static CadRevealNode? CollectGeometryNodesRecursive(
        RvmNode root,
        CadRevealNode? parent,
        TreeIndexGenerator treeIndexGenerator,
        NodeNameFiltering nodeNameFiltering,
        PriorityMapping priorityMapping,
        FailedPrimitivesLogObject failedPrimitivesConversionLogObject
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
            childrenCadNodes = root
                .Children.Select(child =>
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
                                nodeNameFiltering,
                                priorityMapping,
                                failedPrimitivesConversionLogObject
                            );
                        case RvmNode rvmNode:
                            return CollectGeometryNodesRecursive(
                                rvmNode,
                                newNode,
                                treeIndexGenerator,
                                nodeNameFiltering,
                                priorityMapping,
                                failedPrimitivesConversionLogObject
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
            childrenCadNodes = root
                .Children.OfType<RvmNode>()
                .Select(n =>
                    CollectGeometryNodesRecursive(
                        n,
                        newNode,
                        treeIndexGenerator,
                        nodeNameFiltering,
                        priorityMapping,
                        failedPrimitivesConversionLogObject
                    )
                )
                .WhereNotNull()
                .ToArray();
            rvmGeometries = root.Children.OfType<RvmPrimitive>().ToArray();
        }

        newNode.Geometries = rvmGeometries
            .SelectMany(primitive =>
                RvmPrimitiveToAPrimitive.FromRvmPrimitive(
                    newNode.TreeIndex,
                    primitive,
                    root,
                    failedPrimitivesConversionLogObject
                )
            )
            .ToArray();

        newNode.Children = childrenCadNodes;

        var primitiveBoundingBoxes = root
            .Children.OfType<RvmPrimitive>()
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
