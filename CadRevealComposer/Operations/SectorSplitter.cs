namespace CadRevealComposer.Operations;

using IdProviders;
using Primitives;
using RvmSharp.BatchUtils;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using Utils;

public static class SectorSplitter
{
    private const int MainVoxel = 0, SubVoxelA = 1, SubVoxelB = 2, SubVoxelC = 3, SubVoxelD = 4, SubVoxelE = 5, SubVoxelF = 6, SubVoxelG = 7, SubVoxelH = 8;
    private const int StartDepth = 0;
    private const long SectorEstimatedByteSizeBudget = 1_000_000; // bytes, Arbitrary value

    public record ProtoSector(
        uint SectorId,
        uint? ParentSectorId,
        int Depth,
        string Path,
        APrimitive[] Geometries,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax
    );

    private record Node(
        ulong NodeId,
        bool IsExterior,
        string Area,
        APrimitive[] Geometries,
        long EstimatedByteSize,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax,
        Vector3 Center,
        float Diagonal);

    public static IEnumerable<ProtoSector> SplitIntoSectorsByAreas(APrimitive[] allGeometries)
    {
        static Vector3 GetBinPoint(Vector3 center, float binSizeInMeters)
        {
            return new Vector3(
                MathF.Floor(center.X / binSizeInMeters),
                MathF.Floor(center.Y / binSizeInMeters),
                MathF.Floor(center.Z / binSizeInMeters)
            );
        }

        static Node[] ConvertToNodes(APrimitive[] exterior, APrimitive[] interior)
        {
            var exteriorNodes = exterior.GroupBy(p => p.NodeId, (nodeId, primitives) => (nodeId, primitives, isExterior: true));
            var interiorNodes = interior.GroupBy(p => p.NodeId, (nodeId, primitives) => (nodeId, primitives, isExterior: false));

            return exteriorNodes
                .Concat(interiorNodes)
                .Select(node =>
                {
                    var rvmFile = node.primitives.First().RvmFile as RmvPdmsFile ?? throw new Exception("Should be RmvPdmsFile");
                    var rvmFilename = Path.GetFileNameWithoutExtension(rvmFile.RvmFilePath);
                    var filenameParts = rvmFilename.Split('-');
                    if (filenameParts.Length != 2)
                    {
                        throw new Exception("Filename should consist of AREA-DISCIPLINE");
                    }
                    var area = filenameParts.First();

                    var geometries = node.primitives.ToArray();
                    var boundingBoxMin = geometries.GetBoundingBoxMin();
                    var boundingBoxMax = geometries.GetBoundingBoxMax();
                    var center = (boundingBoxMax - boundingBoxMin) / 2 + boundingBoxMin;
                    return new Node(
                        node.nodeId,
                        node.isExterior,
                        area,
                        geometries,
                        geometries.Sum(DrawCallEstimator.EstimateByteSize),
                        boundingBoxMin,
                        boundingBoxMax,
                        center,
                        Vector3.Distance(boundingBoxMin, boundingBoxMax));
                }).ToArray();
        }

        var sectorIdGenerator = new SequentialIdGenerator();

        var (exterior, interior) = ExteriorSplitter.Split(allGeometries);

        var nodes = ConvertToNodes(exterior, interior);

        var rootBudget = SectorEstimatedByteSizeBudget;
        var rootNodes = nodes
            .OrderByDescending(n => n.IsExterior)
            .ThenByDescending(n => n.Diagonal)
            .TakeWhile(n => n.IsExterior && (rootBudget -= n.EstimatedByteSize) > 0)
            .ToArray();
        var childNodes = nodes
            .Except(rootNodes)
            .ToArray();

        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        yield return new ProtoSector(
            rootSectorId,
            ParentSectorId: null,
            StartDepth,
            $"{rootSectorId}/",
            rootNodes.SelectMany(n => n.Geometries).ToArray(),
            allGeometries.GetBoundingBoxMin(),
            allGeometries.GetBoundingBoxMax()
        );

        // TODO: exterior priority - above or below area
        // TODO: exterior priority - above or below area
        // TODO: exterior priority - above or below area

        var groupings = childNodes
            .GroupBy(n => (n.Area, BinnedMidpoint: GetBinPoint(n.Center, 15)));

        foreach (var grouping in groupings)
        {
            var sectors = SplitIntoSectorsRecursive(
                grouping.ToArray(),
                StartDepth + 1,
                $"{rootSectorId}/",
                rootSectorId,
                sectorIdGenerator);

            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }
    }

