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
        var max =  nodes.Select(p => p.BoundingBox.Max).Aggregate(new Vector3(float.MinValue), Vector3.Max);
        var min = nodes.Select(p => p.BoundingBox.Min).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        return new BoundingBox(Min: min, Max: max);
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
                    boundingBox);
            })
            .ToArray();
    }

    public static ProtoSector CreateRootSector(uint sectorId, string path, BoundingBox subTreeBoundingBox, BoundingBox geometryBoundingBox)
    {
        return new ProtoSector(
            sectorId,
            null,
            0,
            path,
            Array.Empty<APrimitive>(),
            subTreeBoundingBox,
            geometryBoundingBox
        );
    }
}