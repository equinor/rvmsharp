namespace CadRevealComposer.Operations;

using IdProviders;
using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Utils;

public static class SectorSplitter
{
    private const int MainVoxel = 0,
        SubVoxelA = 1,
        SubVoxelB = 2,
        SubVoxelC = 3,
        SubVoxelD = 4,
        SubVoxelE = 5,
        SubVoxelF = 6,
        SubVoxelG = 7,
        SubVoxelH = 8;

    private const int StartDepth = 0;
    private const long SectorEstimatedByteSizeBudget = 1_000_000; // bytes, Arbitrary value
    private const float DoNotSplitSectorsSmallerThanMetersInDiameter = 20.0f; // Arbitrary value

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

    private record Node(
        ulong NodeId,
        APrimitive[] Geometries,
        long EstimatedByteSize,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax,
        float Diagonal);

    public static IEnumerable<ProtoSector> CreateSingleSector(APrimitive[] allGeometries)
    {
        yield return CreateRootSector(0, allGeometries);
    }

    public static IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
    {
        var sectorIdGenerator = new SequentialIdGenerator();

        var nodes = allGeometries
            .GroupBy(p => p.TreeIndex)
            .Select(g =>
            {
                var geometries = g.ToArray();
                var boundingBoxMin = geometries.GetBoundingBoxMin();
                var boundingBoxMax = geometries.GetBoundingBoxMax();
                return new Node(
                    g.Key,
                    geometries,
                    geometries.Sum(DrawCallEstimator.EstimateByteSize),
                    boundingBoxMin,
                    boundingBoxMax,
                    Vector3.Distance(boundingBoxMin, boundingBoxMax));
            })
            .ToArray();


        //var sectors = SplitIntoSectorsRecursive(
        //    nodes,
        //    StartDepth,
        //    "",
        //    null,
        //    sectorIdGenerator).ToArray();

        var sectors = SplitIntoUniformSectors(
            nodes,
            sectorIdGenerator).ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
        }
    }

    private static IEnumerable<ProtoSector> SplitIntoUniformSectors(
        Node[] nodes,
        SequentialIdGenerator sectorIdGenerator)
    {
        if (nodes.Length == 0)
        {
            yield break;
        }

        var bbMin = nodes.GetBoundingBoxMin();
        var bbMax = nodes.GetBoundingBoxMax();

        // Root sector
        var rootSectorId = (uint)sectorIdGenerator.GetNextId();
        var rootSectorPath = $"/{rootSectorId}";

        yield return new ProtoSector(
            rootSectorId,
            null,
            0,
            rootSectorPath,
            Array.Empty<APrimitive>(),
            bbMin,
            bbMax,
            Vector3.Zero,
            Vector3.Zero
        );

        // All the other sectors
        int sectorSideSize = 5; // Size of box, assume cubes

        var xSize = bbMax.X - bbMin.X;
        var ySize = bbMax.Y - bbMin.Y;
        var zSize = bbMax.Z - bbMin.Z;

        var numberOfBoxesOnX = (int)(xSize / sectorSideSize) + 1;
        var numberOfBoxesOnY = (int)(ySize / sectorSideSize) + 1;
        var numberOfBoxesOnZ = (int)(zSize / sectorSideSize) + 1;

        var xDict = new Dictionary<int, Dictionary<int, Dictionary<int, List<Node>>>>();

        for (int x = 0; x < numberOfBoxesOnX; x++)
        {
            xDict.Add(x, new Dictionary<int, Dictionary<int, List<Node>>>());

            for (int y = 0; y < numberOfBoxesOnY; y++)
            {
                var yDict = xDict[x];
                yDict.Add(y, new Dictionary<int, List<Node>>());

                for (int z = 0; z < numberOfBoxesOnZ; z++)
                {
                    var zDict = yDict[y];
                    zDict.Add(z, new List<Node>());
                }
            }
        }

        foreach (var node in nodes)
        {
            var center = (node.BoundingBoxMax + node.BoundingBoxMin) / 2.0f;

            var xMapped = (int)((center.X - bbMin.X) / sectorSideSize);
            var yMapped = (int)((center.Y - bbMin.Y) / sectorSideSize);
            var zMapped = (int)((center.Z - bbMin.Z) / sectorSideSize);

            xDict[xMapped][yMapped][zMapped].Add(node);
        }

        for (int x = 0; x < numberOfBoxesOnX; x++)
        {
            for (int y = 0; y < numberOfBoxesOnY; y++)
            {
                for (int z = 0; z < numberOfBoxesOnZ; z++)
                {
                    var geometries = xDict[x][y][z].SelectMany(n => n.Geometries).ToArray();

                    if (geometries.Length == 0)
                        continue;

                    var smallSizeThreshold = 1.0f;
                    var mediumSizeThreshold = 4.0f;

                    var smallGeometryList = new List<APrimitive>();
                    var mediumGeometryList = new List<APrimitive>();
                    var largeGeometryList = new List<APrimitive>();

                    foreach (var geometry in geometries)
                    {
                        if (geometry.AxisAlignedBoundingBox.Diagonal < smallSizeThreshold)
                        {
                            smallGeometryList.Add(geometry);
                        }
                        else if (geometry.AxisAlignedBoundingBox.Diagonal < mediumSizeThreshold)
                        {
                            mediumGeometryList.Add(geometry);
                        }
                        else
                        {
                            largeGeometryList.Add(geometry);
                        }
                    }

                    var smallGeometryArray = smallGeometryList.ToArray();
                    var mediumGeometryArray = mediumGeometryList.ToArray();
                    var largeGeometryArray = largeGeometryList.ToArray();

                    var largeSectorId = (uint)sectorIdGenerator.GetNextId();
                    var largeSectorPath = $"{rootSectorPath}/{largeSectorId}";

                    yield return new ProtoSector(
                        largeSectorId,
                        rootSectorId,
                        1,
                        largeSectorPath,
                        largeGeometryArray,
                        geometries.GetBoundingBoxMin(),
                        geometries.GetBoundingBoxMax(),
                        largeGeometryArray.GetBoundingBoxMin(),
                        largeGeometryArray.GetBoundingBoxMax()
                    );

                    var smallChildSectorId = (uint)sectorIdGenerator.GetNextId();
                    var smallChildPath = $"{rootSectorPath}/{largeSectorId}/{smallChildSectorId}";
                    var mediumChildSectorId = (uint)sectorIdGenerator.GetNextId();
                    var mediumChildPath = $"{rootSectorPath}/{largeSectorId}/{mediumChildSectorId}";

                    if (smallGeometryArray.Length > 0)
                    {
                        yield return new ProtoSector(
                            smallChildSectorId,
                            largeSectorId,
                            2,
                            smallChildPath,
                            smallGeometryArray,
                            smallGeometryArray.GetBoundingBoxMin(),
                            smallGeometryArray.GetBoundingBoxMax(),
                            smallGeometryArray.GetBoundingBoxMin(),
                            smallGeometryArray.GetBoundingBoxMax()
                        );
                    }

                    if (mediumGeometryArray.Length > 0)
                    {
                        yield return new ProtoSector(
                            mediumChildSectorId,
                            largeSectorId,
                            2,
                            mediumChildPath,
                            mediumGeometryArray,
                            mediumGeometryArray.GetBoundingBoxMin(),
                            mediumGeometryArray.GetBoundingBoxMax(),
                            mediumGeometryArray.GetBoundingBoxMin(),
                            mediumGeometryArray.GetBoundingBoxMax()
                        );
                    }
                }
            }
        }
    }

    private static ProtoSector CreateRootSector(uint sectorId, APrimitive[] geometries)
    {
        var bbMin = geometries.GetBoundingBoxMin();
        var bbMax = geometries.GetBoundingBoxMax();
        return new ProtoSector(
            sectorId,
            ParentSectorId: null,
            StartDepth,
            $"{sectorId}",
            geometries,
            bbMin,
            bbMax,
            Vector3.Zero,
            Vector3.Zero
        );
    }

    private static Vector3 GetBoundingBoxMin(this IReadOnlyCollection<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty",
                nameof(nodes));

        return nodes.Select(p => p.BoundingBoxMin).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
    }

    private static Vector3 GetBoundingBoxMax(this IReadOnlyCollection<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty",
                nameof(nodes));

        return nodes.Select(p => p.BoundingBoxMax).Aggregate(new Vector3(float.MinValue), Vector3.Max);
    }
}