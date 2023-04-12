namespace CadRevealComposer.Operations.SectorSplitting;

using IdProviders;
using Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Utils;

public class SectorSplitterOctree : ISectorSplitter
{
    private const long SectorEstimatedByteSizeBudget = 2_500_000; // bytes, Arbitrary value
    private const float DoNotChopSectorsSmallerThanMetersInDiameter = 17.4f; // Arbitrary value
    private const float MinDigagonalSizeAtDepth_1 = 7; // arbitrary value for min size at depth 1
    private const float MinDigagonalSizeAtDepth_2 = 4; // arbitrary value for min size at depth 2
    private const float MinDigagonalSizeAtDepth_3 = 1.5f; // arbitrary value for min size at depth 3

    public IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var sectorIdGenerator = new SequentialIdGenerator();


        var nodesAndOutliers = SplittingUtils.ConvertPrimitivesToNodes(allGeometries).GetNodesExcludingOutliers(0.995f);
        var nodesExcludingOutliers = nodesAndOutliers.regularNodes;
        var excludedOutliers = nodesAndOutliers.outlierNodes;
        var allNodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);
        var boundingBoxEncapsulatingAllNodes = allNodes.CalculateBoundingBox();
        var boundingBoxEncapsulatingMostNodes = nodesExcludingOutliers.CalculateBoundingBox();
        var boundingBoxEncapsulatingOutlierNodes = excludedOutliers.CalculateBoundingBox();

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        var rootPath = "/0";

        yield return CreateRootSector(rootSectorId, rootPath, boundingBoxEncapsulatingAllNodes);

        //Order nodes by diagonal size
        var sortedNodes = nodesExcludingOutliers.OrderByDescending(n => n.Diagonal).ToArray();

        var sectors = SplitIntoSectorsRecursive(
            sortedNodes,
            1,
            rootPath,
            rootSectorId,
            sectorIdGenerator,
            CalculateStartSplittingDepth(boundingBoxEncapsulatingMostNodes)).ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
        }

        // Add outliers to special outliers sector
        var excludedOutliersCount = excludedOutliers!=null?excludedOutliers.Length:0;
        if (excludedOutliers != null && excludedOutliersCount > 0)
        {
            Console.WriteLine($"Warning, adding {excludedOutliersCount} outliers to special sector(s).");
            var outlierSectors = SplitIntoSectorsRecursive(
                excludedOutliers.ToArray(),
                20,     // Arbitrary depth for outlier sectors, just to ensure separation from the rest
                rootPath,
                rootSectorId,
                sectorIdGenerator,
                CalculateStartSplittingDepth(boundingBoxEncapsulatingOutlierNodes)).ToArray();

            foreach (var sector in outlierSectors)
            {
                Console.WriteLine($"Outlier-sector with id {sector.SectorId}, path {sector.Path}, {sector.Geometries.Length} geometries added at depth {sector.Depth}.");
                yield return sector;
            }
        }

    }

    public IEnumerable<ProtoSector> SplitIntoSectorsRecursive(
        Node[] nodes,
        int recursiveDepth,
        string parentPath,
        uint? parentSectorId,
        SequentialIdGenerator sectorIdGenerator,
        int depthToStartSplittingGeometry)
    {
        /* Recursively divides space into eight voxels of about equal size (each dimension X,Y,Z is divided in half).
         * Note: Voxels might have partial overlap, to place nodes that is between two sectors without duplicating the data.
         * Important: Geometries are grouped by NodeId and the group as a whole is placed into the same voxel (that encloses all the geometries in the group).
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
            var additionalMainVoxelNodesByBudget =
                GetNodesByBudget(nodes.ToArray(), SectorEstimatedByteSizeBudget, actualDepth).ToList();
            mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
            subVoxelNodes = nodes.Except(mainVoxelNodes).ToArray();
        }

        if (!subVoxelNodes.Any())
        {
            var sectorId = (uint)sectorIdGenerator.GetNextId();
            var path = $"{parentPath}/{sectorId}";
            var geometries = nodes.SelectMany(n => n.Geometries).ToArray();
            var minDiagonal = nodes.Min(x => x.Diagonal);
            var maxDiagonal = nodes.Max(x => x.Diagonal);

            yield return new ProtoSector(
                sectorId,
                parentSectorId,
                actualDepth,
                path,
                minDiagonal,
                maxDiagonal,
                geometries,
                subtreeBoundingBox,
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
                var geometryBb = geometries.CalculateBoundingBox();

                var minDiagonal = mainVoxelNodes.Any() ? mainVoxelNodes.Min(x => x.Diagonal) : 0;
                var maxDiagonal = mainVoxelNodes.Any() ? mainVoxelNodes.Max(x => x.Diagonal) : 0;
                yield return new ProtoSector(
                    sectorId,
                    parentSectorId,
                    actualDepth,
                    path,
                    minDiagonal,
                    maxDiagonal,
                    geometries,
                    subtreeBoundingBox,
                    geometryBb
                );

                parentPathForChildren = path;
                parentSectorIdForChildren = sectorId;
            }

            var sizeOfSubVoxelNodes = subVoxelNodes.Sum(x => x.EstimatedByteSize);
            var subVoxelDiagonal = subVoxelNodes.CalculateBoundingBox().Diagonal;

            if ( subVoxelDiagonal < DoNotChopSectorsSmallerThanMetersInDiameter || sizeOfSubVoxelNodes < SectorEstimatedByteSizeBudget)
            {
                var sectors = SplitIntoSectorsRecursive(
                    subVoxelNodes,
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry);
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
                        "Main voxel should not appear here. Main voxel should be processed separately.");
                }

                var sectors = SplitIntoSectorsRecursive(
                    voxelGroup.ToArray(),
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry);
                foreach (var sector in sectors)
                {
                    yield return sector;
                }
            }
        }
    }

    private static ProtoSector CreateRootSector(uint sectorId, string path, BoundingBox subtreeBoundingBox)
    {
        return new ProtoSector(
            sectorId,
            null,
            0,
            path,
            0, 0,
            Array.Empty<APrimitive>(),
            subtreeBoundingBox,
            new BoundingBox(Vector3.Zero, Vector3.Zero)
        );
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
            $"Diagonal was: {boundingBox.Diagonal:F2}m. Starting splitting at depth {depth}. Expecting a diagonal of maximum {diagonalAtDepth:F2}m");
        return depth;
    }

    private static IEnumerable<Node> GetNodesByBudget(IReadOnlyList<Node> nodes, long budget, int actualDepth)
    {
        var selectedNodes = actualDepth switch
        {
            1 => nodes.Where(x => x.Diagonal >= MinDigagonalSizeAtDepth_1).ToArray(),
            2 => nodes.Where(x => x.Diagonal >= MinDigagonalSizeAtDepth_2).ToArray(),
            3 => nodes.Where(x => x.Diagonal >= MinDigagonalSizeAtDepth_3).ToArray(),
            _ => nodes.ToArray(),
        };

        var nodesInPrioritizedOrder = selectedNodes
            .OrderByDescending(x => x.Diagonal * (1 - (0.01 * x.EstimatedTriangleCount)));

        var budgetLeft = budget;
        var nodeArray = nodesInPrioritizedOrder.ToArray();
        for (int i = 0; i < nodeArray.Length; i++)
        {
            if (budgetLeft < 0 && nodeArray.Length - i > 10)
            {
                yield break;
            }

            var node = nodeArray[i];
            budgetLeft -= node.EstimatedByteSize;
            yield return node;
        }
    }
}