namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

        bool outliersExist = false;
        int outlierStartIndex = 0;
        for (int i = 1; i < orderedNodes.Length; i++)
        {
            var firstDistance = Vector3.Distance(orderedNodes[i - 1].BoundingBox.Center, truncatedAverage);
            var secondDistance = Vector3.Distance(orderedNodes[i].BoundingBox.Center, truncatedAverage);
            if (secondDistance - firstDistance >= outlierDistance)
            {
                outliersExist = true;
                outlierStartIndex = i;
                break;
            }
        }

        if (!outliersExist)
        {
            return (nodes.ToArray(), Array.Empty<Node>());
        }

        var regularNodes = orderedNodes.Take(outlierStartIndex).ToArray();
        var outlierNodes = orderedNodes.Skip(outlierStartIndex).ToArray();

        return (regularNodes, outlierNodes);
    }

    /// <summary>
    /// Calculates the truncated average center for a collection of nodes.
    /// Discards the first and last 5% of values in an ascending ordered collection of the nodes
    /// to avoid trouble with outliers.
    /// </summary>
    private static Vector3 TruncatedAverageCenter(this IReadOnlyCollection<Node> nodes)
    {
        var avgCenterX = nodes
            .OrderBy(x => x.BoundingBox.Center.X)
            .Skip((int)(nodes.Count * 0.05))
            .Take((int)(nodes.Count * 0.95))
            .Average(x => x.BoundingBox.Center.X);

        var avgCenterY = nodes
            .OrderBy(x => x.BoundingBox.Center.Y)
            .Skip((int)(nodes.Count * 0.05))
            .Take((int)(nodes.Count * 0.95))
            .Average(x => x.BoundingBox.Center.Y);

        var avgCenterZ = nodes
            .OrderBy(x => x.BoundingBox.Center.Z)
            .Skip((int)(nodes.Count * 0.05))
            .Take((int)(nodes.Count * 0.95))
            .Average(x => x.BoundingBox.Center.Z);

        return new Vector3(avgCenterX, avgCenterY, avgCenterZ);
    }

    public static Node[] ConvertPrimitivesToNodes(APrimitive[] primitives)
    {
        return primitives
            .Select(g =>
            {
                var geometries = new[] { g };
                var boundingBox = geometries.CalculateBoundingBox();
                if (boundingBox == null)
                {
                    throw new Exception("Unexpected error, the bounding box should not have been null.");
                }
                return new Node(
                    g.TreeIndex,
                    geometries,
                    geometries.Sum(DrawCallEstimator.EstimateByteSize),
                    EstimatedTriangleCount: DrawCallEstimator.Estimate(geometries).EstimatedTriangleCount,
                    boundingBox
                );
            })
            .ToArray();
    }

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
}
