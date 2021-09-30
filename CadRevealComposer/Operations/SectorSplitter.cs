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

    public static class SectorSplitter
    {
        public static IEnumerable<SceneCreator.SectorInfo> SplitIntoSectors(
            List<APrimitive> allGeometries,
            ulong instancedMeshesFileId,
            int recursiveDepth,
            string? parentPath,
            uint? parentSectorId,
            SequentialIdGenerator meshFileIdGenerator,
            SequentialIdGenerator sectorIdGenerator,
            uint maxDepth)
        {
            const int MainVoxel = 0, SubVoxelA = 1, SubVoxelB = 2, SubVoxelC = 3, SubVoxelD = 4, SubVoxelE = 5, SubVoxelF = 6, SubVoxelG = 7, SubVoxelH = 8;

            static int CalculateVoxelKey(RvmBoundingBox boundingBox, float midX, float midY, float midZ)
            {
                if (boundingBox.Min.X < midX && boundingBox.Max.X > midX ||
                    boundingBox.Min.Y < midY && boundingBox.Max.Y > midY ||
                    boundingBox.Min.Z < midZ && boundingBox.Max.Z > midZ)
                {
                    return MainVoxel; // crosses the mid boundary in either X,Y,Z
                }

                // at this point we know the primitive does not cross mid boundary in X,Y,Z - meaning it can be placed in one of the eight sub quadrants
                return (boundingBox.Min.X < midX, boundingBox.Min.Y < midY, boundingBox.Min.Z < midZ) switch
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

            static int CalculateVoxelKeyForNodeGroup(IEnumerable<APrimitive> nodeGroupPrimitives, float midX, float midY, float midZ)
            {
                // all primitives with the same NodeId shall be placed in the same voxel
                var lastVoxelKey = int.MinValue;
                foreach (var primitive in nodeGroupPrimitives)
                {
                    var voxelKey = CalculateVoxelKey(primitive.AxisAlignedBoundingBox, midX, midY, midZ);
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

            var minX = allGeometries.Min(x => x.AxisAlignedBoundingBox.Min.X);
            var minY = allGeometries.Min(x => x.AxisAlignedBoundingBox.Min.Y);
            var minZ = allGeometries.Min(x => x.AxisAlignedBoundingBox.Min.Z);
            var maxX = allGeometries.Max(x => x.AxisAlignedBoundingBox.Max.X);
            var maxY = allGeometries.Max(x => x.AxisAlignedBoundingBox.Max.Y);
            var maxZ = allGeometries.Max(x => x.AxisAlignedBoundingBox.Max.Z);

            var midX = minX + ((maxX - minX) / 2);
            var midY = minY + ((maxY - minY) / 2);
            var midZ = minZ + ((maxZ - minZ) / 2);

            var grouped = allGeometries
                .GroupBy(x => x.NodeId)
                .GroupBy(x => CalculateVoxelKeyForNodeGroup(x, midX, midY, midZ))
                .OrderBy(x => x.Key)
                .ToImmutableList();

            var isRoot = recursiveDepth == 0;
            var isLeaf = recursiveDepth >= maxDepth || allGeometries.Count < 10000 || grouped.Count == 1;

            if (isLeaf)
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var hasInstanceMeshes = allGeometries.OfType<InstancedMesh>().Any();
                var hasTriangleMeshes = allGeometries.OfType<TriangleMesh>().Any();
                var meshId = hasTriangleMeshes
                    ? meshFileIdGenerator.GetNextId()
                    : (ulong?)null;
                var path = isRoot
                    ? $"{sectorId}"
                    : $"{parentPath}/{sectorId}";
                yield return new SceneCreator.SectorInfo(
                    sectorId,
                    parentSectorId,
                    recursiveDepth,
                    path,
                    $"sector_{sectorId}.i3d",
                    (hasInstanceMeshes, hasTriangleMeshes) switch
                    {
                        (true, true) => new[] { $"mesh_{instancedMeshesFileId}.ctm", $"mesh_{meshId}.ctm" },
                        (true, false) => new[] { $"mesh_{instancedMeshesFileId}.ctm" },
                        (false, true) => new[] { $"mesh_{meshId}.ctm" },
                        (false, false) => Array.Empty<string>()
                    },
                    1234, // TODO: calculate
                    1, // TODO: calculate
                    meshId,
                    allGeometries,
                    new RvmBoundingBox(
                        new Vector3(minX, minY, minZ),
                        new Vector3(maxX, maxY, maxZ)
                    ));
            }
            else
            {
                if (isRoot && grouped.First().Key != MainVoxel)
                {
                    throw new InvalidOperationException("if MainVoxel is not amongst groups in the first function call the root will not be created");
                }

                var parentPathForChildren = parentPath;
                var parentSectorIdForChildren = parentSectorId;

                foreach (var group in grouped)
                {
                    if (group.Key == MainVoxel)
                    {
                        var sectorId = (uint)sectorIdGenerator.GetNextId();
                        var geometries = group.SelectMany(x => x).ToList();
                        var hasInstanceMeshes = geometries.OfType<InstancedMesh>().Any();
                        var hasTriangleMeshes = geometries.OfType<TriangleMesh>().Any();
                        var meshId = hasTriangleMeshes
                            ? meshFileIdGenerator.GetNextId()
                            : (ulong?)null;
                        var path = isRoot
                            ? $"{sectorId}"
                            : $"{parentPath}/{sectorId}";

                        parentPathForChildren = path;
                        parentSectorIdForChildren = sectorId;

                        yield return new SceneCreator.SectorInfo(
                            sectorId,
                            parentSectorId,
                            recursiveDepth,
                            path,
                            $"sector_{sectorId}.i3d",
                            (hasInstanceMeshes, hasTriangleMeshes) switch
                            {
                                (true, true) => new[] { $"mesh_{instancedMeshesFileId}.ctm", $"mesh_{meshId}.ctm" },
                                (true, false) => new[] { $"mesh_{instancedMeshesFileId}.ctm"},
                                (false, true) => new[] { $"mesh_{meshId}.ctm" },
                                (false, false) => Array.Empty<string>()
                            },
                            1234, // TODO: calculate
                            1, // TODO: calculate
                            meshId,
                            geometries,
                            new RvmBoundingBox(
                                new Vector3(minX, minY, minZ),
                                new Vector3(maxX, maxY, maxZ)
                            ));
                    }
                    else
                    {
                        var sectors = SplitIntoSectors(
                            @group.SelectMany(x => x).ToList(),
                            instancedMeshesFileId,
                            recursiveDepth + 1,
                            parentPathForChildren,
                            parentSectorIdForChildren,
                            meshFileIdGenerator,
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
    }
}