    private static IEnumerable<ProtoSector> SplitIntoSectorsRecursive(
        Node[] nodes,
        int recursiveDepth,
        string parentPath,
        uint parentSectorId,
        SequentialIdGenerator sectorIdGenerator)
    {

        /* Recursively divides space into eight voxels of about equal size (each dimension X,Y,Z is divided in half).
         * Note: Voxels might have partial overlap, to place nodes that is between two sectors without duplicating the data.
         * Important: Geometries are grouped by NodeId and the group as a whole is placed into the same voxel (that encloses all the geometries in the group).
         */

        if (nodes.Length == 0)
        {
            yield break;
        }

        var bbMin = nodes.GetBoundingBoxMin();
        var bbMax = nodes.GetBoundingBoxMax();
        var bbMidPoint = bbMin + ((bbMax - bbMin) / 2);

        var mainVoxelNodes = GetNodesByBudget(nodes, SectorEstimatedByteSizeBudget).ToArray();
        var subVoxelNodes = nodes.Except(mainVoxelNodes).ToArray();
        bool isLeaf = subVoxelNodes.Length == 0;

        if (isLeaf)
        {
            var sectorId = (uint)sectorIdGenerator.GetNextId();
            var path = $"{parentPath}/{sectorId}";
            var geometries = nodes.SelectMany(n => n.Geometries).ToArray();
            yield return new ProtoSector(
                sectorId,
                parentSectorId,
                recursiveDepth,
                path,
                geometries,
                bbMin,
                bbMax
            );
        }
        else
        {
            var parentPathForChildren = parentPath;
            var parentSectorIdForChildren = parentSectorId;

            if (mainVoxelNodes.Length != 0)
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var geometries = mainVoxelNodes.SelectMany(node => node.Geometries).ToArray();
                var path = $"{parentPath}/{sectorId}";

                parentPathForChildren = path;
                parentSectorIdForChildren = sectorId;

                yield return new ProtoSector(
                    sectorId,
                    parentSectorId,
                    recursiveDepth,
                    path,
                    geometries,
                    bbMin,
                    bbMax
                );
            }

            var voxels = subVoxelNodes
                .GroupBy(node => CalculateVoxelKeyForNode(node, bbMidPoint))
                .OrderBy(x => x.Key)
                .ToImmutableList();

            foreach (var voxelGroup in voxels)
            {
                if (voxelGroup.Key == MainVoxel)
                {
                    throw new Exception("Main voxel should not appear here. Main voxel should be processed separately.");
                }

                var sectors = SplitIntoSectorsRecursive(
                    voxelGroup.ToArray(),
                    recursiveDepth + 1,
                    parentPathForChildren,
                    parentSectorIdForChildren,
                    sectorIdGenerator);
                foreach (var sector in sectors)
                {
                    yield return sector;
                }
            }
        }
    }

    private static IEnumerable<Node> GetNodesByBudget(Node[] nodes, long budget)
    {
        var nodesInPrioritizedOrder = nodes
            .OrderByDescending(x =>
            {
                var hasTriangleMesh = x.Geometries
                    .OfType<TriangleMesh>()
                    .Any();

                    // Theory: Primitives have more overhead than their byte size. This is not verified.
                    return hasTriangleMesh switch
                {
                    true => x.Diagonal / x.EstimatedByteSize,
                    false => x.Diagonal / (x.EstimatedByteSize * 10)
                };
            }
        );

        var budgetLeft = budget;
        return nodesInPrioritizedOrder
            .TakeWhile(n => (budgetLeft -= n.EstimatedByteSize) > 0);
    }

    private static int CalculateVoxelKeyForGeometry(RvmBoundingBox geometryBoundingBox, Vector3 bbMidPoint)
    {
        return (
                geometryBoundingBox.Center.X < bbMidPoint.X,
                geometryBoundingBox.Center.Y < bbMidPoint.Y,
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

    private static int CalculateVoxelKeyForNode(Node nodeGroupGeometries, Vector3 bbMidPoint)
    {
        var voxelKeyAndUsageCount = new Dictionary<int, int>();

        foreach (var geometry in nodeGroupGeometries.Geometries)
        {
            var voxelKey = CalculateVoxelKeyForGeometry(geometry.AxisAlignedBoundingBox, bbMidPoint);
            var count = voxelKeyAndUsageCount.TryGetValue(voxelKey, out int existingCount) ? existingCount : 0;
            voxelKeyAndUsageCount[voxelKey] = count + 1;
        }

        // Return the voxel key where most of the node's geometries lie
        return voxelKeyAndUsageCount
            .Aggregate((l, r) => l.Value > r.Value ? l : r)
            .Key;
    }

    private static Vector3 GetBoundingBoxMin(this IEnumerable<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty", nameof(nodes));

        return nodes
            .Select(p => p.BoundingBoxMin)
            .Aggregate(new Vector3(float.MaxValue), Vector3.Min);
    }

    private static Vector3 GetBoundingBoxMax(this IEnumerable<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty", nameof(nodes));

        return nodes
            .Select(p => p.BoundingBoxMax)
            .Aggregate(new Vector3(float.MinValue), Vector3.Max);
    }
}
