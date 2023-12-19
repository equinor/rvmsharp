namespace CadRevealComposer.Operations.SectorSplitting;

using IdProviders;
using Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                        outlierGroup.CalculateBoundingBox(),
                        false
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
        BoundingBox startBox,
        bool avoidSmallerPartsInFirstDepths = true
    )
    {
        if (nodes.Length <= 0)
        {
            yield break;
        }

        var dimensions = FindDimensions(startBox, depth);

        var subBoxes = SplitBoxIntoSubBoxes(startBox, dimensions); // TODO find length, width, height

        var nodeInBoundingBoxDictionary = PlaceNodesInBoundingBoxes(subBoxes, nodes);

        var placedNodes = new ConcurrentBag<Node>();

        var sectorTests = subBoxes
            .Where(subBox => nodeInBoundingBoxDictionary.ContainsKey(subBox))
            // .AsParallel()
            .Select(subBox =>
            {
                var nodesInBox = nodeInBoundingBoxDictionary[subBox];

                var nodesByBudget = GetNodesByBudget(nodesInBox.ToArray(), depth, avoidSmallerPartsInFirstDepths);

                if (nodesByBudget.Any())
                {
                    foreach (var node in nodesByBudget)
                    {
                        placedNodes.Add(node);
                    }

                    return CreateSector(
                        nodesByBudget.ToArray(),
                        (uint)sectorIdGenerator.GetNextId(),
                        parentSectorId,
                        parentPath,
                        depth,
                        nodesByBudget.CalculateBoundingBox()
                    );
                }

                return null;
            });

        foreach (var sector in sectorTests)
        {
            if (sector != null)
            {
                yield return sector;
            }
        }

        var restNodes = nodes.Except(placedNodes).ToArray();

        var sectors = SplitIntoSectorsRecursive(
            restNodes,
            depth + 1,
            parentPath,
            parentSectorId,
            sectorIdGenerator,
            startBox,
            avoidSmallerPartsInFirstDepths
        );

        foreach (var sector in sectors)
        {
            yield return sector;
        }
    }

    private Dictionary<BoundingBox, List<Node>> PlaceNodesInBoundingBoxes(BoundingBox[] subBoxes, Node[] nodes)
    {
        var min = subBoxes[0].Min;
        var firstMax = subBoxes[0].Max;
        var sideLengths = firstMax - min;

        var dict = new Dictionary<BoundingBox, List<Node>>();
        foreach (var node in nodes)
        {
            var index = CalculateIndex(node, min, sideLengths, subBoxes.Length);
            var subBox = subBoxes[index];

            if (dict.TryGetValue(subBox, out var existingValue))
            {
                existingValue.Add(node);
            }
            else
            {
                dict[subBox] = new List<Node> { node };
            }
        }

        return dict;
    }

    private int CalculateIndex(Node node, Vector3 min, Vector3 sideLengths, int subBoxesLength)
    {
        var boxesOnSide = (int)(MathF.Pow(subBoxesLength, 1.0f / 3));
        var pos = node.BoundingBox.Center;

        int x = (int)((pos.X - min.X) / sideLengths.X);
        int y = (int)((pos.Y - min.Y) / sideLengths.Y);
        int z = (int)((pos.Z - min.Z) / sideLengths.Z);

        return boxesOnSide * boxesOnSide * z + boxesOnSide * y + x;
    }

    private Vector3 FindDimensions(BoundingBox startBox, int depth)
    {
        int[] primes = { 1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71 };
        Vector3 dimensions;

        if (depth >= primes.Length)
        {
            dimensions = new Vector3(primes[primes.Length - 1]);
        }
        else
        {
            dimensions = new Vector3(primes[depth]);
        }

        return dimensions; // TODO: Do something smart here. Using primes or something? Try to avoid aligning sector edges
    }

    private BoundingBox[] SplitBoxIntoSubBoxes(BoundingBox startBox, Vector3 dimensions)
    {
        float xStartMin = startBox.Min.X;
        float yStartMin = startBox.Min.Y;
        float zStartMin = startBox.Min.Z;

        var startBoxLengths = startBox.Max - startBox.Min;
        var xLength = startBoxLengths.X / dimensions.X;
        var yLength = startBoxLengths.Y / dimensions.Y;
        var zLength = startBoxLengths.Z / dimensions.Z;

        var splitBoxes = new List<BoundingBox>();

        for (int k = 0; k < dimensions.Z; k++)
        {
            for (int j = 0; j < dimensions.Y; j++)
            {
                for (int i = 0; i < dimensions.X; i++)
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

    private static Node[] GetNodesByBudget(Node[] nodes, int depth, bool avoidSmallerPartsInFirstDepths)
    {
        IEnumerable<Node> selectedNodes;

        if (avoidSmallerPartsInFirstDepths)
        {
            selectedNodes = depth switch
            {
                1 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_1).ToArray(),
                2 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_2).ToArray(),
                3 => nodes.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_3).ToArray(),
                _ => nodes.ToArray(),
            };
        }
        else
        {
            selectedNodes = nodes;
        }

        var nodesInPrioritizedOrder = selectedNodes.OrderByDescending(x => x.Diagonal); // TODO: Already sorted?

        var nodeArray = nodesInPrioritizedOrder.ToArray();

        var byteSizeBudgetLeft = SectorEstimatedByteSizeBudget;
        var primitiveBudgetLeft = SectorEstimatedPrimitiveBudget;
        var trianglesBudgetLeft = SectorEstimatesTrianglesBudget;

        var chosenNodes = new List<Node>();
        for (int i = 0; i < nodeArray.Length; i++)
        {
            if (
                (byteSizeBudgetLeft < 0 || primitiveBudgetLeft <= 0 || trianglesBudgetLeft <= 0)
                && nodeArray.Length - i > 10
            )
            {
                break;
            }

            var node = nodeArray[i];
            byteSizeBudgetLeft -= node.EstimatedByteSize;
            primitiveBudgetLeft -= node.Geometries.Count(x => x is not (InstancedMesh or TriangleMesh));
            trianglesBudgetLeft -= node.EstimatedTriangleCount;

            chosenNodes.Add(node);
        }

        return chosenNodes.ToArray();
    }
}
