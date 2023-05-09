namespace CadRevealComposer.Operations.SectorSplitting;

using IdProviders;
using Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Utils;

public class SectorSplitterOctree : ISectorSplitter
{
    private const long SectorEstimatedByteSizeBudget = 2_000_000; // bytes, Arbitrary value
    private const long SectorEstimatedPrimitiveBudget = 5_000; // count, Arbitrary value
    private const float DoNotChopSectorsSmallerThanMetersInDiameter = 17.4f; // Arbitrary value
    private const float MinDiagonalSizeAtDepth_1 = 7; // arbitrary value for min size at depth 1
    private const float MinDiagonalSizeAtDepth_2 = 4; // arbitrary value for min size at depth 2
    private const float MinDiagonalSizeAtDepth_3 = 1.5f; // arbitrary value for min size at depth 3
    private const float SplitDetailsThreshold = 0.1f; // arbitrary value for splitting out details from last nodes
    private const int MinimumNumberOfSmallPartsBeforeSplitting = 1000;

    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var sectorIdGenerator = new SequentialIdGenerator();

        var allNodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);
        var (regularNodes, outlierNodes) = allNodes.SplitNodesIntoRegularAndOutlierNodes(0.995f);
        var boundingBoxEncapsulatingAllNodes = allNodes.CalculateBoundingBox();
        var boundingBoxEncapsulatingMostNodes = regularNodes.CalculateBoundingBox();

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        var rootPath = "/0";

        yield return CreateRootSector(rootSectorId, rootPath, boundingBoxEncapsulatingAllNodes);

        //Order nodes by diagonal size
        var sortedNodes = regularNodes.OrderByDescending(n => n.Diagonal).ToArray();

        var sectors = SplitIntoSectorsRecursive(
                sortedNodes,
                1,
                rootPath,
                rootSectorId,
                sectorIdGenerator,
                CalculateStartSplittingDepth(boundingBoxEncapsulatingMostNodes)
            )
            .ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
        }

        // Add outliers to special outliers sector
        var excludedOutliersCount = outlierNodes.Length;
        if (excludedOutliersCount > 0)
        {
            var boundingBoxEncapsulatingOutlierNodes = outlierNodes.CalculateBoundingBox();

            Console.WriteLine($"Warning, adding {excludedOutliersCount} outliers to special sector(s).");
            var outlierSectors = SplitIntoSectorsRecursive(
                    outlierNodes.ToArray(),
                    20, // Arbitrary depth for outlier sectors, just to ensure separation from the rest
                    rootPath,
                    rootSectorId,
                    sectorIdGenerator,
                    CalculateStartSplittingDepth(boundingBoxEncapsulatingOutlierNodes)
                )
                .ToArray();

            foreach (var sector in outlierSectors)
            {
                Console.WriteLine(
                    $"Outlier-sector with id {sector.SectorId}, path {sector.Path}, {sector.Geometries.Length} geometries added at depth {sector.Depth}."
                );
                yield return sector;
            }
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
        /*
         * Recursively divides space into eight voxels of about equal size (each dimension X,Y,Z is divided in half).
         * Note: Voxels might have partial overlap, to place nodes that is between two sectors without duplicating the data.
         */

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
            var lastSectors = HandleLastNodes(nodes, actualDepth, parentSectorId, parentPath, sectorIdGenerator);

            foreach (var sector in lastSectors)
            {
                yield return sector;
            }
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

                yield return CreateSector(
                    mainVoxelNodes,
                    sectorId,
                    parentSectorId,
                    parentPath,
                    actualDepth,
                    subtreeBoundingBox
                );

                parentPathForChildren = $"{parentPath}/{sectorId}";
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

    private IEnumerable<InternalSector> HandleLastNodes(
        Node[] nodes,
        int depth,
        uint? parentSectorId,
        string parentPath,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var sectorId = (uint)sectorIdGenerator.GetNextId();

        var smallNodes = nodes.Where(n => n.Diagonal < SplitDetailsThreshold).ToArray();
        var largeNodes = nodes.Except(smallNodes).ToArray();

        var subtreeBoundingBox = nodes.CalculateBoundingBox();

        if (
            largeNodes.Length > 0
            && smallNodes.Length > MinimumNumberOfSmallPartsBeforeSplitting
            && smallNodes.Any(n => n.Diagonal > 0) // There can be nodes with diagonal = 0, no point in splitting if they're all 0
        )
        {
            yield return CreateSector(largeNodes, sectorId, parentSectorId, parentPath, depth, subtreeBoundingBox);

            var smallNodesSectorId = (uint)sectorIdGenerator.GetNextId();
            var smallNodesParentPath = $"{parentPath}/{sectorId}";

            yield return CreateSector(
                smallNodes,
                smallNodesSectorId,
                sectorId,
                smallNodesParentPath,
                depth + 1,
                smallNodes.CalculateBoundingBox()
            );
        }
        else
        {
            yield return CreateSector(nodes, sectorId, parentSectorId, parentPath, depth, subtreeBoundingBox);
        }
    }

    private InternalSector CreateSector(
        Node[] nodes,
        uint sectorId,
        uint? parentSectorId,
        string parentPath,
        int depth,
        BoundingBox subtreeBoundingBox
    )
    {
        var path = $"{parentPath}/{sectorId}";

        var minDiagonal = nodes.Any() ? nodes.Min(n => n.Diagonal) : 0;
        var maxDiagonal = nodes.Any() ? nodes.Max(n => n.Diagonal) : 0;
        var geometries = nodes.SelectMany(n => n.Geometries).ToArray();
        var geometryBoundingBox = geometries.CalculateBoundingBox();

        return new InternalSector(
            sectorId,
            parentSectorId,
            depth,
            path,
            minDiagonal,
            maxDiagonal,
            geometries,
            subtreeBoundingBox,
            geometryBoundingBox
        );
    }

    private static InternalSector CreateRootSector(uint sectorId, string path, BoundingBox subtreeBoundingBox)
    {
        return new InternalSector(sectorId, null, 0, path, 0, 0, Array.Empty<APrimitive>(), subtreeBoundingBox, null);
    }

    private static int CalculateStartSplittingDepth(BoundingBox boundingBox)
    {
        // If we start splitting too low in the octree, we might end up with way too many sectors
        // If we start splitting too high, we might get some large sectors with a lot of data, which always will be prioritized

        var diagonalAtDepth = boundingBox.Diagonal;
        int depth = 1;
        // Todo: Arbitrary numbers in this method based on gut feeling.
        // Assumes 3 levels of "LOD Splitting":
        // 300x300 for Very large parts
        // 150x150 for large parts
        // 75x75 for > 1 meter parts
        // 37,5 etc by budget
        const float level1SectorsMaxDiagonal = 500;
        while (diagonalAtDepth > level1SectorsMaxDiagonal)
        {
            diagonalAtDepth /= 2;
            depth++;
        }

        Console.WriteLine(
            $"Diagonal was: {boundingBox.Diagonal:F2}m. Starting splitting at depth {depth}. Expecting a diagonal of maximum {diagonalAtDepth:F2}m"
        );
        return depth;
    }

    private static IEnumerable<Node> GetNodesByBudget(IReadOnlyList<Node> nodes, long budget, int actualDepth)
    {
        var selectedNodes = actualDepth switch
        {
            1 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_1).ToArray(),
            2 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_2).ToArray(),
            3 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_3).ToArray(),
            _ => nodes.ToArray(),
        };

        var nodesInPrioritizedOrder = selectedNodes.OrderByDescending(x => x.Diagonal);

        var budgetLeft = budget;
        var nodeArray = nodesInPrioritizedOrder.ToArray();
        var primitiveBudget = SectorEstimatedPrimitiveBudget;
        for (int i = 0; i < nodeArray.Length; i++)
        {
            if (budgetLeft < 0 || primitiveBudget <= 0 && nodeArray.Length - i > 10)
            {
                yield break;
            }

            var node = nodeArray[i];
            budgetLeft -= node.EstimatedByteSize;
            primitiveBudget -= node.Geometries.Count(x => x is not (InstancedMesh or TriangleMesh));
            yield return node;
        }
    }
}
