namespace CadRevealComposer.Operations
{
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
        private const int StartDepth = 1;
        private const long SectorEstimatedByteSizeBudget = 1_000_000; // bytes

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

        public static IEnumerable<ProtoSector> SplitIntoSectors(ZoneSplitter.Zone[] zones)
        {
            var sectorIdGenerator = new SequentialIdGenerator();

            var rootZone = zones.Single(z => z is ZoneSplitter.RootZone);

            var rootSectorId = (uint)sectorIdGenerator.GetNextId();
            //yield return CreateRootSector(rootSectorId, rootZone.Primitives);

            foreach (var zone in zones.Where(z => z is not ZoneSplitter.RootZone))
            {
                var nodes = zone.Primitives
                    .GroupBy(p => p.NodeId)
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
                    StartDepth + 1,
                    $"{rootSectorId}",
                    rootSectorId,
                    sectorIdGenerator);

                foreach (var sector in sectors)
                {
                    yield return sector;
                }
            }
        }

        public static IEnumerable<ProtoSector> CreateSingleSector(APrimitive[] allGeometries)
        {
            //yield return CreateRootSector(0, allGeometries);
            yield return null;
        }

        public static IEnumerable<ProtoSector> SplitIntoSectors(APrimitive[] allGeometries)
        {
            var sectorIdGenerator = new SequentialIdGenerator();

            var rootSectorId = (uint)sectorIdGenerator.GetNextId();

            var nodes = allGeometries
                .GroupBy(p => p.NodeId)
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

            var rootNodes = GetRootSectorNodes(nodes);
            var rootGeometries = rootNodes.SelectMany(n => n.Geometries).ToArray();
            //var rootSector = CreateRootSector(rootSectorId, rootGeometries);

            var restNodes = nodes.ToArray();
            var sectors = SplitIntoSectorsRecursive(
                restNodes,
                StartDepth + 1,
                $"{rootSectorId}",
                rootSectorId,
                sectorIdGenerator).ToArray();

            var minBB = sectors.Select(s => s.BoundingBoxMin).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
            var maxBB = sectors.Select(s => s.BoundingBoxMax).Aggregate(new Vector3(float.MinValue), Vector3.Max);

            var rootSector = CreateRootSector(rootSectorId, minBB, maxBB);

            yield return rootSector;
            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }

        private static int[] sectorBudget = new int[] { 0, 1_000_000, 2_000_000, 2_000_000, 10_000_000 }; // Arbitrary numbers

        private static IEnumerable<ProtoSector> SplitIntoSectorsRecursive(
            Node[] nodes,
            int recursiveDepth,
            string parentPath,
            uint parentSectorId,
            SequentialIdGenerator sectorIdGenerator)
        {
            /* Recursively divides space into eight voxels of equal size (each dimension X,Y,Z is divided in half).
             * A geometry is placed in a voxel only if it fully encloses the geometry.
             * Important: Geometries are grouped by NodeId and the group as a whole is placed into the same voxel (that encloses all the geometries in the group).
             */

            if (nodes.Length == 0)
            {
                yield break;
            }

            var bbMin = nodes.GetBoundingBoxMin();
            var bbMax = nodes.GetBoundingBoxMax();
            var bbMidPoint = bbMin + ((bbMax - bbMin) / 2);


            var mainVoxelNodes = new Node[] { };
            if (recursiveDepth >= 3)
            {
                mainVoxelNodes = nodes
                    .Where(node => CalculateVoxelKeyForNode(node.Geometries, bbMidPoint) == MainVoxel)
                    .ToArray();
            }
            var subVoxelNodes = nodes
                .Except(mainVoxelNodes)
                .ToArray();

            // fill main voxel according to budget
            int budget;
            if (recursiveDepth >= sectorBudget.Length)
            {
                budget = sectorBudget[sectorBudget.Length - 1];
            }
            else
            {
                budget = sectorBudget[recursiveDepth];
            }

            var estimatedByteSize = mainVoxelNodes.Sum(n => n.EstimatedByteSize);
            //var additionalMainVoxelNodesByBudget = GetNodesByBudget(subVoxelNodes, SectorEstimatedByteSizeBudget - estimatedByteSize).ToArray();
            var additionalMainVoxelNodesByBudget = GetNodesByBudget(subVoxelNodes, budget - estimatedByteSize).ToArray();
            mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
            subVoxelNodes = subVoxelNodes.Except(additionalMainVoxelNodesByBudget).ToArray();

            //var isLeaf = subVoxelNodes.Length == 0 || nodes.Sum(n => n.EstimatedByteSize) <= SectorEstimatedByteSizeBudget;
            var isLeaf = subVoxelNodes.Length == 0 || nodes.Sum(n => n.EstimatedByteSize) <= budget;
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
                        recursiveDepth == StartDepth + 1
                            ? bbMin - new Vector3(100.0f)
                            : bbMin,
                        recursiveDepth == StartDepth + 1
                            ? bbMax + new Vector3(100.0f)
                            : bbMax
                    );
                }

                var voxels = subVoxelNodes
                    .GroupBy(node => CalculateVoxelKeyForNode(node.Geometries, bbMidPoint))
                    .OrderBy(x => x.Key)
                    .ToImmutableList();

                foreach (var voxelGroup in voxels)
                {
                    //if (voxelGroup.Key == MainVoxel)
                    //{
                    //    throw new Exception("Main voxel should not appear here. Main voxel should be processed separately.");
                    //}

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

        private static ProtoSector CreateRootSector(uint sectorId, Vector3 bbMin, Vector3 bbMax)
        {
            //var bbMin = geometries.GetBoundingBoxMin(); // TODO Change to Rvm axis box encapsulate
            //var bbMax = geometries.GetBoundingBoxMax();

            return new ProtoSector(
                sectorId,
                ParentSectorId: null,
                StartDepth,
                $"{sectorId}",
                Array.Empty<APrimitive>(),
                bbMin,
                bbMax
                );
        }

        private static Node[] GetRootSectorNodes(Node[] nodes)
        {
            // get bounding box for platform using approximation (99th percentile)
            var percentile = 0.00;
            var platformMinX = nodes.Select(node => node.BoundingBoxMin.X).OrderBy(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMinY = nodes.Select(node => node.BoundingBoxMin.Y).OrderBy(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMinZ = nodes.Select(node => node.BoundingBoxMin.Z).OrderBy(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMaxX = nodes.Select(node => node.BoundingBoxMax.X).OrderByDescending(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMaxY = nodes.Select(node => node.BoundingBoxMax.Y).OrderByDescending(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMaxZ = nodes.Select(node => node.BoundingBoxMax.Z).OrderByDescending(x => x).Skip((int)(percentile * nodes.Length)).First();

            // pad the 99th percentile bounding box, the idea is that objects near the edge of the platform should stay inside the bounding box
            const int platformPadding = 5; // meters
            var bbMin = new Vector3(platformMinX - platformPadding, platformMinY - platformPadding, platformMinZ - platformPadding);
            var bbMax = new Vector3(platformMaxX + platformPadding, platformMaxY + platformPadding, platformMaxZ + platformPadding);

            bool IsNodeOutsidePlatform(Node node)
            {
                // all geometries outside the main platform belongs to the root sector
                return node.BoundingBoxMin.X < bbMin.X ||
                       node.BoundingBoxMin.Y < bbMin.Y ||
                       node.BoundingBoxMin.Z < bbMin.Z ||
                       node.BoundingBoxMax.X > bbMax.X ||
                       node.BoundingBoxMax.Y > bbMax.Y ||
                       node.BoundingBoxMax.Z > bbMax.Z;
            }

            return nodes
                .Where(IsNodeOutsidePlatform)
                .ToArray();
        }

        private static IEnumerable<Node> GetNodesByBudget(Node[] nodes, long budget)
        {
            var nodesInPrioritizedOrder = nodes
                .OrderByDescending(x => x.Diagonal);

            var budgetLeft = budget;
            foreach (var node in nodesInPrioritizedOrder)
            {
                if (budgetLeft - node.EstimatedByteSize < 0)
                {
                    yield break;
                }

                budgetLeft -= node.EstimatedByteSize;
                yield return node;
            }
        }

        private static int CalculateVoxelKeyForGeometry(RvmBoundingBox geometryBoundingBox, Vector3 bbMidPoint)
        {
            if (geometryBoundingBox.Min.X < bbMidPoint.X && geometryBoundingBox.Max.X > bbMidPoint.X ||
                geometryBoundingBox.Min.Y < bbMidPoint.Y && geometryBoundingBox.Max.Y > bbMidPoint.Y ||
                geometryBoundingBox.Min.Z < bbMidPoint.Z && geometryBoundingBox.Max.Z > bbMidPoint.Z)
            {
                return MainVoxel; // crosses the mid boundary in either X,Y,Z
            }

            // at this point we know the geometry does not cross mid boundary in X,Y,Z - meaning it can be placed in one of the eight sub voxels
            return (geometryBoundingBox.Min.X < bbMidPoint.X, geometryBoundingBox.Min.Y < bbMidPoint.Y, geometryBoundingBox.Min.Z < bbMidPoint.Z) switch
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

        private static int CalculateVoxelKeyForNode(APrimitive[] nodeGroupGeometries, Vector3 bbMidPoint)
        {
            // all geometries with the same NodeId shall be placed in the same voxel
            var lastVoxelKey = int.MinValue;
            foreach (var geometry in nodeGroupGeometries)
            {
                var voxelKey = CalculateVoxelKeyForGeometry(geometry.AxisAlignedBoundingBox, bbMidPoint);
                if (voxelKey == MainVoxel)
                {
                    return MainVoxel;
                }
                var differentVoxelKeyDetected = lastVoxelKey != int.MinValue && lastVoxelKey != voxelKey;
                if (differentVoxelKeyDetected)
                {
                    return MainVoxel;
                }

                lastVoxelKey = voxelKey;
            }

            // at this point all geometries hashes to the same sub voxel
            return lastVoxelKey;
        }

        private static Vector3 GetBoundingBoxMin(this IEnumerable<Node> nodes)
        {
            return nodes.Select(p => p.BoundingBoxMin).Aggregate(new Vector3(float.MaxValue), Vector3.Min);
        }

        private static Vector3 GetBoundingBoxMax(this IEnumerable<Node> nodes)
        {
            return nodes.Select(p => p.BoundingBoxMax).Aggregate(new Vector3(float.MinValue), Vector3.Max);
        }
    }
}