namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.Operations.SectorSplitting;
using IdProviders;
using Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Utils;
using static CadRevealComposer.Operations.SectorSplitting.SplittingUtils;

public class SectorSplitterOctree : ISectorSplitter
{
    private const long SectorEstimatedByteSizeBudget = 1_000_000; // bytes, Arbitrary value
    private const float DoNotSplitSectorsSmallerThanMetersInDiameter = 20.0f; // Arbitrary value

    public IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
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

        int depthToStartSplittingGeometry =
            3; //Math.Min(4, (int)MathF.Sqrt(sizeOfAllNodes / 100f)); // EH, a bit random O:)

        // var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        // var rootPath = "/0";

        // // Root sector
        // yield return new ProtoSector(
        //     rootSectorId,
        //     null,
        //     0,
        //     rootPath,
        //     Array.Empty<APrimitive>(),
        //     bbMin,
        //     bbMax,
        //     Vector3.Zero,
        //     Vector3.Zero
        // );

        var sectors = SplitIntoSectorsRecursive(
            nodes,
            0,
            "",
            null,
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
                var additionalMainVoxelNodesByBudget =
                    GetNodesByBudget(nodes.ToArray(), SectorEstimatedByteSizeBudget - estimatedByteSize).ToList();
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
                actualDepth,
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
            string parentPathForChildren = parentPath;
            uint? parentSectorIdForChildren = parentSectorId;

            var geometries = mainVoxelNodes.SelectMany(node => node.Geometries).ToArray();

            if (recursiveDepth == 0) // Create root sector
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var path = $"{parentPath}/{sectorId}";

                yield return new ProtoSector(
                    sectorId,
                    parentSectorId,
                    0,
                    path,
                    Array.Empty<APrimitive>(),
                    bbMin,
                    bbMax,
                    Vector3.Zero,
                    Vector3.Zero
                );

                parentPathForChildren = path;
                parentSectorIdForChildren = sectorId;
            }
            else if (geometries.Any())
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var path = $"{parentPath}/{sectorId}";

                yield return new ProtoSector(
                    sectorId,
                    parentSectorId,
                    actualDepth,
                    path,
                    geometries,
                    bbMin,
                    bbMax,
                    geometries.Any() ? geometries.GetBoundingBoxMin() : Vector3.Zero,
                    geometries.Any() ? geometries.GetBoundingBoxMax() : Vector3.Zero
                );

                parentPathForChildren = path;
                parentSectorIdForChildren = sectorId;
            }

            var voxels = subVoxelNodes
                .GroupBy(node => SplittingUtils.CalculateVoxelKeyForNode(node, bbMidPoint))
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

    private IEnumerable<Node> GetNodesByBudget(IReadOnlyList<Node> nodes, long budget)
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