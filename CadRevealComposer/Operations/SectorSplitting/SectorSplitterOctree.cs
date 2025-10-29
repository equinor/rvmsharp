namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IdProviders;
using Primitives;
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

    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var allNodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);
        (Node[] regularNodes, Node[] outlierNodes) = allNodes.SplitNodesIntoRegularAndOutlierNodes();
        var boundingBoxEncapsulatingAllNodes = allNodes.CalculateBoundingBox();
        var boundingBoxEncapsulatingMostNodes = regularNodes.CalculateBoundingBox();

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        const string rootPath = "/0";

        yield return SplittingUtils.CreateRootSector(rootSectorId, rootPath, boundingBoxEncapsulatingAllNodes);

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
            $"Tried to convert {TooFewPrimitivesHandler.TriedConvertedGroupsOfPrimitives} out of {TooFewPrimitivesHandler.TotalGroupsOfPrimitive} total groups of primitives"
        );
        Console.WriteLine(
            $"Successfully converted {TooFewPrimitivesHandler.SuccessfullyConvertedGroupsOfPrimitives} groups of primitives"
        );
        Console.WriteLine(
            $"This resulted in {TooFewPrimitivesHandler.AdditionalNumberOfTriangles} additional triangles"
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

        using (new TeamCityLogBlock("Outlier Sectors"))
        {
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

                foreach (var sector in outlierSectors)
                {
                    // Mark this sector as an outlier sector
                    Console.WriteLine(
                        $"Outlier-sector with id {sector.SectorId}, path {sector.Path}, {sector.Geometries.Length} geometries added at depth {sector.Depth}."
                    );
                    if (sector.SplittingStats.SplitReason == SplitReason.None)
                    {
                        yield return sector with
                        {
                            SplittingStats = sector.SplittingStats with { SplitReason = SplitReason.Outlier },
                        };
                    }
                    else
                    {
                        yield return sector;
                    }
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
        SplitReason splitReason = SplitReason.None;
        BudgetInfo? budgetInfo = null;

        if (recursiveDepth < depthToStartSplittingGeometry)
        {
            subVoxelNodes = nodes;
        }
        else
        {
            // fill main voxel according to budget
            var (additionalMainVoxelNodesByBudget, budgetSplitReason, budgetInfoResult) = GetNodesByBudget(
                nodes.ToArray(),
                SectorEstimatedByteSizeBudget,
                actualDepth
            );
            mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
            subVoxelNodes = nodes.Except(mainVoxelNodes).ToArray();
            splitReason = budgetSplitReason;
            budgetInfo = budgetInfoResult;
        }

        if (!subVoxelNodes.Any())
        {
            var sectorId = (uint)sectorIdGenerator.GetNextId();

            yield return SplittingUtils.CreateSectorWithPrimitiveHandling(
                mainVoxelNodes,
                sectorId,
                parentSectorId,
                parentPath,
                actualDepth,
                subtreeBoundingBox,
                splitReason,
                budgetInfo
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

                yield return SplittingUtils.CreateSectorWithPrimitiveHandling(
                    mainVoxelNodes,
                    sectorId,
                    parentSectorId,
                    parentPath,
                    actualDepth,
                    subtreeBoundingBox,
                    splitReason,
                    budgetInfo
                );

                parentPathForChildren = path;
                parentSectorIdForChildren = sectorId;
            }

            var subVoxelDiagonal = subVoxelNodes.CalculateBoundingBox().Diagonal;
            var diagonalSmallerThanSplitThreshold = subVoxelDiagonal < DoNotChopSectorsSmallerThanMetersInDiameter;

            var sizeOfSubVoxelNodes = subVoxelNodes.Sum(x => x.EstimatedByteSize);
            var byteSizeBelowBudget = sizeOfSubVoxelNodes < SectorEstimatedByteSizeBudget;

            if (diagonalSmallerThanSplitThreshold || byteSizeBelowBudget)
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
                    // Mark sectors that were created because size threshold was hit
                    if (sector.SplittingStats.SplitReason == SplitReason.None)
                    {
                        yield return sector with
                        {
                            SplittingStats = sector.SplittingStats with { SplitReason = SplitReason.SizeThreshold },
                        };
                    }
                    else
                    {
                        yield return sector;
                    }
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

    private static (IEnumerable<Node> nodes, SplitReason splitReason, BudgetInfo? budgetInfo) GetNodesByBudget(
        IReadOnlyList<Node> nodes,
        long byteSizeBudget,
        int actualDepth
    )
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
        var resultNodes = new List<Node>();
        var splitReason = SplitReason.None;
        BudgetInfo? budgetInfo = null;

        for (int i = 0; i < nodeArray.Length; i++)
        {
            if (
                (byteSizeBudgetLeft < 0 || primitiveBudgetLeft <= 0 || trianglesBudgetLeft <= 0)
                && nodeArray.Length - i > 10
            )
            {
                // Determine which budget(s) were exceeded
                var byteSizeExceeded = byteSizeBudgetLeft < 0;
                var primitiveExceeded = primitiveBudgetLeft <= 0;
                var trianglesExceeded = trianglesBudgetLeft <= 0;

                var exceededCount =
                    (byteSizeExceeded ? 1 : 0) + (primitiveExceeded ? 1 : 0) + (trianglesExceeded ? 1 : 0);

                splitReason = exceededCount switch
                {
                    > 1 => SplitReason.BudgetMultiple,
                    1 when byteSizeExceeded => SplitReason.BudgetByteSize,
                    1 when primitiveExceeded => SplitReason.BudgetPrimitiveCount,
                    1 when trianglesExceeded => SplitReason.BudgetTriangleCount,
                    _ => SplitReason.None,
                };

                // Calculate actual used values
                var byteSizeUsed = byteSizeBudget - byteSizeBudgetLeft;
                var primitiveCountUsed = SectorEstimatedPrimitiveBudget - primitiveBudgetLeft;
                var triangleCountUsed = SectorEstimatesTrianglesBudget - trianglesBudgetLeft;

                // Create budget info with only the exceeded budgets populated
                budgetInfo = new BudgetInfo(
                    ByteSizeBudget: byteSizeExceeded ? byteSizeBudget : null,
                    ByteSizeUsed: byteSizeExceeded ? byteSizeUsed : null,
                    PrimitiveCountBudget: primitiveExceeded ? SectorEstimatedPrimitiveBudget : null,
                    PrimitiveCountUsed: primitiveExceeded ? primitiveCountUsed : null,
                    TriangleCountBudget: trianglesExceeded ? SectorEstimatesTrianglesBudget : null,
                    TriangleCountUsed: trianglesExceeded ? triangleCountUsed : null
                );

                break;
            }

            var node = nodeArray[i];
            byteSizeBudgetLeft -= node.EstimatedByteSize;
            primitiveBudgetLeft -= node.Geometries.Count(x => x is not (InstancedMesh or TriangleMesh));
            trianglesBudgetLeft -= node.EstimatedTriangleCount;

            resultNodes.Add(node);
        }

        return (resultNodes, splitReason, budgetInfo);
    }
}
