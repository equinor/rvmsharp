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
                sortedNodes.CalculateBoundingBox()
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

        using (new TeamCityLogBlock("Outlier Sectors"))
        {
            foreach (var outlierGroup in outlierGroups)
            {
                var outlierSectors = SplitIntoSectorsRecursive(
                        outlierGroup,
                        1, // Arbitrary depth for outlier sectors, just to ensure separation from the rest
                        rootPath,
                        rootSectorId,
                        sectorIdGenerator,
                        outlierGroup.CalculateBoundingBox()
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
    }

    private IEnumerable<InternalSector> SplitIntoSectorsRecursive(
        Node[] nodes,
        int depth,
        string parentPath,
        uint? parentSectorId,
        SequentialIdGenerator sectorIdGenerator,
        BoundingBox startBox
    )
    {
        if (nodes.Length <= 0)
        {
            yield break;
        }

        var subBoxes = SplitBoxIntoSubBoxes(startBox, depth, depth, depth); // TODO find length, width, height

        var placedNodes = new List<Node>();
        var sectors = new List<InternalSector>();

        foreach (var subBox in subBoxes)
        {
            int tempBudget = 10000;
            var sectorNodes = new List<Node>();

            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (subBox.IsInside(node.BoundingBox.Center))
                {
                    sectorNodes.Add(node);
                    tempBudget--;
                }

                if (tempBudget <= 0)
                    break;
            }

            if (sectorNodes.Any())
            {
                sectors.Add(
                    CreateSector(
                        sectorNodes.ToArray(),
                        (uint)sectorIdGenerator.GetNextId(),
                        parentSectorId,
                        parentPath,
                        depth,
                        sectorNodes.CalculateBoundingBox()
                    )
                );
                placedNodes.AddRange(sectorNodes);
            }
        }

        var restNodes = nodes.Except(placedNodes).ToArray();

        sectors.AddRange(
            SplitIntoSectorsRecursive(restNodes, depth + 1, parentPath, parentSectorId, sectorIdGenerator, startBox)
        );

        foreach (var sector in sectors)
        {
            yield return sector;
        }
    }

    private BoundingBox[] SplitBoxIntoSubBoxes(BoundingBox startBox, int xBoxes, int yBoxes, int zBoxes)
    {
        float xStartMin = startBox.Min.X;
        float yStartMin = startBox.Min.Y;
        float zStartMin = startBox.Min.Z;

        var startBoxLengths = startBox.Max - startBox.Min;
        var xLength = startBoxLengths.X / xBoxes;
        var yLength = startBoxLengths.Y / yBoxes;
        var zLength = startBoxLengths.Z / zBoxes;

        var splitBoxes = new List<BoundingBox>();

        for (int i = 0; i < xBoxes; i++)
        {
            for (int j = 0; j < yBoxes; j++)
            {
                for (int k = 0; k < zBoxes; k++)
                {
                    var newMin = new Vector3(xStartMin + xLength * i, yStartMin + yLength * j, zStartMin + zLength * k);
                    var newMax = new Vector3(newMin.X + xLength, newMin.Y + yLength, newMin.Z + zLength);

                    splitBoxes.Add(new BoundingBox(newMin, newMax));
                }
            }
        }

        return splitBoxes.ToArray();
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
