using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;

namespace CadRevealComposer.Operations.SectorSplitting;

/// <summary>
/// K-D tree sector splitter that uses binary median split on the longest axis
/// combined with visual importance weighting for node prioritization.
/// </summary>
public class SectorSplitterKdTree : ISectorSplitter
{
    private const int OutlierStartDepth = 20;

    public IEnumerable<InternalSector> SplitIntoSectors(
        APrimitive[] allGeometries,
        SequentialIdGenerator sectorIdGenerator
    )
    {
        var allNodes = SplittingUtils.ConvertPrimitivesToNodes(allGeometries);

        // Scale-relative outlier distance: max(20m, 5% of model diagonal)
        var modelDiagonal = allNodes.CalculateBoundingBox().Diagonal;
        var outlierDistance = Math.Max(20f, modelDiagonal * 0.05f);

        (Node[] regularNodes, Node[] outlierNodes) = allNodes.SplitNodesIntoRegularAndOutlierNodes(outlierDistance);
        var boundingBoxEncapsulatingAllNodes = allNodes.CalculateBoundingBox();
        var boundingBoxEncapsulatingMostNodes = regularNodes.CalculateBoundingBox();

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        const string rootPath = "/0";

        yield return SplittingUtils.CreateRootSector(rootSectorId, rootPath, boundingBoxEncapsulatingAllNodes);

        // Sort by visual importance descending instead of diagonal
        var sortedNodes = regularNodes.OrderByDescending(n => n.GetVisualImportance()).ToArray();

        // Capture the root scene diagonal once for the LOD filter. Passing it unchanged through
        // recursion gives a single predictable exponential decay, avoiding double-decay from
        // the subtree diagonal shrinking at each recursive level.
        var rootSceneDiagonal = boundingBoxEncapsulatingMostNodes.Diagonal;

        var sectors = SplitIntoSectorsRecursive(
                sortedNodes,
                1,
                rootPath,
                rootSectorId,
                sectorIdGenerator,
                CalculateStartSplittingDepth(boundingBoxEncapsulatingMostNodes),
                rootSceneDiagonal
            )
            .ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
        }

