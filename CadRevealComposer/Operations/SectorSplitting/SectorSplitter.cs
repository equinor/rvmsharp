namespace CadRevealComposer.Operations;

using CadRevealComposer.Operations.SectorSplitting;
using IdProviders;
using Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Utils;

public static class SectorSplitter
{
    private const long SectorEstimatedByteSizeBudget = 1_000_000; // bytes, Arbitrary value
    private const float DoNotSplitSectorsSmallerThanMetersInDiameter = 20.0f; // Arbitrary value

    public record ProtoSector(
        uint SectorId,
        uint? ParentSectorId,
        int Depth,
        string Path,
        APrimitive[] Geometries,
        Vector3 SubtreeBoundingBoxMin,
        Vector3 SubtreeBoundingBoxMax,
        Vector3 GeometryBoundingBoxMin,
        Vector3 GeometryBoundingBoxMax
    );

    public record Node(
        ulong NodeId,
        APrimitive[] Geometries,
        long EstimatedByteSize,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax,
        float Diagonal
    );

    public static IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var sectorIdGenerator = new SequentialIdGenerator();

        var nodes = allGeometries
            .GroupBy(p => p.TreeIndex)
            .Select(g =>
            {
                var geometries = g.ToArray();
                var boundingBoxMin = geometries.GetBoundingBoxMin();
                var boundingBoxMax = geometries.GetBoundingBoxMax();
                return new Node(
                    g.Key,
                    geometries,
                    geometries.Sum(DrawCallEstimator.EstimateByteSize),
                    boundingBoxMin,
                    boundingBoxMax,
                    Vector3.Distance(boundingBoxMin, boundingBoxMax));
            })
            .ToArray();


        var bbMin = nodes.GetBoundingBoxMin();
        var bbMax = nodes.GetBoundingBoxMax();
        var sizeOfAllNodes = Vector3.Distance(bbMin, bbMax);

        int depthToStartSplittingGeometry = Math.Min(4, (int)MathF.Sqrt(sizeOfAllNodes / 100f)); // EH, a bit random O:)

        var sectors = SplitIntoSectorsRecursive(
            nodes,
            0,
            "",
            null,
            sectorIdGenerator,
            depthToStartSplittingGeometry).ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
        }
    }

    public static IEnumerable<ProtoSector> SplitIntoSectorsRecursive(
        Node[] nodes,
        int recursiveDepth,
        string parentPath,
        uint? parentSectorId,
        SequentialIdGenerator sectorIdGenerator,
        int depthToStartSplittingGeometry = 0)
    {

        /* Recursively divides space into eight voxels of about equal size (each dimension X,Y,Z is divided in half).
         * Note: Voxels might have partial overlap, to place nodes that is between two sectors without duplicating the data.
         * Important: Geometries are grouped by NodeId and the group as a whole is placed into the same voxel (that encloses all the geometries in the group).
         */

        if (nodes.Length == 0)
        {
            yield break;
        }

        var bbMin = nodes.GetBoundingBoxMin();
        var bbMax = nodes.GetBoundingBoxMax();
        var bbMidPoint = (bbMin + bbMax) / 2;
        var bbSize = Vector3.Distance(bbMin, bbMax);

        var mainVoxelNodes = Array.Empty<Node>();
        var subVoxelNodes = Array.Empty<Node>();
        bool isLeaf = false;

        if (bbSize < DoNotSplitSectorsSmallerThanMetersInDiameter)
        {
            mainVoxelNodes = nodes;
            var estimatedByteSize = mainVoxelNodes.Sum(n => n.EstimatedByteSize);
            isLeaf = true;
        }
        else
        {
            if (recursiveDepth < depthToStartSplittingGeometry)
            {
                subVoxelNodes = nodes;
            }
            else
            {

                // fill main voxel according to budget
                long estimatedByteSize = 0;
                var additionalMainVoxelNodesByBudget = GetNodesByBudget(nodes.ToArray(), SectorEstimatedByteSizeBudget - estimatedByteSize).ToList();
                mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
                subVoxelNodes = nodes.Except(additionalMainVoxelNodesByBudget).ToArray();
            }

            isLeaf = subVoxelNodes.Length == 0;
        }

        if (isLeaf)
        {
            var sectorId = (uint)sectorIdGenerator.GetNextId();
            var path = $"{parentPath}/{sectorId}";
            var geometries = nodes.SelectMany(n => n.Geometries).ToArray();
            yield return new ProtoSector(
                sectorId,
                parentSectorId,
                recursiveDepth,
                path,
                geometries,
                bbMin,
                bbMax,
                bbMin,
                bbMax
            );
        }
        else
        {
            var parentPathForChildren = parentPath;
            var parentSectorIdForChildren = parentSectorId;

            var sectorId = (uint)sectorIdGenerator.GetNextId();
            var geometries = mainVoxelNodes.SelectMany(node => node.Geometries).ToArray();

            var path = $"{parentPath}/{sectorId}";

            parentPathForChildren = path;
            parentSectorIdForChildren = sectorId;

            yield return new ProtoSector(
                sectorId,
                parentSectorId,
                recursiveDepth,
                path,
                geometries,
                bbMin,
                bbMax,
                geometries.Any() ? geometries.GetBoundingBoxMin() : Vector3.Zero,
                geometries.Any() ? geometries.GetBoundingBoxMax() : Vector3.Zero
            );

            var voxels = subVoxelNodes
                    .GroupBy(node => SplittingUtils.CalculateVoxelKeyForNode(node, bbMidPoint))
                    .OrderBy(x => x.Key)
                    .ToImmutableList();

            foreach (var voxelGroup in voxels)
            {
                if (voxelGroup.Key == SplittingUtils.MainVoxel)
                {
                    throw new Exception("Main voxel should not appear here. Main voxel should be processed separately.");
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

    private static IEnumerable<Node> GetNodesByBudget(IReadOnlyList<Node> nodes, long budget)
    {
        var nodesInPrioritizedOrder = nodes
            .OrderByDescending(x => x.Diagonal);

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