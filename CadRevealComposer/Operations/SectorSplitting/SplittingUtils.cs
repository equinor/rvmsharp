namespace CadRevealComposer.Operations.SectorSplitting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Primitives;
using Utils;

public static class SplittingUtils
{
    public const int MainVoxel = 0;

    private const int SubVoxelA = 1,
        SubVoxelB = 2,
        SubVoxelC = 3,
        SubVoxelD = 4,
        SubVoxelE = 5,
        SubVoxelF = 6,
        SubVoxelG = 7,
        SubVoxelH = 8;

    public static int CalculateVoxelKeyForNode(Node nodeGroupGeometries, BoundingBox boundingBox)
    {
        var voxelKeyAndUsageCount = new Dictionary<int, int>();

        foreach (var geometry in nodeGroupGeometries.Geometries)
        {
            var voxelKey = CalculateVoxelKeyForGeometry(geometry.AxisAlignedBoundingBox, boundingBox);
            var count = voxelKeyAndUsageCount.TryGetValue(voxelKey, out int existingCount) ? existingCount : 0;
            voxelKeyAndUsageCount[voxelKey] = count + 1;
        }

        // Return the voxel key where most of the node's geometries lie
        return voxelKeyAndUsageCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
    }

    private static int CalculateVoxelKeyForGeometry(BoundingBox geometryBoundingBox, BoundingBox boundingBox)
    {
        var bbMidPoint = boundingBox.Center;
        return (
            geometryBoundingBox.Center.X < bbMidPoint.X,
            geometryBoundingBox.Center.Y < bbMidPoint.Y,
            geometryBoundingBox.Center.Z < bbMidPoint.Z
        ) switch
        {
            (false, false, false) => SubVoxelA,
            (false, false, true) => SubVoxelB,
            (false, true, false) => SubVoxelC,
            (false, true, true) => SubVoxelD,
            (true, false, false) => SubVoxelE,
            (true, false, true) => SubVoxelF,
            (true, true, false) => SubVoxelG,
            (true, true, true) => SubVoxelH
        };
    }

    /// <summary>
    /// Calculates a bounding box encapsulating all nodes in the list.
    /// List must have 1 or more nodes.
    /// </summary>
    /// <exception cref="ArgumentException">List must have 1 or more nodes</exception>
    public static BoundingBox CalculateBoundingBox(this IReadOnlyCollection<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException(
                $"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty",
                nameof(nodes)
            );
        // This could be done in just one pass if profiling shows its needed.
        var max = nodes.Select(p => p.BoundingBox.Max).Aggregate(new Vector3(float.MinValue), Vector3.Max);
        var min = nodes.Select(p => p.BoundingBox.Min).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        return new BoundingBox(Min: min, Max: max);
    }

    /// <summary>
    /// Split Nodes into groups of "regular nodes" and outlier nodes. Outliers are based on distance from the average truncated center.
    /// </summary>
    /// <param name="nodes">Nodes</param>
    /// <param name="outlierDistance">The minimum distance between most distant "regular" node and first outlier node.  Default 20.0 </param>
    /// <returns>
    /// (Node[] regularNodes, Node[] outlierNodes)
    /// </returns>
    public static (Node[] regularNodes, Node[] outlierNodes) SplitNodesIntoRegularAndOutlierNodes(
        this IReadOnlyCollection<Node> nodes,
        float outlierDistance = 20.0f
    )
    {
        var truncatedAverage = TruncatedAverageCenter(nodes);

        var orderedNodes = nodes.OrderBy(x => Vector3.Distance(x.BoundingBox.Center, truncatedAverage)).ToArray();
        var orderedNodeDistances = orderedNodes
            .Select(x => Vector3.Distance(x.BoundingBox.Center, truncatedAverage))
            .ToArray();

        for (int i = 1; i < orderedNodeDistances.Length; i++)
        {
            if (orderedNodeDistances[i] - orderedNodeDistances[i - 1] >= outlierDistance)
            {
                var regularNodes = orderedNodes.Take(i).ToArray();
                var outlierNodes = orderedNodes.Skip(i).ToArray();
                return (regularNodes, outlierNodes);
            }
        }

        return (nodes.ToArray(), []);
    }

    /// <summary>
    /// Calculates the truncated average center for a collection of nodes.
    /// Discards the first and last 5% of values in an ascending ordered collection of the nodes
    /// to avoid trouble with outliers.
    /// </summary>
    private static Vector3 TruncatedAverageCenter(this IReadOnlyCollection<Node> nodes)
    {
        var avgCenterX = AvgCenter(nodes.Select(node => node.BoundingBox.Center.X));
        var avgCenterY = AvgCenter(nodes.Select(node => node.BoundingBox.Center.Y));
        var avgCenterZ = AvgCenter(nodes.Select(node => node.BoundingBox.Center.Z));

        return new Vector3(avgCenterX, avgCenterY, avgCenterZ);

        float AvgCenter(IEnumerable<float> values)
        {
            var discardCount = (int)(nodes.Count * 0.05);
            var keepCount = nodes.Count - 2 * discardCount;
            return values.Order().Skip(discardCount).Take(keepCount).Average();
        }
    }

