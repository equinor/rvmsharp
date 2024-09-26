namespace CadRevealComposer.Operations.SectorSplitting;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;

public class PrioritySectorSplitter : ISectorSplitter
{
    private const long SectorEstimatedByteSizeBudget = 50_000; // bytes, Arbitrary value

    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries, ulong nextSectorId)
    {
        var sectorIdGenerator = new SequentialIdGenerator(nextSectorId);

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        const string rootPath = "/0";
        yield return SplittingUtils.CreateRootSector(
            rootSectorId,
            rootPath,
            new BoundingBox(Vector3.Zero, Vector3.One)
        );

        var primitivesGroupedByDiscipline = allGeometries.GroupBy(x => x.Discipline);

        var sectors = new List<InternalSector>();
        foreach (var disciplineGroup in primitivesGroupedByDiscipline)
        {
            var geometryGroups = disciplineGroup.GroupBy(x => x.TreeIndex); // Group by treeindex to avoid having one treeindex uneccessary many sectors
            var nodes = PrioritySplittingUtils.ConvertPrimitiveGroupsToNodes(geometryGroups);

            // Ignore outlier nodes
            // TODO: Decide if this is the right thing to do
            (Node[] regularNodes, _) = nodes.SplitNodesIntoRegularAndOutlierNodes();

            sectors.AddRange(SplitIntoTreeIndexSectors(regularNodes, rootPath, rootSectorId, sectorIdGenerator));
        }

        foreach (var sector in sectors)
        {
            yield return sector with
            {
                IsHighlightSector = true
            };
        }
    }

    private IEnumerable<InternalSector> SplitIntoTreeIndexSectors(
        Node[] nodes,
        string rootPath,
        uint rootSectorId,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var nodesUsed = 0;

        while (nodesUsed < nodes.Length)
        {
            var nodesByBudget = GetNodesByBudgetSimple(nodes, nodesUsed).ToArray();
            nodesUsed += nodesByBudget.Length;

            var sectorId = (uint)sectorIdGenerator.GetNextId();
            var subtreeBoundingBox = nodesByBudget.CalculateBoundingBox();

            yield return SplittingUtils.CreateSector(
                nodesByBudget,
                sectorId,
                rootSectorId,
                rootPath,
                1,
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
