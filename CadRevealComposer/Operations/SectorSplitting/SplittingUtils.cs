namespace CadRevealComposer.Operations.SectorSplitting;

using CadRevealComposer.Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class SplittingUtils
{
    public const int MainVoxel = 0, SubVoxelA = 1, SubVoxelB = 2, SubVoxelC = 3, SubVoxelD = 4, SubVoxelE = 5, SubVoxelF = 6, SubVoxelG = 7, SubVoxelH = 8;

    public record ProtoSector(
        uint SectorId,
        uint? ParentSectorId,
        int Depth,
        string Path,
        APrimitive[] Geometries,
        Vector3 SubtreeBoundingBoxMin,
        Vector3 SubtreeBoundingBoxMax,
        Vector3 GeometryBoundingBoxMin,
        Vector3 GeometryBoundingBoxMax
    );

    public record Node(
        ulong NodeId,
        APrimitive[] Geometries,
        long EstimatedByteSize,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax,
        float Diagonal
    );

    public static int CalculateVoxelKeyForGeometry(RvmBoundingBox geometryBoundingBox, Vector3 bbMidPoint)
    {
        return (geometryBoundingBox.Center.X < bbMidPoint.X, geometryBoundingBox.Center.Y < bbMidPoint.Y, geometryBoundingBox.Center.Z < bbMidPoint.Z) switch
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

    public static int CalculateVoxelKeyForNode(Node nodeGroupGeometries, Vector3 bbMidPoint)
    {
        var voxelKeyAndUsageCount = new Dictionary<int, int>();

        foreach (var geometry in nodeGroupGeometries.Geometries)
        {
            var voxelKey = CalculateVoxelKeyForGeometry(geometry.AxisAlignedBoundingBox, bbMidPoint);
            var count = voxelKeyAndUsageCount.TryGetValue(voxelKey, out int existingCount) ? existingCount : 0;
            voxelKeyAndUsageCount[voxelKey] = count + 1;
        }

        // Return the voxel key where most of the node's geometries lie
        return voxelKeyAndUsageCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
    }

    public static Vector3 GetBoundingBoxMin(this IReadOnlyCollection<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty", nameof(nodes));

        return nodes.Select(p => p.BoundingBoxMin).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
    }

    public static Vector3 GetBoundingBoxMax(this IReadOnlyCollection<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty", nameof(nodes));

        return nodes.Select(p => p.BoundingBoxMax).Aggregate(new Vector3(float.MinValue), Vector3.Max);
    }
}