        if (outlierNodes.Length > 0)
        {
            var outlierGroupingDistance = Math.Max(20f, modelDiagonal * 0.05f);
            // Outliers get their own scene diagonal from their bounding box
            var outlierSceneDiagonal = outlierNodes.CalculateBoundingBox().Diagonal;
            var outlierSectors = HandleOutlierSplitting(
                outlierNodes,
                rootPath,
                rootSectorId,
                sectorIdGenerator,
                outlierGroupingDistance,
                outlierSceneDiagonal
            );
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

    private IEnumerable<InternalSector> HandleOutlierSplitting(
        Node[] outlierNodes,
        string rootPath,
        uint rootSectorId,
        SequentialIdGenerator sectorIdGenerator,
        float outlierGroupingDistance,
        float rootSceneDiagonal
    )
    {
        var outlierGroups = SplittingUtils.GroupOutliersRecursive(outlierNodes, outlierGroupingDistance);

        using (new TeamCityLogBlock("Outlier Sectors"))
        {
            foreach (var outlierGroup in outlierGroups)
            {
                var outlierSectors = SplitIntoSectorsRecursive(
                        outlierGroup,
                        OutlierStartDepth,
                        rootPath,
                        rootSectorId,
                        sectorIdGenerator,
                        0,
                        rootSceneDiagonal
                    )
                    .ToArray();

                foreach (var sector in outlierSectors)
                {
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
        int depthToStartSplittingGeometry,
        float rootSceneDiagonal
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
        SplitReason splitReason = SplitReason.None;
        BudgetInfo? budgetInfo = null;

        if (recursiveDepth < depthToStartSplittingGeometry)
        {
            subVoxelNodes = nodes;
            splitReason = SplitReason.EarlyDepth;
        }
        else
        {
            // Use the stable root scene diagonal for LOD filtering, not the shrinking subtree diagonal
            var (additionalMainVoxelNodesByBudget, budgetSplitReason, budgetInfoResult) = GetNodesByBudget(
                nodes.ToArray(),
                SectorBudgets.EstimatedByteSizeBudget,
                actualDepth,
                rootSceneDiagonal
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

            var sizeOfSubVoxelNodes = subVoxelNodes.Sum(x => x.EstimatedByteSize);
            var byteSizeBelowBudget = sizeOfSubVoxelNodes < SectorBudgets.EstimatedByteSizeBudget;

            // No diagonal-based size threshold needed for the K-D tree (unlike the octree's
            // DoNotChopSectorsSmallerThanMetersInDiameter). The octree's 8-way split creates up
            // to 8 micro-sectors from a small volume — wasteful. The K-D tree's binary split only
            // creates 2 children, and byteSizeBelowBudget naturally terminates after ⌈log₂(S/budget)⌉
            // splits. Removing the diagonal guard also eliminates depth-only SizeThreshold chains
            // that bloat the sector tree without adding spatial discrimination.
            if (byteSizeBelowBudget)
            {
                // Don't spatially split — recurse one level deeper (same as octree's SizeThreshold path)
                foreach (
                    var sector in SplitIntoSectorsRecursive(
                        subVoxelNodes,
                        recursiveDepth + 1,
                        parentPathForChildren,
                        parentSectorIdForChildren,
                        sectorIdGenerator,
                        depthToStartSplittingGeometry,
                        rootSceneDiagonal
                    )
                )
                {
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

            // Binary split on the longest axis at the median
            var extents = subVoxelNodes.CalculateBoundingBox().Extents;
            int longestAxis = GetLongestAxis(extents);

            var sorted = longestAxis switch
            {
                0 => subVoxelNodes.OrderBy(n => n.BoundingBox.Center.X).ToArray(),
                1 => subVoxelNodes.OrderBy(n => n.BoundingBox.Center.Y).ToArray(),
                _ => subVoxelNodes.OrderBy(n => n.BoundingBox.Center.Z).ToArray(),
            };

            int mid = sorted.Length / 2;
            var leftNodes = sorted[..mid];
            var rightNodes = sorted[mid..];

            // Recurse on left half
            foreach (
                var sector in SplitIntoSectorsRecursive(
                    leftNodes,
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry,
                    rootSceneDiagonal
                )
            )
            {
                if (sector.SplittingStats.SplitReason == SplitReason.None)
                {
                    yield return sector with
                    {
                        SplittingStats = sector.SplittingStats with { SplitReason = SplitReason.KdTreeMedian },
                    };
                }
                else
                {
                    yield return sector;
                }
            }

            // Recurse on right half
            foreach (
                var sector in SplitIntoSectorsRecursive(
                    rightNodes,
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator,
                    depthToStartSplittingGeometry,
                    rootSceneDiagonal
                )
            )
            {
                if (sector.SplittingStats.SplitReason == SplitReason.None)
                {
                    yield return sector with
                    {
                        SplittingStats = sector.SplittingStats with { SplitReason = SplitReason.KdTreeMedian },
                    };
                }
                else
                {
                    yield return sector;
                }
            }
        }
    }

    /// <summary>
    /// Returns 0 for X, 1 for Y, 2 for Z — whichever axis has the largest extent.
    /// </summary>
    public static int GetLongestAxis(Vector3 extents)
    {
        if (extents.X >= extents.Y && extents.X >= extents.Z)
            return 0;
        if (extents.Y >= extents.Z)
            return 1;
        return 2;
    }

    /// <summary>
    /// K-D tree halves one axis per level (not three like the octree), so the diagonal
    /// shrinks by roughly √2 per level instead of 2. We model this with /1.414 to start
    /// budget-checking at the right depth — earlier than the octree for the same model size.
    /// </summary>
    private static int CalculateStartSplittingDepth(BoundingBox boundingBox)
    {
        var diagonalAtDepth = boundingBox.Diagonal;
        int depth = 1;
        const float level1SectorsMaxDiagonal = 500;
        // √2 ≈ 1.414: average diagonal reduction per single-axis median split
        const float diagonalReductionPerLevel = 1.414f;
        while (diagonalAtDepth > level1SectorsMaxDiagonal)
        {
            diagonalAtDepth /= diagonalReductionPerLevel;
            depth++;
        }

        Console.WriteLine(
            $"[KdTree] Diagonal was: {boundingBox.Diagonal:F2}m. Starting splitting at depth {depth}. Expecting a diagonal of maximum {diagonalAtDepth:F2}m"
        );
        return depth;
    }

    /// <summary>
    /// Budget filling with continuous LOD filter based on visual importance.
    /// Uses a smooth exponential curve instead of stepped MinDiagonalSizeAtDepth constants.
    /// The sceneDiagonal parameter should be the stable root scene diagonal, not the current subtree's.
    /// </summary>
    private static (IEnumerable<Node> nodes, SplitReason splitReason, BudgetInfo? budgetInfo) GetNodesByBudget(
        IReadOnlyList<Node> nodes,
        long byteSizeBudget,
        int actualDepth,
        float sceneDiagonal
    )
    {
        // Continuous LOD filter: exponentially decreasing minimum visual importance with depth.
        float minVisualImportance = sceneDiagonal / 50f / MathF.Pow(2, actualDepth - 1);
        if (minVisualImportance < 0.1f)
            minVisualImportance = 0f;

        var selectedNodes = nodes.Where(x => x.GetVisualImportance() >= minVisualImportance).ToArray();

        var nodesInPrioritizedOrder = selectedNodes.OrderByDescending(x => x.GetVisualImportance());

        var nodeArray = nodesInPrioritizedOrder.ToArray();
        var byteSizeBudgetLeft = byteSizeBudget;
        var primitiveBudgetLeft = (long)SectorBudgets.EstimatedPrimitiveBudget;
        var trianglesBudgetLeft = (long)SectorBudgets.EstimatedTrianglesBudget;
        var resultNodes = new List<Node>();
        var splitReason = SplitReason.None;
        BudgetInfo? budgetInfo = null;

        for (int i = 0; i < nodeArray.Length; i++)
        {
            var node = nodeArray[i];
            byteSizeBudgetLeft -= node.EstimatedByteSize;
            primitiveBudgetLeft -= node.Geometries.Count(x => x is not (InstancedMesh or TriangleMesh));
            trianglesBudgetLeft -= node.EstimatedTriangleCount;

            resultNodes.Add(node);

            if (
                (byteSizeBudgetLeft < 0 || primitiveBudgetLeft < 0 || trianglesBudgetLeft < 0)
                && nodeArray.Length - i - 1 > SectorBudgets.MinRemainingNodesToEnforceBudget
            )
            {
                (splitReason, budgetInfo) = SplittingUtils.DetermineBudgetExceededInfo(
                    byteSizeBudget,
                    byteSizeBudgetLeft,
                    SectorBudgets.EstimatedPrimitiveBudget,
                    primitiveBudgetLeft,
                    SectorBudgets.EstimatedTrianglesBudget,
                    trianglesBudgetLeft
                );

                break;
            }
        }

        if (splitReason == SplitReason.None && resultNodes.Count > 0)
        {
            splitReason = SplitReason.Leaf;
        }

        return (resultNodes, splitReason, budgetInfo);
    }
}
