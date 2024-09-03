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
        NodeNameFiltering nodeNameFiltering
    )
    {
        var failedPrimitiveConversionsLogObject = new FailedPrimitivesLogObject();
        var cadRevealRootNodes = rvmStore.RvmFiles
            .SelectMany(f => f.Model.Children)
            .Select(
                root =>
                    CollectGeometryNodesRecursive(
                        root,
                        parent: null,
                        treeIndexGenerator,
                        nodeNameFiltering,
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

        var allNodes = cadRevealRootNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();

        SetPriorityForHighlightSplitting(allNodes);

        return allNodes;
    }

    private static void SetPriorityForHighlightSplitting(CadRevealNode[] nodes)
    {
        var disciplineFilteredNodes = FilterAndSetDiscipline(nodes).ToArray();

        // TODO
        // Are we going to use the custom STID mapper, or should we wait for a more official solution?
        // Notes: Uncommenting code below requires that a relevant file exists on build server
        var tagMappingAndDisciplineFilteredNodes = disciplineFilteredNodes; // StidTagMapper.FilterNodesWithTag(disciplineFilteredNodes);

        var tagAndDisciplineFilteredNodes = FilterByIfTagExists(tagMappingAndDisciplineFilteredNodes).ToArray();
        SetPriortyOnNodesAndChildren(tagAndDisciplineFilteredNodes);
    }

    private static IEnumerable<CadRevealNode> FilterAndSetDiscipline(CadRevealNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var discipline = node.Attributes.GetValueOrNull("Discipline");

            if (discipline != null && discipline != "STRU")
            {
                var children = CadRevealNode.GetAllNodesFlat(node);
                foreach (var child in children)
                {
                    child.Geometries = child.Geometries.Select(g => g with { Discipline = discipline }).ToArray();
                }

                yield return node;
            }
        }
    }

    private static IEnumerable<CadRevealNode> FilterByIfTagExists(CadRevealNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var tag = node.Attributes.GetValueOrNull("Tag");

            if (tag != null)
            {
                yield return node;
            }
        }
    }

    private static void SetPriortyOnNodesAndChildren(CadRevealNode[] nodes)
    {
        foreach (var node in nodes)
        {
            var allChildren = CadRevealNode.GetAllNodesFlat(node);
            foreach (var child in allChildren)
            {
                child.Geometries = child.Geometries.Select(g => g with { Priority = 1 }).ToArray();
            }
        }
    }

    private static CadRevealNode? CollectGeometryNodesRecursive(
        RvmNode root,
        CadRevealNode? parent,
        TreeIndexGenerator treeIndexGenerator,
        NodeNameFiltering nodeNameFiltering,
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
                                nodeNameFiltering,
                                failedPrimitivesConversionLogObject
                            );
                        case RvmNode rvmNode:
                            return CollectGeometryNodesRecursive(
                                rvmNode,
                                newNode,
                                treeIndexGenerator,
                                nodeNameFiltering,
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
            childrenCadNodes = root.Children
                .OfType<RvmNode>()
                .Select(
                    n =>
                        CollectGeometryNodesRecursive(
                            n,
                            newNode,
                            treeIndexGenerator,
                            nodeNameFiltering,
                            failedPrimitivesConversionLogObject
                        )
                )
                .WhereNotNull()
                .ToArray();
            rvmGeometries = root.Children.OfType<RvmPrimitive>().ToArray();
        }

        newNode.Geometries = rvmGeometries
            .SelectMany(
                primitive =>
                    RvmPrimitiveToAPrimitive.FromRvmPrimitive(
                        newNode.TreeIndex,
                        primitive,
                        root,
                        failedPrimitivesConversionLogObject
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
