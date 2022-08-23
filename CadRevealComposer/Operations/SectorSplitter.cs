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
    private const int MainVoxel = 0, SubVoxelA = 1, SubVoxelB = 2, SubVoxelC = 3, SubVoxelD = 4, SubVoxelE = 5, SubVoxelF = 6, SubVoxelG = 7, SubVoxelH = 8;
    private const int StartDepth = 0;
    private const long SectorEstimatedByteSizeBudget = 1_000_000; // bytes, Arbitrary value
    private const float DoNotSplitSectorsSmallerThanMetersInDiameter = 20.0f; // Arbitrary value

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
        APrimitive[] Geometries,
        long EstimatedByteSize,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax,
        float Diagonal);

    public static IEnumerable<ProtoSector> SplitIntoSectors(ZoneSplitter.Zone[] zones, bool createEmptyRootSector=false)
    {
        var sectorIdGenerator = new SequentialIdGenerator();

        if (createEmptyRootSector)
        {
            var rootSector = CreateEmptyRootSector((uint)sectorIdGenerator.GetNextId(), zones.First().SceneBoundingBoxMin, zones.First().SceneBoundingBoxMax);
            yield return rootSector; // Root sector, containing no nodes
            var rootZone = zones.FirstOrDefault(z => z is ZoneSplitter.RootZone);
            if (rootZone != null)
            {
                var nodes = GetNodesInZone(rootZone);
                var rootSectorLevel2 = CreateEmptyRootSector((uint)sectorIdGenerator.GetNextId(), rootZone.SceneBoundingBoxMin, rootZone.SceneBoundingBoxMax);
                yield return rootSectorLevel2; // Root sector, containing no nodes

                var sectors = SplitIntoSectorsRecursive(
                    nodes,
                    StartDepth + 1,
                    $"0/",
                    0,
                    sectorIdGenerator).ToArray();

                foreach (var sector in sectors)
                {
                    yield return sector;
                }

            }
        }
        else
        {
            var rootZone = zones.FirstOrDefault(z => z is ZoneSplitter.RootZone);
            if (rootZone == null)
            {
                Console.WriteLine("WARNING: RootZone is missing."); //Consider throwing if this hits?
            }
            else
            {
                var nodes = GetNodesInZone(rootZone);

                var sectors = SplitIntoSectorsRecursive(
                    nodes,
                    StartDepth,
                    "",
                    null,
                    sectorIdGenerator).ToArray();

                foreach (var sector in sectors)
                {
                    yield return sector;
                }

            }
        }
        foreach (var zone in zones.Where(z => z is not ZoneSplitter.RootZone))
        {
            var nodes = GetNodesInZone(zone);

            var sectors = SplitIntoSectorsRecursive(
                nodes,
                StartDepth + 1,
                "0/",
                0,
                sectorIdGenerator).ToArray();

            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }
    }

    private static ProtoSector CreateEmptyRootSector(uint sectorId, Vector3 bbMin, Vector3 bbMax)
    {
        return new ProtoSector(
            sectorId,
            ParentSectorId: null,
            StartDepth,
            $"{sectorId}/",
            null!,
            bbMin,
            bbMax
        );

    }
    private static Node[] GetNodesInZone(ZoneSplitter.Zone zone)
    {
        return zone.Primitives
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
    }
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


        var sectors = SplitIntoSectorsRecursive(
            nodes,
            StartDepth,
            "",
            null,
            sectorIdGenerator).ToArray();

        foreach (var sector in sectors)
        {
            yield return sector;
        }
    }

    private static IEnumerable<ProtoSector> SplitIntoSectorsRecursive(
        Node[] nodes,
        int recursiveDepth,
        string parentPath,
        uint? parentSectorId,
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
        var bbMidPoint = (bbMin + bbMax) / 2;
        var bbSize = Vector3.Distance(bbMin, bbMax);

        var mainVoxelNodes = Array.Empty<Node>();
        var subVoxelNodes = Array.Empty<Node>();
        bool isLeaf = false;

        if (bbSize < DoNotSplitSectorsSmallerThanMetersInDiameter)
        {
            mainVoxelNodes = nodes;
            var estimatedByteSize = mainVoxelNodes.Sum(n => n.EstimatedByteSize);
            isLeaf = true;
        }
        else
        {
            // fill main voxel according to budget
            long estimatedByteSize = 0;
            var additionalMainVoxelNodesByBudget = GetNodesByBudget(nodes.ToArray(), SectorEstimatedByteSizeBudget - estimatedByteSize).ToList();
            mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
            subVoxelNodes = nodes.Except(additionalMainVoxelNodesByBudget).ToArray();

            isLeaf = subVoxelNodes.Length == 0;
        }

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
            bbMax
        );
    }

    private static IEnumerable<Node> GetNodesByBudget(IReadOnlyList<Node> nodes, long budget)
    {
        // TODO: Optimize, or find a better way, to include the right amount of TriangleMesh and primitives. Without weighting too many primitives will be included.
        var nodesInPrioritizedOrder = nodes
            .OrderByDescending(x =>
                {
                    var isTriangleMesh = x.Geometries.Any(y => y is TriangleMesh);
                    var weightFactor = isTriangleMesh ? 1 : 10; // Theory: Primitives have more overhead than their byte size. This is not verified.

                    return x.Diagonal / (x.EstimatedByteSize * weightFactor);
                }
            );

        // Always add atleast one node if there is still budget left, to avoid nothing ever being added if the largest node exceeds the maximum budget
        var budgetLeft = budget;
        foreach (var node in nodesInPrioritizedOrder)
        {
            if (budgetLeft < 0)
            {
                yield break;
            }

            budgetLeft -= node.EstimatedByteSize;
            yield return node;
        }
    }

    private static int CalculateVoxelKeyForGeometry(RvmBoundingBox geometryBoundingBox, Vector3 bbMidPoint)
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
        return voxelKeyAndUsageCount.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
    }

    private static Vector3 GetBoundingBoxMin(this IReadOnlyCollection<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty", nameof(nodes));

        return nodes.Select(p => p.BoundingBoxMin).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
    }

    private static Vector3 GetBoundingBoxMax(this IReadOnlyCollection<Node> nodes)
    {
        if (!nodes.Any())
            throw new ArgumentException($"Need to have at least 1 node to calculate bounds. {nameof(nodes)} was empty", nameof(nodes));

        return nodes.Select(p => p.BoundingBoxMax).Aggregate(new Vector3(float.MinValue), Vector3.Max);
    }
}