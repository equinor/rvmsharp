namespace CadRevealComposer.Operations
{
    using IdProviders;
    using Primitives;
    using RvmSharp.Primitives;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;
    using Utils;

    public static class SectorSplitter
    {
        const int MainVoxel = 0, SubVoxelA = 1, SubVoxelB = 2, SubVoxelC = 3, SubVoxelD = 4, SubVoxelE = 5, SubVoxelF = 6, SubVoxelG = 7, SubVoxelH = 8;

        public const int StartDepth = 1;

        /// <summary>
        /// Optimization that prioritizes a node to be in a sector if either XY/XZ/YZ plane is bigger than X percent of the corresponding plane of the sector bounding box. Regardless if the node could be placed in a sub voxel.
        /// REMARK: Same logic is used to prioritize which objects go into the root sector.
        /// </summary>
        public const float Node2DPlanePrioritizationThreshold = 0.01f; // percent of plane

        public record ProtoSector(
            uint SectorId,
            uint? ParentSectorId,
            int Depth,
            string Path,
            APrimitive[] Geometries,
            Vector3 BoundingBoxMin,
            Vector3 BoundingBoxMax
        );

        public static IEnumerable<ProtoSector> SplitIntoSectors(
            APrimitive[] allGeometries,
            SequentialIdGenerator sectorIdGenerator,
            uint maxDepth)
        {
            var rootSectorId = (uint)sectorIdGenerator.GetNextId();
            var rootSector = GetRootSector(rootSectorId, allGeometries);

            var restGeometries = allGeometries.Except(rootSector.Geometries).ToArray();
            var sectors = SplitIntoSectors(
                restGeometries,
                StartDepth + 1,
                $"{rootSectorId}",
                rootSectorId,
                sectorIdGenerator,
                maxDepth);

            yield return rootSector;
            foreach (var sector in sectors)
            {
                yield return sector;
            }
        }

        private static IEnumerable<ProtoSector> SplitIntoSectors(
            APrimitive[] allGeometries,
            int recursiveDepth,
            string parentPath,
            uint parentSectorId,
            SequentialIdGenerator sectorIdGenerator,
            uint maxDepth)
        {
            /* Recursively divides space into eight voxels of equal size (each dimension X,Y,Z is divided in half).
             * A geometry is placed in a voxel only if it fully encloses the geometry.
             * Important: Geometries are grouped by NodeId and the group as a whole is placed into the same voxel (that encloses all the geometries in the group).
             */

            if (allGeometries.Length == 0)
            {
                yield break;
            }

            var bbMin = allGeometries.GetBoundingBoxMin();
            var bbMax = allGeometries.GetBoundingBoxMax();
            var bbMidPoint = bbMin + ((bbMax - bbMin) / 2);
            var bbVolume = bbMax - bbMin;

            var grouped = allGeometries
                .GroupBy(x => x.NodeId)
                .GroupBy(x => CalculateVoxelKeyForNodeGroup(x.ToArray(), bbMidPoint, bbVolume))
                .OrderBy(x => x.Key)
                .ToImmutableList();

            var isLeaf = recursiveDepth >= maxDepth || allGeometries.Length < 10000 || grouped.Count == 1;
            if (isLeaf)
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var path = $"{parentPath}/{sectorId}";
                yield return new ProtoSector(
                    sectorId,
                    parentSectorId,
                    recursiveDepth,
                    path,
                    allGeometries,
                    bbMin,
                    bbMax
                    );
            }
            else
            {
                var parentPathForChildren = parentPath;
                var parentSectorIdForChildren = parentSectorId;

                foreach (var group in grouped)
                {
                    if (group.Key == MainVoxel)
                    {
                        var sectorId = (uint)sectorIdGenerator.GetNextId();
                        var geometries = group.SelectMany(x => x).ToArray();
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
                    else
                    {
                        var sectors = SplitIntoSectors(
                            group.SelectMany(x => x).ToArray(),
                            recursiveDepth + 1,
                            parentPathForChildren,
                            parentSectorIdForChildren,
                            sectorIdGenerator,
                            maxDepth);
                        foreach (var sector in sectors)
                        {
                            yield return sector;
                        }
                    }
                }
            }
        }

        private static ProtoSector GetRootSector(uint sectorId, APrimitive[] allGeometries)
        {
            var rootPrimitives = GetRootPrimitives(allGeometries);
            var bbMin = allGeometries.GetBoundingBoxMin();
            var bbMax = allGeometries.GetBoundingBoxMax();
            return new ProtoSector(
                sectorId,
                ParentSectorId: null,
                StartDepth,
                $"{sectorId}",
                rootPrimitives,
                bbMin,
                bbMax
                );
        }

        private static APrimitive[] GetRootPrimitives(APrimitive[] allGeometries)
        {
            // get bounding box for platform using approximation (95 percentile)
            var platformMinX = allGeometries.Select(x => x.AxisAlignedBoundingBox.Min.X).OrderBy(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMinY = allGeometries.Select(x => x.AxisAlignedBoundingBox.Min.Y).OrderBy(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMinZ = allGeometries.Select(x => x.AxisAlignedBoundingBox.Min.Z).OrderBy(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMaxX = allGeometries.Select(x => x.AxisAlignedBoundingBox.Max.X).OrderByDescending(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMaxY = allGeometries.Select(x => x.AxisAlignedBoundingBox.Max.Y).OrderByDescending(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMaxZ = allGeometries.Select(x => x.AxisAlignedBoundingBox.Max.Z).OrderByDescending(x => x).Skip((int)(0.05 * allGeometries.Length)).First();

            // pad the 95 percentile bounding box, the idea is that objects near the edge of the platform should stay inside the bounding box
            const int platformPadding = 5; // meters
            var bbMin = new Vector3(platformMinX - platformPadding, platformMinY - platformPadding, platformMinZ - platformPadding);
            var bbMax = new Vector3(platformMaxX + platformPadding, platformMaxY + platformPadding, platformMaxZ + platformPadding);
            var bbVolume = bbMax - bbMin;

            bool FilterNodeGroupedPrimitives(IGrouping<ulong, APrimitive> grouping)
            {
                var nodeBoundingBoxMin = grouping.GetBoundingBoxMin();
                var nodeBoundingBoxMax = grouping.GetBoundingBoxMax();

                // all geometries outside the main platform belongs to the root sector
                if (nodeBoundingBoxMin.X < bbMin.X ||
                    nodeBoundingBoxMin.Y < bbMin.Y ||
                    nodeBoundingBoxMin.Z < bbMin.Z ||
                    nodeBoundingBoxMax.X > bbMax.X ||
                    nodeBoundingBoxMax.Y > bbMax.Y ||
                    nodeBoundingBoxMax.Z > bbMax.Z)
                {
                    return true;
                }

                // optimization heuristic: if the object's 2D surfaces are big then place it in the root (2D surface based on axis aligned bounding box)
                const float thresholdFactor = Node2DPlanePrioritizationThreshold * Node2DPlanePrioritizationThreshold; // percent of 2D plane
                var nodeVolume = nodeBoundingBoxMax - nodeBoundingBoxMin;
                if (nodeVolume.X * nodeVolume.Y > thresholdFactor * bbVolume.X * bbVolume.Y) // XY plane
                {
                    return true;
                }
                if (nodeVolume.X * nodeVolume.Z > thresholdFactor * bbVolume.X * bbVolume.Z) // XZ plane
                {
                    return true;
                }
                if (nodeVolume.Y * nodeVolume.Z > thresholdFactor * bbVolume.Y * bbVolume.Z) // YZ plane
                {
                    return true;
                }

                return false;
            }

            return allGeometries
                .GroupBy(p => p.NodeId)
                .Where(FilterNodeGroupedPrimitives)
                .SelectMany(g => g)
                .ToArray();
        }

        private static int CalculateVoxelKeyForPrimitive(RvmBoundingBox primitiveBoundingBox, Vector3 bbMidPoint)
        {
            if (primitiveBoundingBox.Min.X < bbMidPoint.X && primitiveBoundingBox.Max.X > bbMidPoint.X ||
                primitiveBoundingBox.Min.Y < bbMidPoint.Y && primitiveBoundingBox.Max.Y > bbMidPoint.Y ||
                primitiveBoundingBox.Min.Z < bbMidPoint.Z && primitiveBoundingBox.Max.Z > bbMidPoint.Z)
            {
                return MainVoxel; // crosses the mid boundary in either X,Y,Z
            }

            // at this point we know the primitive does not cross mid boundary in X,Y,Z - meaning it can be placed in one of the eight sub voxels
            return (primitiveBoundingBox.Min.X < bbMidPoint.X, primitiveBoundingBox.Min.Y < bbMidPoint.Y, primitiveBoundingBox.Min.Z < bbMidPoint.Z) switch
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

        private static int CalculateVoxelKeyForNodeGroup(APrimitive[] nodeGroupPrimitives, Vector3 bbMidPoint, Vector3 bbVolume)
        {
            // optimization heuristic: if the object's 2D surfaces are big then place it in main voxel (2D surface based on axis aligned bounding box)
            var nodeBoundingBoxMin = nodeGroupPrimitives.GetBoundingBoxMin();
            var nodeBoundingBoxMax = nodeGroupPrimitives.GetBoundingBoxMax();
            var nodeBoundingBoxVolume = nodeBoundingBoxMax - nodeBoundingBoxMin;
            const float thresholdFactor = Node2DPlanePrioritizationThreshold * Node2DPlanePrioritizationThreshold; // percent of 2D plane
            if (nodeBoundingBoxVolume.X * nodeBoundingBoxVolume.Y > thresholdFactor * bbVolume.X * bbVolume.Y) // XY plane
            {
                return MainVoxel;
            }
            if (nodeBoundingBoxVolume.X * nodeBoundingBoxVolume.Z > thresholdFactor * bbVolume.X * bbVolume.Z) // XZ plane
            {
                return MainVoxel;
            }
            if (nodeBoundingBoxVolume.Y * nodeBoundingBoxVolume.Z > thresholdFactor * bbVolume.Y * bbVolume.Z) // YZ plane
            {
                return MainVoxel;
            }

            // all primitives with the same NodeId shall be placed in the same voxel
            var lastVoxelKey = int.MinValue;
            foreach (var primitive in nodeGroupPrimitives)
            {
                var voxelKey = CalculateVoxelKeyForPrimitive(primitive.AxisAlignedBoundingBox, bbMidPoint);
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

            // at this point all primitives hashes to the same sub voxel
            return lastVoxelKey;
        }
    }
}