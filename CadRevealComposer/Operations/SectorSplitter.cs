namespace CadRevealComposer.Operations
{
    using IdProviders;
    using Primitives;
    using RvmSharp.Primitives;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Numerics;

    public static class SectorSplitter
    {
        const int MainVoxel = 0, SubVoxelA = 1, SubVoxelB = 2, SubVoxelC = 3, SubVoxelD = 4, SubVoxelE = 5, SubVoxelF = 6, SubVoxelG = 7, SubVoxelH = 8;

        public const int StartDepth = 1;

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
            var sectors = SplitIntoSectors(restGeometries, StartDepth + 1, $"{rootSectorId}", rootSectorId, sectorIdGenerator, maxDepth);

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

            var minX = allGeometries.Min(x => x.AxisAlignedBoundingBox.Min.X);
            var minY = allGeometries.Min(x => x.AxisAlignedBoundingBox.Min.Y);
            var minZ = allGeometries.Min(x => x.AxisAlignedBoundingBox.Min.Z);
            var maxX = allGeometries.Max(x => x.AxisAlignedBoundingBox.Max.X);
            var maxY = allGeometries.Max(x => x.AxisAlignedBoundingBox.Max.Y);
            var maxZ = allGeometries.Max(x => x.AxisAlignedBoundingBox.Max.Z);

            var midX = minX + ((maxX - minX) / 2);
            var midY = minY + ((maxY - minY) / 2);
            var midZ = minZ + ((maxZ - minZ) / 2);

            var midPoint = new Vector3(midX, midY, midZ);
            var volume = new Vector3(maxX, maxY, maxZ) - new Vector3(minX, minY, minZ);

            var grouped = allGeometries
                .GroupBy(x => x.NodeId)
                .GroupBy(x => CalculateVoxelKeyForNodeGroup(x, midPoint, volume))
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
                    new Vector3(minX, minY, minZ),
                    new Vector3(maxX, maxY, maxZ)
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
                            new Vector3(minX, minY, minZ),
                            new Vector3(maxX, maxY, maxZ)
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
            // get bounding box for platform using 95 percentile (approximation)
            var platformMinX = allGeometries.Select(x => x.AxisAlignedBoundingBox.Min.X).OrderBy(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMinY = allGeometries.Select(x => x.AxisAlignedBoundingBox.Min.Y).OrderBy(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMinZ = allGeometries.Select(x => x.AxisAlignedBoundingBox.Min.Z).OrderBy(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMaxX = allGeometries.Select(x => x.AxisAlignedBoundingBox.Max.X).OrderByDescending(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMaxY = allGeometries.Select(x => x.AxisAlignedBoundingBox.Max.Y).OrderByDescending(x => x).Skip((int)(0.05 * allGeometries.Length)).First();
            var platformMaxZ = allGeometries.Select(x => x.AxisAlignedBoundingBox.Max.Z).OrderByDescending(x => x).Skip((int)(0.05 * allGeometries.Length)).First();

            const int platformPadding = 5; // meters
            var rootPrimitives = GetRootPrimitives(
                allGeometries,
                new Vector3(platformMinX - platformPadding, platformMinY - platformPadding, platformMinZ - platformPadding),
                new Vector3(platformMaxX + platformPadding, platformMaxY + platformPadding, platformMaxZ + platformPadding));

            // get bounding box (bigger than that for the platform)
            var minX = rootPrimitives.Min(x => x.AxisAlignedBoundingBox.Min.X);
            var minY = rootPrimitives.Min(x => x.AxisAlignedBoundingBox.Min.Y);
            var minZ = rootPrimitives.Min(x => x.AxisAlignedBoundingBox.Min.Z);
            var maxX = rootPrimitives.Max(x => x.AxisAlignedBoundingBox.Max.X);
            var maxY = rootPrimitives.Max(x => x.AxisAlignedBoundingBox.Max.Y);
            var maxZ = rootPrimitives.Max(x => x.AxisAlignedBoundingBox.Max.Z);

            return new ProtoSector(
                sectorId,
                null,
                StartDepth,
                $"{sectorId}",
                rootPrimitives,
                new Vector3(minX, minY, minZ),
                new Vector3(maxX, maxY, maxZ)
                );
        }

        private static APrimitive[] GetRootPrimitives(APrimitive[] allPrimitives, Vector3 platformMin, Vector3 platformMax)
        {
            var platformVolume = platformMax - platformMin;

            bool FilterPrimitive(APrimitive primitive)
            {
                var primitiveBoundingBox = primitive.AxisAlignedBoundingBox;

                // all geometries outside the main platform belongs to the root sector
                if (primitiveBoundingBox.Min.X < platformMin.X ||
                    primitiveBoundingBox.Min.Y < platformMin.Y ||
                    primitiveBoundingBox.Min.Z < platformMin.Z ||
                    primitiveBoundingBox.Max.X > platformMax.X ||
                    primitiveBoundingBox.Max.Y > platformMax.Y ||
                    primitiveBoundingBox.Max.Z > platformMax.Z)
                {
                    return true;
                }

                // optimization heuristic: if the object's 2D surfaces are big then place it in the root (2D surface based on axis aligned bounding box)
                const float thresholdFactor = 0.01f * 0.01f; // 1% of 2D plane
                var primitiveVolume = primitiveBoundingBox.Extents;
                if (primitiveVolume.X * primitiveVolume.Y > thresholdFactor * platformVolume.X * platformVolume.Y) // XY
                {
                    return true;
                }
                if (primitiveVolume.X * primitiveVolume.Z > thresholdFactor * platformVolume.X * platformVolume.Z) // XZ
                {
                    return true;
                }
                if (primitiveVolume.Y * primitiveVolume.Z > thresholdFactor * platformVolume.Y * platformVolume.Z) // YZ
                {
                    return true;
                }

                return false;
            }

            return allPrimitives.Where(FilterPrimitive).ToArray();
        }

        private static int CalculateVoxelKey(RvmBoundingBox primitiveBoundingBox, Vector3 mainVoxelMidPoint, Vector3 mainVoxelVolume)
        {
            // TODO: prioritize discipline: safety

            // optimization heuristic: if the object's 2D surfaces are big then place it in main voxel (2D surface based on axis aligned bounding box)
            const float thresholdFactor = 0.01f * 0.01f; // 1% of 2D plane
            var primitiveVolume = primitiveBoundingBox.Extents;
            if (primitiveVolume.X * primitiveVolume.Y > thresholdFactor * mainVoxelVolume.X * mainVoxelVolume.Y) // XY
            {
                return MainVoxel;
            }
            if (primitiveVolume.X * primitiveVolume.Z > thresholdFactor * mainVoxelVolume.X * mainVoxelVolume.Z) // XZ
            {
                return MainVoxel;
            }
            if (primitiveVolume.Y * primitiveVolume.Z > thresholdFactor * mainVoxelVolume.Y * mainVoxelVolume.Z) // YZ
            {
                return MainVoxel;
            }

            if (primitiveBoundingBox.Min.X < mainVoxelMidPoint.X && primitiveBoundingBox.Max.X > mainVoxelMidPoint.X ||
                primitiveBoundingBox.Min.Y < mainVoxelMidPoint.Y && primitiveBoundingBox.Max.Y > mainVoxelMidPoint.Y ||
                primitiveBoundingBox.Min.Z < mainVoxelMidPoint.Z && primitiveBoundingBox.Max.Z > mainVoxelMidPoint.Z)
            {
                return MainVoxel; // crosses the mid boundary in either X,Y,Z
            }

            // at this point we know the primitive does not cross mid boundary in X,Y,Z - meaning it can be placed in one of the eight sub quadrants
            return (primitiveBoundingBox.Min.X < mainVoxelMidPoint.X, primitiveBoundingBox.Min.Y < mainVoxelMidPoint.Y, primitiveBoundingBox.Min.Z < mainVoxelMidPoint.Z) switch
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

        private static int CalculateVoxelKeyForNodeGroup(IEnumerable<APrimitive> nodeGroupPrimitives, Vector3 mainVoxelMidPoint, Vector3 mainVoxelVolume)
        {
            // all primitives with the same NodeId shall be placed in the same voxel
            var lastVoxelKey = int.MinValue;
            foreach (var primitive in nodeGroupPrimitives)
            {
                var voxelKey = CalculateVoxelKey(primitive.AxisAlignedBoundingBox, mainVoxelMidPoint, mainVoxelVolume);
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