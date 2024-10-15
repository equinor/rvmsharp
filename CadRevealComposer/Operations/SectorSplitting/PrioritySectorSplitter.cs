namespace CadRevealComposer.Operations.SectorSplitting;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IdProviders;
using Primitives;

public class PrioritySectorSplitter : ISectorSplitter
{
    private const long SectorEstimatedByteSizeBudget = 50_000; // bytes, Arbitrary value

    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var rootSector = SplittingUtils.CreateRootSector(0, "/0", new BoundingBox(Vector3.Zero, Vector3.One));
        yield return rootSector;
        var primitivesGroupedByDiscipline = allGeometries.GroupBy(x => x.Discipline);

        var sectors = new List<InternalSector>();
        foreach (var disciplineGroup in primitivesGroupedByDiscipline)
        {
            var geometryGroups = disciplineGroup.GroupBy(primitive => primitive.TreeIndex); // Group by treeindex to avoid having one treeindex unnecessary many sectors
            var nodes = PrioritySplittingUtils.ConvertPrimitiveGroupsToNodes(geometryGroups);

            // Ignore outlier nodes
            // TODO: Decide if this is the right thing to do
            (Node[] regularNodes, _) = nodes.SplitNodesIntoRegularAndOutlierNodes();

            sectors.AddRange(SplitIntoTreeIndexSectors(regularNodes, rootSector, sectorIdGenerator));
        }

        foreach (var sector in sectors)
        {
            yield return sector with
            {
                IsHighlightSector = true
            };
        }
    }

    private static IEnumerable<InternalSector> SplitIntoTreeIndexSectors(
        Node[] nodes,
        InternalSector rootSector,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var nodesUsed = 0;

        // Sorting by Id as we believe the TreeIndex to group similar parts in the hierarchy together
        var nodesOrderedByTreeIndex = nodes.OrderBy(x => x.TreeIndex).ToArray();

        while (nodesUsed < nodes.Length)
        {
            // TODO: Should this use spatially aware splitting? Should it place nodes with similar attributes together? Ex: tags?
            var nodesByBudget = GetNodesByBudgetSimple(nodesOrderedByTreeIndex, nodesUsed).ToArray();
            nodesUsed += nodesByBudget.Length;

            var sectorId = sectorIdGenerator.GetNextId();
            var subtreeBoundingBox = nodesByBudget.CalculateBoundingBox();

            yield return SplittingUtils.CreateSector(
                nodesByBudget,
                sectorId,
                rootSector,
                rootSector.Depth + 1,
                subtreeBoundingBox
            );
        }
    }

    private static IEnumerable<Node> GetNodesByBudgetSimple(IReadOnlyList<Node> nodes, int indexToStart)
    {
        var byteSizeBudget = SectorEstimatedByteSizeBudget;

        for (int i = indexToStart; i < nodes.Count; i++)
        {
            if (byteSizeBudget < 0)
            {
                yield break;
            }

            var node = nodes[i];
            byteSizeBudget -= node.EstimatedByteSize;

            yield return node;
        }
    }
}
