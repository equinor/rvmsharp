namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;

public class HighlightSectorSplitter : ISectorSplitter
{
    private const float MinDiagonalSizeAtDepth_1 = 7; // arbitrary value for min size at depth 1
    private const float MinDiagonalSizeAtDepth_2 = 4; // arbitrary value for min size at depth 2
    private const float MinDiagonalSizeAtDepth_3 = 1.5f; // arbitrary value for min size at depth 3
    private const long SectorEstimatedByteSizeBudget = 100_000; // bytes, Arbitrary value
    private const float DoNotChopSectorsSmallerThanMetersInDiameter = 17.4f; // Arbitrary value

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
            var nodes = HighlightSplittingUtils.ConvertPrimitiveGroupsToNodes(geometryGroups);

            // Ignore outlier nodes
            // TODO: Decide if this is the right thing to do
            (Node[] regularNodes, Node[] outlierNodes) = nodes.SplitNodesIntoRegularAndOutlierNodes();

            sectors.AddRange(SplitIntoTreeIndexSectors(regularNodes, rootPath, rootSectorId, sectorIdGenerator));
        }

        foreach (var sector in sectors)
        {
            // TODO Is there a better way to mark as highlightsector

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

    private IEnumerable<InternalSector> SplitIntoSectorsRecursive(
        Node[] nodes,
        int recursiveDepth,
        string parentPath,
        uint? parentSectorId,
        SequentialIdGenerator sectorIdGenerator,
        int depthToStartSplittingGeometry
    )
    {
        if (nodes.Length == 0)
        {
            yield break;
        }

        var actualDepth = Math.Max(1, recursiveDepth - depthToStartSplittingGeometry + 1);

        var subtreeBoundingBox = nodes.CalculateBoundingBox();

        var mainVoxelNodes = Array.Empty<Node>();
        Node[] subVoxelNodes;

        if (recursiveDepth < depthToStartSplittingGeometry)
        {
            subVoxelNodes = nodes;
        }
        else
        {
            // fill main voxel according to budget
            var additionalMainVoxelNodesByBudget = GetNodesByBudget(
                    nodes.ToArray(),
                    SectorEstimatedByteSizeBudget,
                    actualDepth
                )
                .ToList();
            mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
            subVoxelNodes = nodes.Except(mainVoxelNodes).ToArray();
        }

        if (!subVoxelNodes.Any())
        {
            var sectorId = (uint)sectorIdGenerator.GetNextId();

            yield return SplittingUtils.CreateSector(
                mainVoxelNodes,
                sectorId,
                parentSectorId,
                parentPath,
                actualDepth,
                subtreeBoundingBox
            );
        }
        else
        {
            string parentPathForChildren = parentPath;
            uint? parentSectorIdForChildren = parentSectorId;

            var geometries = mainVoxelNodes.SelectMany(n => n.Geometries).ToArray();

            // Should we keep empty sectors???? yes no?
            if (geometries.Any() || subVoxelNodes.Any())
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var path = $"{parentPath}/{sectorId}";

                yield return SplittingUtils.CreateSector(
                    mainVoxelNodes,
                    sectorId,
                    parentSectorId,
                    parentPath,
                    actualDepth,
                    subtreeBoundingBox
                );

                parentPathForChildren = path;
                parentSectorIdForChildren = sectorId;
            }

            var sizeOfSubVoxelNodes = subVoxelNodes.Sum(x => x.EstimatedByteSize);
            var subVoxelDiagonal = subVoxelNodes.CalculateBoundingBox().Diagonal;

            if (
                subVoxelDiagonal < DoNotChopSectorsSmallerThanMetersInDiameter
                || sizeOfSubVoxelNodes < SectorEstimatedByteSizeBudget
            )
            {
                var sectors = SplitIntoSectorsRecursive(
                    subVoxelNodes,
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry
                );
                foreach (var sector in sectors)
                {
                    yield return sector;
                }

                yield break;
            }

            var voxels = subVoxelNodes
                .GroupBy(node => SplittingUtils.CalculateVoxelKeyForNode(node, subtreeBoundingBox))
                .OrderBy(x => x.Key)
                .ToImmutableList();

            foreach (var voxelGroup in voxels)
            {
                if (voxelGroup.Key == SplittingUtils.MainVoxel)
                {
                    throw new Exception(
                        "Main voxel should not appear here. Main voxel should be processed separately."
                    );
                }

                var sectors = SplitIntoSectorsRecursive(
                    voxelGroup.ToArray(),
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry
                );
                foreach (var sector in sectors)
                {
                    yield return sector;
                }
            }
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

    private static IEnumerable<Node> GetNodesByBudget(IReadOnlyList<Node> nodes, long byteSizeBudget, int actualDepth)
    {
        var selectedNodes = actualDepth switch
        {
            1 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_1).ToArray(),
            2 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_2).ToArray(),
            3 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_3).ToArray(),
            _ => nodes.ToArray(),
        };

        var nodesInPrioritizedOrder = selectedNodes.OrderByDescending(x => x.Diagonal);

        var nodeArray = nodesInPrioritizedOrder.ToArray();
        var byteSizeBudgetLeft = byteSizeBudget;
        for (int i = 0; i < nodeArray.Length; i++)
        {
            if ((byteSizeBudgetLeft < 0) && nodeArray.Length - i > 10)
            {
                yield break;
            }

            var node = nodeArray[i];
            byteSizeBudgetLeft -= node.EstimatedByteSize;

            yield return node;
        }
    }
}
