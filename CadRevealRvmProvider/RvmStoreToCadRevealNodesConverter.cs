namespace CadRevealRvmProvider.Converters;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Utils;
using RvmSharp.Containers;
using RvmSharp.Primitives;
using SharpGLTF.Schema2;
using System.Diagnostics;

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

        foreach (var node in allNodes)
        {
            var tag = node.Attributes.GetValueOrNull("Tag");

            if (tag != null)
            {
                var allChildren = CadRevealNode.GetAllNodesFlat(node);
                foreach (var child in allChildren)
                {
                    child.Geometries = child.Geometries.Select(g => g with { Priority = 1 }).ToArray();
                }
            }
        }

        //foreach (var cadRevealRootNode in cadRevealRootNodes)
        //{
        //    var children = cadRevealRootNode.Children;

        //    if (children == null)
        //        continue;

        //    foreach (var child in children)
        //    {
        //        var tag = child.Attributes.GetValueOrNull("Tag");

        //        if (tag != null)
        //        {
        //            var allChildNodes = CadRevealNode.GetAllNodesFlat(child);
        //            foreach (var node in allChildNodes)
        //            {
        //                node.Geometries = node.Geometries.Select(g => g with { Priority = 1 }).ToArray();
        //            }
        //        }
        //    }
        //}

        //allNodes = allNodes
        //    .Select(node =>
        //    {
        //        var tag = node.Attributes.GetValueOrNull("Tag");

        //        if (tag != null)
        //            Console.WriteLine("mklfd");

        //        //if (type is "VALV")
        //        //{
        //        //    var geometries = node.Geometries;
        //        //    node.Geometries = geometries.Select(g => g with { Priority = 1 }).ToArray();
        //        //}

        //        return node;
        //    })
        //    .ToArray();

        var allGeometry = allNodes.SelectMany(g => g.Geometries).ToArray();

        int allGeometryCount = allGeometry.Length;
        var prioritizedGeometry = allGeometry.Where(g => g.Priority == 1).Count();

        Console.WriteLine($"PRIORITIZING {prioritizedGeometry} OF {allGeometryCount} GEOMETRIES");

        return allNodes;
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
