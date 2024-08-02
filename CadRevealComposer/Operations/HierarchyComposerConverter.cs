namespace CadRevealComposer.Operations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HierarchyComposer.Model;
using Utils;

public static class HierarchyComposerConverter
{
    private static CadRevealNode FindRootNode(CadRevealNode revealNode)
    {
        var root = revealNode;
        while (root.Parent != null)
        {
            root = root.Parent;
        }

        return root;
    }

    public static IReadOnlyList<HierarchyNode> ConvertToHierarchyNodes(IReadOnlyList<CadRevealNode> nodes)
    {
        return nodes.Select(ConvertRevealNodeToHierarchyNode).WhereNotNull().ToImmutableList();
    }

    /// <summary>
    /// Convert a CadRevealNode to a HierarchyNode
    /// If the RevealNode does not have a RvmNode, it will not be converted.
    /// </summary>
    /// <param name="revealNode"></param>
    /// <returns></returns>
    private static HierarchyNode? ConvertRevealNodeToHierarchyNode(CadRevealNode revealNode)
    {
        var maybeRefNoString = revealNode.Attributes.GetValueOrNull("RefNo");

        RefNo? maybeRefNo = null;
        if (!string.IsNullOrWhiteSpace(maybeRefNoString))
        {
            maybeRefNo = RefNo.Parse(maybeRefNoString);
        }
        var boundingBox = revealNode.BoundingBoxAxisAligned;
        bool hasMesh = revealNode.Geometries.Any();
        AABB? aabb = null;
        if (boundingBox != null)
        {
            aabb = new AABB
            {
                min = new Vector3EfSerializable(boundingBox.Min),
                max = new Vector3EfSerializable(boundingBox.Max)
            };
        }

        // ReSharper disable once MergeIntoPattern
        var maybeParent = revealNode.Parent;

        // FindRootNode could be slow. Easy to improve if profiling identifies as a problem.
        CadRevealNode rootNode = FindRootNode(revealNode);

        return new HierarchyNode
        {
            NodeId = ConvertUlongToUintOrThrowIfTooLarge(revealNode.TreeIndex),
            EndId = ConvertUlongToUintOrThrowIfTooLarge(GetLastIndexInChildrenIncludingSelf(revealNode)),
            RefNoPrefix = maybeRefNo?.Prefix,
            RefNoDb = maybeRefNo?.DbNo,
            RefNoSequence = maybeRefNo?.SequenceNo,
            Name = revealNode.Name,
            TopNodeId = ConvertUlongToUintOrThrowIfTooLarge(rootNode.TreeIndex),
            ParentId = maybeParent != null ? ConvertUlongToUintOrThrowIfTooLarge(maybeParent.TreeIndex) : null,
            PDMSData = FilterRedundantAttributes(revealNode.Attributes),
            HasMesh = hasMesh,
            AABB = aabb,
            OptionalDiagnosticInfo = revealNode.OptionalDiagnosticInfo
        };
    }

    /// <summary>
    /// Finds the last index of this node or its children. Including its own index.
    /// Assumes children are sorted by index
    /// </summary>
    private static ulong GetLastIndexInChildrenIncludingSelf(CadRevealNode node)
    {
        while (true)
        {
            if (node.Children == null || node.Children.Length == 0)
            {
                return node.TreeIndex;
            }

            var lastChild = node.Children[^1];

            node = lastChild;
        }
    }

    /// <summary>
    /// Filter the attributes by excluding some keys that are essentially duplicated.
    /// This saves space in the database.
    /// </summary>
    /// <param name="inputPdmsAttributes">Original Pdms Attributes</param>
    /// <returns>New Dict without the given keys</returns>
    private static Dictionary<string, string> FilterRedundantAttributes(IDictionary<string, string> inputPdmsAttributes)
    {
        return inputPdmsAttributes
            .Where(kvp =>
                !string.Equals("Name", kvp.Key, StringComparison.OrdinalIgnoreCase)
                && !string.Equals("Position", kvp.Key, StringComparison.OrdinalIgnoreCase)
                && !string.Equals("RefNo", kvp.Key, StringComparison.OrdinalIgnoreCase)
            )
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Added this method as a lazy hack:
    /// "TreeIndex is assumed to never exceed ~4 billion nodes,
    /// and we need to adjust the Hierarchy Api if it actually is that large."
    /// </summary>
    /// <param name="input">Input number</param>
    /// <returns>An uint IF its below or equal to <see cref="uint.MaxValue"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">If <see cref="input"/> is above <see cref="uint.MaxValue"/></exception>
    private static uint ConvertUlongToUintOrThrowIfTooLarge(ulong input)
    {
        if (input > uint.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                input,
                $"input was higher than the max uint32 value  {uint.MaxValue}. This is a TODO guard. \n"
                    + "If this becomes a problem we can and should fix the Hierarchy Service to allow for larger IDs."
            );
        }

        return (uint)input;
    }
}
