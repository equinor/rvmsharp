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

    private const int
        SubVoxelA = 1,
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
        return (geometryBoundingBox.Center.X < bbMidPoint.X, geometryBoundingBox.Center.Y < bbMidPoint.Y,
                geometryBoundingBox.Center.Z < bbMidPoint.Z) switch
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
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty",
                nameof(nodes));
        // This could be done in just one pass if profiling shows its needed.
        var max = nodes.Select(p => p.BoundingBox.Max).Aggregate(new Vector3(float.MinValue), Vector3.Max);
        var min = nodes.Select(p => p.BoundingBox.Min).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        return new BoundingBox(Min: min, Max: max);
    }


    /// <summary>
    /// Calculates a bounding box encapsulating a factor of nodes in the input. Based on distance from the average center.
    /// </summary>
    /// <param name="nodes">Nodes</param>
    /// <param name="keepFactor">Value between 0 and 1 for how many percent of nodes to keep based on distance from center. 1 is 100% and keeps everything.</param>
    /// <exception cref="ArgumentException">List must have 1 or more nodes</exception>
    public static BoundingBox CalculateBoundingBox(this IReadOnlyCollection<Node> nodes, float keepFactor)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty",
                nameof(nodes));

        if (keepFactor is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(keepFactor), keepFactor,
                "Must be > 0 and <= 1");
        }

        Node[] nodesToKeep = nodes.GetNodesExcludingOutliers(keepFactor);
        Console.WriteLine(
            $"Using {nodesToKeep.Length} of {nodes.Count} ({nodesToKeep.Length / (float)nodes.Count:P2}%) to create bounding box of all non-outliers");
        return nodesToKeep.CalculateBoundingBox();
    }

    public static Node[] GetNodesExcludingOutliers(this IReadOnlyCollection<Node> nodes, float keepFactor,
        float paddingFactor = 1.1f)
    {
        var firstNodeCenter = nodes.First().BoundingBox.Center;
        // TODO Optimize me
        var avgCenter = new Vector3(
            nodes.Average(x=>x.BoundingBox.Center.X),
            nodes.Average(x=>x.BoundingBox.Center.Y),
            nodes.Average(x=>x.BoundingBox.Center.Z));


        var percentileNode = nodes.OrderBy(x => Vector3.Distance(x.BoundingBox.Center, avgCenter))
            .Skip((int)(nodes.Count * keepFactor)).FirstOrDefault();
        if (percentileNode == null) return nodes.ToArray();

        float distanceToPercentileNode =
            Vector3.Distance(percentileNode.BoundingBox.Center, avgCenter) * paddingFactor /* Slight Padding */;
        var nodesToKeep = nodes.Where(x => Vector3.Distance(x.BoundingBox.Center, avgCenter) < distanceToPercentileNode)
            .ToArray();
        return nodesToKeep;
    }


    public static Node[] ConvertPrimitivesToNodes(APrimitive[] primitives)
    {
        return primitives
            .GroupBy(p => p.TreeIndex)
            .Select(g =>
            {
                var geometries = g.ToArray();
                var boundingBox = geometries.CalculateBoundingBox();
                return new Node(
                    g.Key,
                    geometries,
                    geometries.Sum(DrawCallEstimator.EstimateByteSize),
                    EstimatedTriangleCount: DrawCallEstimator.Estimate(geometries).EstimatedTriangleCount,
                    boundingBox);
            })
            .ToArray();
    }

    public static ProtoSector CreateRootSector(uint sectorId, string path, BoundingBox subTreeBoundingBox,
        BoundingBox geometryBoundingBox)
    {
        return new ProtoSector(
            sectorId,
            null,
            0,
            path,
            0,
            0,
            Array.Empty<APrimitive>(),
            subTreeBoundingBox,
            geometryBoundingBox
        );
    }
}