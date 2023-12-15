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

        var dimensions = FindDimensions(startBox, depth);

        var subBoxes = SplitBoxIntoSubBoxes(startBox, dimensions); // TODO find length, width, height

        var placedNodes = new List<Node>();

        foreach (var subBox in subBoxes)
        {
            var nodesByBudget = GetNodesByBudget(nodes, subBox, depth);

            if (nodesByBudget.Any())
            {
                yield return CreateSector(
                    nodesByBudget.ToArray(),
                    (uint)sectorIdGenerator.GetNextId(),
                    parentSectorId,
                    parentPath,
                    depth,
                    nodesByBudget.CalculateBoundingBox()
                );
                placedNodes.AddRange(nodesByBudget);
            }
        }

        var restNodes = nodes.Except(placedNodes).ToArray();

        var sectors = SplitIntoSectorsRecursive(
            restNodes,
            depth + 1,
            parentPath,
            parentSectorId,
            sectorIdGenerator,
            startBox
        );

        foreach (var sector in sectors)
        {
            yield return sector;
        }
    }

    private Vector3 FindDimensions(BoundingBox startBox, int depth)
    {
        return new Vector3(depth); // TODO: Do something smart here. Using primes or something? Try to avoid aligning sector edges
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

        for (int i = 0; i < dimensions.X; i++)
        {
            for (int j = 0; j < dimensions.Y; j++)
            {
                for (int k = 0; k < dimensions.Z; k++)
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

    private static Node[] GetNodesByBudget(Node[] nodes, BoundingBox subBox, int depth)
    {
        var nodesInside = nodes.Where(x => subBox.IsInside(x.BoundingBox.Center)); // TODO: Is it neccessary to go through all nodes, since a subset will be chosen in the end (and they are prioritized)?

        var selectedNodes = depth switch
        {
            1 => nodesInside.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_1).ToArray(),
            2 => nodesInside.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_2).ToArray(),
            3 => nodesInside.Where(x => x.Diagonal >= MinDiagonalSizeAtDepth_3).ToArray(),
            _ => nodesInside.ToArray(),
        };

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
