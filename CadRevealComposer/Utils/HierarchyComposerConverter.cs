namespace CadRevealComposer.Utils
{
    using HierarchyComposer.Model;
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public static class HierarchyComposerConverter
    {
        private static CadRevealNode FindRootNode(CadRevealNode revealNode)
        {
            var root = revealNode;
            while (root.Parent?.Group is RvmNode)
            {
                root = root.Parent;
            }

            return root;
        }

        public static IReadOnlyList<HierarchyNode> ConvertToHierarchyNodes(CadRevealNode[] nodes)
        {
            return nodes
                .Select(ConvertRevealNodeToHierarchyNode)
                .WhereNotNull()
                .ToImmutableList();
        }

        /// <summary>
        /// Convert a CadRevealNode to a HierarchyNode
        /// If the RevealNode does not have a RvmNode, it will not be converted.
        /// </summary>
        /// <param name="revealNode"></param>
        /// <returns></returns>
        private static HierarchyNode? ConvertRevealNodeToHierarchyNode(CadRevealNode revealNode)
        {
            if (revealNode.Group is not RvmNode rvmNode)
                return null;

            var maybeRefNoString = rvmNode.Attributes.GetValueOrNull("RefNo");
            var maybeRefNo = maybeRefNoString != null ? RefNo.Parse(maybeRefNoString) : null;

            var boundingBox = revealNode.BoundingBoxAxisAligned;
            bool hasMesh = revealNode.RvmGeometries.Any();
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
            var maybeParent = revealNode.Parent?.Group is RvmNode ? revealNode.Parent : null;

            // FindRootNode could be slow. Easy to improve if profiling identifies as a problem.
            CadRevealNode cadRevealNode = FindRootNode(revealNode);

            return new HierarchyNode
            {
                NodeId = ConvertUlongToUintOrThrowIfTooLarge(revealNode.TreeIndex),
                RefNoDb = maybeRefNo?.DbNo,
                RefNoSequence = maybeRefNo?.SequenceNo,
                Name = rvmNode.Name,
                TopNodeId = ConvertUlongToUintOrThrowIfTooLarge(cadRevealNode.TreeIndex),
                ParentId = maybeParent != null
                    ? ConvertUlongToUintOrThrowIfTooLarge(maybeParent.TreeIndex)
                    : null,
                PDMSData = FilterRedundantPdmsAttributes(rvmNode.Attributes),
                HasMesh = hasMesh,
                AABB = aabb,
                OptionalDiagnosticInfo = revealNode.OptionalDiagnosticInfo
            };
        }

        /// <summary>
        /// Filter the attributes by excluding some keys that are essentially duplicated.
        /// This saves space in the database.
        /// </summary>
        /// <param name="inputPdmsAttributes">Original Pdms Attributes</param>
        /// <returns>New Dict without the given keys</returns>
        private static Dictionary<string, string> FilterRedundantPdmsAttributes(IDictionary<string, string> inputPdmsAttributes)
        {
            string[] excludedKeysIgnoreCase = new[]
            {
                "Name",
                "RefNo",
                "Position"
            };

            return inputPdmsAttributes
                .Where(kvp => !excludedKeysIgnoreCase.Any(ex => string.Equals(ex, kvp.Key, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Added this method as a lazy hack:
        /// "TreeIndex is assumed to never exceed ~4 billion nodes,
        /// and we need to adjust the Hierarchy Api if it actually is that large."
        /// </summary>
        /// <param name="input">Input number</param>
        /// <returns>An uint IF its below or equal to <see cref="uint.MaxValue"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">If <see cref="input"/> is above <see cref="UInt32.MaxValue"/></exception>
        private static uint ConvertUlongToUintOrThrowIfTooLarge(ulong input)
        {
            if (input > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(input), input,
                    $"input was higher than the max uint32 value  {uint.MaxValue}. This is a TODO guard. \n" +
                    "If this becomes a problem we can and should fix the Hierarchy Service to allow for larger IDs.");
            }

            return (uint)input;
        }
    }
}