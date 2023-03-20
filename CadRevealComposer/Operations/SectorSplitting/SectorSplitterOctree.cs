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
    private const float DoNotSplitSectorsSmallerThanMetersInDiameter = 20.0f; // Arbitrary value
    private const int MinDigagonalSizeAtDepth_1 = 7; // arbitrary value for min size at depth 1
    private const int MinDigagonalSizeAtDepth_2 = 5; // arbitrary value for min size at depth 2
    private const int MinDigagonalSizeAtDepth_3 = 3; // arbitrary value for min size at depth 3

    public IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var sectorIdGenerator = new SequentialIdGenerator();
        var nodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);

        var boundingBoxEncapsulatingAllNodes = nodes.CalculateBoundingBox();
        var boundingBoxEncapsulatingMostNodes = nodes.CalculateBoundingBox(keepFactor: 0.995f);


        int depthToStartSplittingGeometry = CalculateStartSplittingDepth(boundingBoxEncapsulatingMostNodes);
        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        var rootPath = "/0";

        yield return CreateRootSector(rootSectorId, rootPath, boundingBoxEncapsulatingAllNodes);

        //Order nodes by diagonal size
        var sortedNodes = nodes.OrderByDescending(n => n.Diagonal).ToArray();

        var sectors = SplitIntoSectorsRecursive(
            sortedNodes,
            1,
            rootPath,
            rootSectorId,
            sectorIdGenerator,
            depthToStartSplittingGeometry
        ).ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
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
        var subVoxelNodes = Array.Empty<Node>();

        if (recursiveDepth < depthToStartSplittingGeometry)
        {
            subVoxelNodes = nodes;
        }
        else
        {
            if (subtreeBoundingBox.Diagonal < DoNotSplitSectorsSmallerThanMetersInDiameter)
            {
                mainVoxelNodes = nodes;
            }
            else
            {
                // fill main voxel according to budget
                var additionalMainVoxelNodesByBudget =
                    GetNodesByBudget(nodes.ToArray(), SectorEstimatedByteSizeBudget, actualDepth).ToList();
                mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
                subVoxelNodes = nodes.Except(additionalMainVoxelNodesByBudget).ToArray();
            }
        }

        if (!subVoxelNodes.Any())
        {
            var sectorId = (uint)sectorIdGenerator.GetNextId();
            var path = $"{parentPath}/{sectorId}";
            var geometries = nodes.SelectMany(n => n.Geometries).ToArray();

            yield return new ProtoSector(
                sectorId,
                parentSectorId,
                actualDepth,
                path,
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

            if (geometries.Any())
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var path = $"{parentPath}/{sectorId}";
                var geometryBb = geometries.CalculateBoundingBox();

                yield return new ProtoSector(
                    sectorId,
                    parentSectorId,
                    actualDepth,
                    path,
                    geometries,
                    subtreeBoundingBox,
                    geometryBb
                );

                parentPathForChildren = path;
                parentSectorIdForChildren = sectorId;
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
            Array.Empty<APrimitive>(),
            subtreeBoundingBox,
            new BoundingBox(Vector3.Zero, Vector3.Zero)
        );
    }

    private static int CalculateStartSplittingDepth(BoundingBox bb)
    {
        // If we start splitting too low in the octree, we might end up with way too many sectors
        // If we start splitting too high, we might get some large sectors with a lot of data, which always will be prioritized

        var sizeOfAllNodes = bb.Diagonal;

        float dividedByTwo = bb.Diagonal / 2;

        var diagonalAtDepth = sizeOfAllNodes;
        int depth = 1;
        // Todo: Arbitrary numbers in this method based on gut feeling.
        const float level1SectorsMaxDiagonal = 150;
        while (diagonalAtDepth > level1SectorsMaxDiagonal || diagonalAtDepth > dividedByTwo)
        {
            diagonalAtDepth /= 2;
            depth++;
        }

        Console.WriteLine(
            $"Diagonal was: {bb.Diagonal:F2}m. Starting splitting at depth {depth}. Expecting a diagonal of maximum {diagonalAtDepth:F2}m");
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