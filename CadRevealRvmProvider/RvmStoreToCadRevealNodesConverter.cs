namespace CadRevealRvmProvider.Converters;

using System.Diagnostics;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Operations.SectorSplitting;
using CadRevealComposer.Utils;
using RvmSharp.Containers;
using RvmSharp.Primitives;

internal static class RvmStoreToCadRevealNodesConverter
{
    class VariousStatsLogObject
    {
        internal int NumberOfExcludedEmptyNodes = 0;

        public void LogStats()
        {
            Console.WriteLine("Various stats:");
            Console.WriteLine($"\tNumber of squashed empty nodes: {NumberOfExcludedEmptyNodes}");
        }
    }

    public static CadRevealNode[] RvmStoreToCadRevealNodes(
        RvmStore rvmStore,
        TreeIndexGenerator treeIndexGenerator,
        NodeNameFiltering nodeNameFiltering,
        bool truncateNodesWithoutMetadata
    )
    {
        var failedPrimitiveConversionsLogObject = new FailedPrimitivesLogObject();
        var variousStatsLogObject = new VariousStatsLogObject();
        var cadRevealRootNodes = rvmStore
            .RvmFiles.SelectMany(f => f.Model.Children)
            .Select(root =>
                ConvertRvmNodesToCadRevealNodesRecursive(
                    root,
                    parent: null,
                    treeIndexGenerator,
                    truncateNodesWithoutMetadata,
                    nodeNameFiltering,
                    failedPrimitiveConversionsLogObject,
                    variousStatsLogObject
                )
            )
            .WhereNotNull()
            .ToArray();
        failedPrimitiveConversionsLogObject.LogFailedPrimitives();

        variousStatsLogObject.LogStats();

        var subBoundingBox = cadRevealRootNodes
            .Select(x => x.BoundingBoxAxisAligned)
            .WhereNotNull()
            .ToArray()
            .Aggregate((a, b) => a.Encapsulate(b));

        Trace.Assert(subBoundingBox != null, "Root node has no bounding box. Are there any meshes in the input?");

        var allNodes = cadRevealRootNodes.SelectMany(CadRevealNode.GetAllNodesFlat).ToArray();

        return allNodes;
    }

    private static CadRevealNode? ConvertRvmNodesToCadRevealNodesRecursive(
        RvmNode root,
        CadRevealNode? parent,
        TreeIndexGenerator treeIndexGenerator,
        bool truncateNodesWithoutMetadata,
        NodeNameFiltering nodeNameFiltering,
        FailedPrimitivesLogObject failedPrimitivesConversionLogObject,
        VariousStatsLogObject statsLogObject
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

        if (
            truncateNodesWithoutMetadata
            && GetChildRvmNodesRecursive(root).Any()
            && GetChildRvmNodesRecursive(root).All(x => x.Attributes.Count == 0)
        )
        {
            // These nodes have no attributes so we believe they are safe to remove if we need to save TreeIndexes
            // We may need to save TreeIndexes on assets where we have over 2^24 nodes
            statsLogObject.NumberOfExcludedEmptyNodes += GetChildRvmNodesRecursive(root).Count();
            rvmGeometries = GetChildrenPrimitivesRecursive(root).ToArray();
            childrenCadNodes = [];
        }
        else if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
        {
            childrenCadNodes = root
                .Children.Select(child =>
                {
                    switch (child)
                    {
                        case RvmPrimitive rvmPrimitive:
                            return ConvertRvmNodesToCadRevealNodesRecursive(
                                new RvmNode(2, "Implicit geometry", root.Translation, root.MaterialId)
                                {
                                    Children = { rvmPrimitive }
                                },
                                newNode,
                                treeIndexGenerator,
                                truncateNodesWithoutMetadata,
                                nodeNameFiltering,
                                failedPrimitivesConversionLogObject,
                                statsLogObject
                            );
                        case RvmNode rvmNode:
                            return ConvertRvmNodesToCadRevealNodesRecursive(
                                rvmNode,
                                newNode,
                                treeIndexGenerator,
                                truncateNodesWithoutMetadata,
                                nodeNameFiltering,
                                failedPrimitivesConversionLogObject,
                                statsLogObject
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
                    ConvertRvmNodesToCadRevealNodesRecursive(
                        n,
                        newNode,
                        treeIndexGenerator,
                        truncateNodesWithoutMetadata,
                        nodeNameFiltering,
                        failedPrimitivesConversionLogObject,
                        statsLogObject
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

    /// <summary>
    /// Gets the Children of a node, and the children of the children etc, recursively.
    /// Does not include the input node itself.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private static IEnumerable<RvmNode> GetChildRvmNodesRecursive(RvmNode node)
    {
        foreach (var child in node.Children)
        {
            if (child is not RvmNode rvmNode)
                continue;

            foreach (var rvmNode1 in GetRvmNodesRecursive(rvmNode))
                yield return rvmNode1;
        }
    }

    private static IEnumerable<RvmNode> GetRvmNodesRecursive(RvmNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        {
            if (child is not RvmNode rvmNode)
                continue;

            foreach (var rvmNode1 in GetChildRvmNodesRecursive(rvmNode))
                yield return rvmNode1;
        }
    }

    private static IEnumerable<RvmPrimitive> GetChildrenPrimitivesRecursive(RvmNode node)
    {
        foreach (var child in node.Children)
        {
            switch (child)
            {
                case RvmPrimitive rvmPrimitive:
                    yield return rvmPrimitive;
                    break;
                case RvmNode rvmNode:
                    foreach (var rvmPrimitive in GetChildrenPrimitivesRecursive(rvmNode))
                    {
                        yield return rvmPrimitive;
                    }

                    break;
            }
        }
    }
}
