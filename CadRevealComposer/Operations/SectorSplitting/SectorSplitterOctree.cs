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
    private const long SectorEstimatedByteSizeBudget = 2_000_000; // bytes, Arbitrary value
    private const long SectorEstimatesTrianglesBudget = 300_000; // triangles, Arbitrary value
    private const long SectorEstimatedPrimitiveBudget = 5_000; // count, Arbitrary value
    private const float DoNotChopSectorsSmallerThanMetersInDiameter = 17.4f; // Arbitrary value
    private const float MinDiagonalSizeAtDepth_1 = 7; // arbitrary value for min size at depth 1
    private const float MinDiagonalSizeAtDepth_2 = 4; // arbitrary value for min size at depth 2
    private const float MinDiagonalSizeAtDepth_3 = 1.5f; // arbitrary value for min size at depth 3

    private const float OutlierGroupingDistance = 20f; // arbitrary distance between nodes before we group them
    private const int OutlierStartDepth = 20; // arbitrary depth for outlier sectors, just to ensure separation from the rest

    private readonly TooFewInstancesHandler _tooFewInstancesHandler = new();
    private readonly TooFewPrimitivesHandler _tooFewPrimitivesHandler = new();

    public IEnumerable<InternalSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var sectorIdGenerator = new SequentialIdGenerator();

        var allNodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);
        (Node[] regularNodes, Node[] outlierNodes) = allNodes.SplitNodesIntoRegularAndOutlierNodes();
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
            // Group and split outliers
            var outlierSectors = HandleOutlierSplitting(outlierNodes, rootPath, rootSectorId, sectorIdGenerator);
            foreach (var sector in outlierSectors)
            {
                yield return sector;
            }
        }

        Console.WriteLine(
            $"Tried to convert {_tooFewPrimitivesHandler.TriedConvertedGroupsOfPrimitives} out of {_tooFewPrimitivesHandler.TotalGroupsOfPrimitive} total groups of primitives"
        );
        Console.WriteLine(
            $"Successfully converted {_tooFewPrimitivesHandler.SuccessfullyConvertedGroupsOfPrimitives} groups of primitives"
        );
        Console.WriteLine(
            $"This resulted in {_tooFewPrimitivesHandler.AdditionalNumberOfTriangles} additional triangles"
        );
    }

    /// <summary>
    /// Group outliers by distance, and run splitting on each separate group
    /// </summary>
    private IEnumerable<InternalSector> HandleOutlierSplitting(
        Node[] outlierNodes,
        string rootPath,
        uint rootSectorId,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var outlierGroups = SplittingUtils.GroupOutliersRecursive(outlierNodes, OutlierGroupingDistance);

        foreach (var outlierGroup in outlierGroups)
        {
            var outlierSectors = SplitIntoSectorsRecursive(
                    outlierGroup,
                    OutlierStartDepth, // Arbitrary depth for outlier sectors, just to ensure separation from the rest
                    rootPath,
                    rootSectorId,
                    sectorIdGenerator,
                    0 // Hackish: This is set to a value a lot lower than OutlierStartDepth to skip size checking in budget
                )
                .ToArray();

            using (new TeamCityLogBlock("Outlier Sectors"))
            {
                foreach (var sector in outlierSectors)
                {
                    Console.WriteLine(
                        $"Outlier-sector with id {sector.SectorId}, path {sector.Path}, {sector.Geometries.Length} geometries added at depth {sector.Depth}."
                    );
                    yield return sector;
                }
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

            yield return CreateSector(
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

                yield return CreateSector(
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

    private InternalSector CreateRootSector(uint sectorId, string path, BoundingBox subtreeBoundingBox)
    {
        return new InternalSector(sectorId, null, 0, path, 0, 0, Array.Empty<APrimitive>(), subtreeBoundingBox, null);
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

        var geometriesCount = geometries.Length;

        // NOTE: This increases triangle count
        geometries = _tooFewInstancesHandler.ConvertInstancesWhenTooFew(geometries);
        if (geometries.Length != geometriesCount)
        {
            throw new Exception(
                $"The number of primitives was changed when running TooFewInstancesHandler from {geometriesCount} to {geometries}"
            );
        }

        // NOTE: This increases triangle count
        geometries = _tooFewPrimitivesHandler.ConvertPrimitivesWhenTooFew(geometries);
        if (geometries.Length != geometriesCount)
        {
            throw new Exception(
                $"The number of primitives was changed when running TooFewPrimitives from {geometriesCount} to {geometries.Length}"
            );
        }

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
        var primitiveBudgetLeft = SectorEstimatedPrimitiveBudget;
        var trianglesBudgetLeft = SectorEstimatesTrianglesBudget;
        for (int i = 0; i < nodeArray.Length; i++)
        {
            if (
                (byteSizeBudgetLeft < 0 || primitiveBudgetLeft <= 0 || trianglesBudgetLeft <= 0)
                && nodeArray.Length - i > 10
            )
            {
                yield break;
            }

            var node = nodeArray[i];
            byteSizeBudgetLeft -= node.EstimatedByteSize;
            primitiveBudgetLeft -= node.Geometries.Count(x => x is not (InstancedMesh or TriangleMesh));
            trianglesBudgetLeft -= node.EstimatedTriangleCount;

            yield return node;
        }
    }
}
