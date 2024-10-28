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
        if (sectorIdGenerator.PeekNextId == 0)
            _ = sectorIdGenerator.GetNextId(); // Get and discard id 0, as 0 is hardcoded below
        var dummyRootSector = SplittingUtils.CreateRootSector(0, "/0", new BoundingBox(Vector3.Zero, Vector3.One));
        yield return dummyRootSector; // Using dummy root sector because we need to remap it to the non-priority sector root later on

        var sectors = new List<InternalSector>();
        // We split the geometries into sectors based on the discipline because there are very few highlight cases that are cross-discipline
        // This helps reduce the overhead of a priority sector, since highlighting of multiple tags within the same discipline is more common
        var primitivesGroupedByDiscipline = allGeometries.GroupBy(x => x.Discipline);
        foreach (var disciplineGroup in primitivesGroupedByDiscipline)
        {
            var geometryGroups = disciplineGroup.GroupBy(primitive => primitive.TreeIndex); // Group by treeindex to avoid having one treeindex unnecessary many sectors
            var nodes = PrioritySplittingUtils.ConvertPrimitiveGroupsToNodes(geometryGroups);

            // Ignore outlier nodes
            // TODO: Decide if this is the right thing to do
            (Node[] regularNodes, _) = nodes.SplitNodesIntoRegularAndOutlierNodes();

            sectors.AddRange(SplitIntoTreeIndexSectors(regularNodes, dummyRootSector, sectorIdGenerator));
        }

        foreach (var sector in sectors)
        {
            yield return sector with
            {
                IsPrioritizedSector = true
            };
        }
    }

    private static IEnumerable<InternalSector> SplitIntoTreeIndexSectors(
        Node[] nodes,
        InternalSector rootSector,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var nodesProcessed = 0;

        // Sorting by Id as we believe the TreeIndex to group similar parts in the hierarchy together
        var nodesOrderedByTreeIndex = nodes.OrderBy(x => x.TreeIndex).ToArray();

        while (nodesProcessed < nodes.Length)
        {
            // TODO: Should this use spatially aware splitting? Should it place nodes with similar attributes together? Ex: tags?
            var nodesByBudget = GetNodesByBudgetSimple(nodesOrderedByTreeIndex, nodesProcessed).ToArray();
            nodesProcessed += nodesByBudget.Length;

            var sectorId = sectorIdGenerator.GetNextId();
            var subtreeBoundingBox = nodesByBudget.CalculateBoundingBox();

            yield return SplittingUtils.CreateSector(nodesByBudget, sectorId, rootSector, subtreeBoundingBox);
        }
    }

    private static IEnumerable<Node> GetNodesByBudgetSimple(Node[] nodes, int indexToStart)
    {
        var remainingBudget = SectorEstimatedByteSizeBudget;

        for (int i = indexToStart; i < nodes.Length; i++)
        {
            if (remainingBudget < 0)
            {
                yield break;
            }

            var node = nodes[i];
            remainingBudget -= node.EstimatedByteSize;

            yield return node;
        }
    }
}