    public static Node[] ConvertPrimitivesToNodes(IEnumerable<APrimitive> primitives)
    {
        return primitives
            .Select(primitive =>
            {
                APrimitive[] primitiveGeometry = [primitive];
                var boundingBox = primitiveGeometry.CalculateBoundingBox();
                if (boundingBox == null)
                {
                    throw new Exception("Unexpected error, the bounding box should not have been null.");
                }
                return new Node(
                    primitive.TreeIndex,
                    [primitive],
                    primitiveGeometry.Sum(DrawCallEstimator.EstimateByteSize),
                    EstimatedTriangleCount: DrawCallEstimator.Estimate(primitiveGeometry).EstimatedTriangleCount,
                    boundingBox
                );
            })
            .ToArray();
    }

    /// <summary>
    /// Group outliers based on outlierGroupingDistance.
    /// This is done recursively to avoid the problem where nodes are at equal distance to the measuring point,
    /// but not actually close to each other.
    /// </summary>
    public static IEnumerable<Node[]> GroupOutliersRecursive(Node[] outlierNodes, float outlierGroupingDistance)
    {
        var groups = GroupOutliers(outlierNodes, outlierNodes[0].BoundingBox.Center, outlierGroupingDistance);

        if (groups.Count == 1)
        {
            yield return groups[0];
            yield break;
        }

        // Try to handle nodes in a group that were symmetrical about the distance measure point
        foreach (var group in groups)
        {
            var subGroups = GroupOutliersRecursive(group, outlierGroupingDistance);
            foreach (var subGroup in subGroups)
            {
                yield return subGroup;
            }
        }
    }

    private static List<Node[]> GroupOutliers(
        Node[] outlierNodes,
        Vector3 distanceMeasurementPoint,
        float outlierGroupingDistance
    )
    {
        var sortedOutlierNodes = outlierNodes
            .OrderBy(x => Vector3.Distance(distanceMeasurementPoint, x.BoundingBox.Center))
            .ToArray();

        var outlierDistances = sortedOutlierNodes
            .Select(x => Vector3.Distance(distanceMeasurementPoint, x.BoundingBox.Center))
            .ToArray();

        var listOfGroups = new List<Node[]>();
        var currentGroup = new List<Node>();
        for (int i = 0; i < sortedOutlierNodes.Length; i++)
        {
            currentGroup.Add(sortedOutlierNodes[i]);

            var isLastIteration = i == sortedOutlierNodes.Length - 1;
            if (isLastIteration || outlierDistances[i + 1] - outlierDistances[i] > outlierGroupingDistance)
            {
                listOfGroups.Add(currentGroup.ToArray());
                currentGroup.Clear();
            }
        }

        return listOfGroups;
    }

    public static InternalSector CreateRootSector(uint sectorId, string path, BoundingBox subtreeBoundingBox)
    {
        return new InternalSector(sectorId, null, 0, path, 0, 0, Array.Empty<APrimitive>(), subtreeBoundingBox, null);
    }

    public static InternalSector CreateSector(
        Node[] nodes,
        uint sectorId,
        InternalSector parent,
        BoundingBox subtreeBoundingBox
    )
    {
        var minDiagonal = nodes.Any() ? nodes.Min(n => n.Diagonal) : 0;
        var maxDiagonal = nodes.Any() ? nodes.Max(n => n.Diagonal) : 0;
        var geometries = nodes.SelectMany(n => n.Geometries).ToArray();
        var geometryBoundingBox = geometries.CalculateBoundingBox();

        var path = parent.Path + "/" + sectorId;

        return new InternalSector(
            sectorId,
            parent.SectorId,
            parent.Depth + 1,
            path,
            minDiagonal,
            maxDiagonal,
            geometries,
            subtreeBoundingBox,
            geometryBoundingBox
        );
    }

    public static InternalSector CreateSectorWithPrimitiveHandling(
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
        geometries = TooFewInstancesHandler.ConvertInstancesWhenTooFew(geometries);
        if (geometries.Length != geometriesCount)
        {
            throw new Exception(
                $"The number of primitives was changed when running TooFewInstancesHandler from {geometriesCount} to {geometries}"
            );
        }

        // NOTE: This increases triangle count
        geometries = TooFewPrimitivesHandler.ConvertPrimitivesWhenTooFew(geometries);
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
}
