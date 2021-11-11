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

    // TODO: facet group matching prioritize by cost instead of group of 300
    // TODO: use RVM file bounding box as sector
    // TODO: prioritize by discipline
    // TODO: pad sector bounding boxes to control sector loading

    public static class SectorSplitter
    {
        private const int MainVoxel = 0, SubVoxelA = 1, SubVoxelB = 2, SubVoxelC = 3, SubVoxelD = 4, SubVoxelE = 5, SubVoxelF = 6, SubVoxelG = 7, SubVoxelH = 8;
        private const int StartDepth = 1;
        private const int SectorDrawCallBudget = 1000;

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
            int EstimatedDrawCalls,
            Vector3 BoundingBoxMin,
            Vector3 BoundingBoxMax);

        public static IEnumerable<ProtoSector> SplitIntoSectors(
            APrimitive[] allGeometries,
            SequentialIdGenerator sectorIdGenerator,
            bool useSingleSector)
        {
            var rootSectorId = (uint)sectorIdGenerator.GetNextId();

            if (useSingleSector)
            {
                yield return CreateRootSector(rootSectorId, allGeometries);
                yield break;
            }

            var nodes = allGeometries
                .GroupBy(p => p.NodeId)
                .Select(g =>
                {
                    var geometries = g.ToArray();
                    return new Node(
                        g.Key,
                        geometries,
                        DrawCallEstimator.Estimate(geometries).EstimatedDrawCalls,
                        geometries.GetBoundingBoxMin(),
                        geometries.GetBoundingBoxMax());
                })
                .ToArray();

            var rootNodes = GetRootSectorNodes(nodes);
            var rootGeometries = rootNodes.SelectMany(n => n.Geometries).ToArray();
            var rootSector = CreateRootSector(rootSectorId, rootGeometries);

            var restNodes = nodes.Except(rootNodes).ToArray();
            var sectors = SplitIntoSectors(
                restNodes,
                StartDepth + 1,
                $"{rootSectorId}",
                rootSectorId,
                sectorIdGenerator);

            yield return rootSector;
            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }

        private static IEnumerable<ProtoSector> SplitIntoSectors(
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

            var mainVoxelNodes = nodes
                .Where(node => CalculateVoxelKeyForNode(node.Geometries, bbMidPoint) == MainVoxel)
                .ToArray();
            var subVoxelNodes = nodes
                .Except(mainVoxelNodes)
                .ToArray();

            var drawCalls = mainVoxelNodes.Sum(node => node.EstimatedDrawCalls);
            var additionalMainVoxelNodesByBudget = GetNodesByBudget(subVoxelNodes, SectorDrawCallBudget - drawCalls).ToArray();
            mainVoxelNodes = mainVoxelNodes.Concat(additionalMainVoxelNodesByBudget).ToArray();
            subVoxelNodes = subVoxelNodes.Except(additionalMainVoxelNodesByBudget).ToArray();

            var isLeaf = subVoxelNodes.Length == 0 || nodes.Sum(n => n.EstimatedDrawCalls) <= SectorDrawCallBudget;
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
                    if (voxelGroup.Key == MainVoxel)
                    {
                        throw new Exception("Main voxel should not appear here. Main voxel should be processed separately.");
                    }

                    var sectors = SplitIntoSectors(
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

        private static Node[] GetRootSectorNodes(Node[] nodes)
        {
            // get bounding box for platform using approximation (99th percentile)
            var percentile = 0.01;
            var platformMinX = nodes.Select(node => node.BoundingBoxMin.X).OrderBy(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMinY = nodes.Select(node => node.BoundingBoxMin.Y).OrderBy(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMinZ = nodes.Select(node => node.BoundingBoxMin.Z).OrderBy(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMaxX = nodes.Select(node => node.BoundingBoxMax.X).OrderByDescending(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMaxY = nodes.Select(node => node.BoundingBoxMax.Y).OrderByDescending(x => x).Skip((int)(percentile * nodes.Length)).First();
            var platformMaxZ = nodes.Select(node => node.BoundingBoxMax.Z).OrderByDescending(x => x).Skip((int)(percentile * nodes.Length)).First();

            // pad the 95 percentile bounding box, the idea is that objects near the edge of the platform should stay inside the bounding box
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

        private static IEnumerable<Node> GetNodesByBudget(Node[] nodes, int budget)
        {
            var nodesInPrioritizedOrder = nodes
                .Select(node => new
                {
                    Node = node,
                    node.EstimatedDrawCalls,
                    Diagonal = Vector3.Distance(node.BoundingBoxMin, node.BoundingBoxMax)
                })
                .OrderByDescending(x => x.Diagonal);

            var budgetLeft = budget;
            foreach (var item in nodesInPrioritizedOrder)
            {
                if (budgetLeft < 1)
                {
                    yield break;
                }

                if (budgetLeft - item.EstimatedDrawCalls >= 0)
                {
                    budgetLeft -= item.EstimatedDrawCalls;
                    yield return item.Node;
                }
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