namespace CadRevealComposer
{
    using HierarchyComposer.Functions;
    using IdProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using Primitives;
    using Primitives.Reflection;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;
    using Utils;
    using Utils.Comparers;
    using Writers;

    public static class SceneCreator
    {
        private const int I3DFMagicBytes = 1178874697; // I3DF chars as bytes.

        public record SectorInfo(
            uint SectorId,
            uint? ParentSectorId,
            long Depth,
            string Path,
            string Filename,
            string[] PeripheralFiles,
            long EstimatedTriangleCount,
            long EstimatedDrawCallCount,
            ulong? MeshFileId,
            IReadOnlyList<APrimitive> Geometries,
            RvmBoundingBox BoundingBox
        )
        {
            public long DownloadSize { get; init; }
        };

        public static void ExportHierarchyDatabase(string databasePath, CadRevealNode[] allNodes)
        {
            var nodes = HierarchyComposerConverter.ConvertToHierarchyNodes(allNodes);

            ILogger<DatabaseComposer> databaseLogger = NullLogger<DatabaseComposer>.Instance;
            var exporter = new DatabaseComposer(databaseLogger);
            exporter.ComposeDatabase(nodes.ToList(), Path.GetFullPath(databasePath));
        }

        public static IEnumerable<SectorInfo> SplitIntoSectors(
            List<APrimitive> allGeometries,
            ulong instancedMeshesFileId,
            int recursiveDepth,
            string? parentPath,
            uint? parentSectorId,
            SequentialIdGenerator meshFileIdGenerator,
            SequentialIdGenerator sectorIdGenerator)
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
            var isLeaf = recursiveDepth > 5 || allGeometries.Count < 10000 || grouped.Count == 1;

            if (isLeaf)
            {
                var sectorId = (uint)sectorIdGenerator.GetNextId();
                var hasInstanceMeshes = allGeometries.OfType<InstancedMesh>().Any();
                var hasTriangleMeshes = allGeometries.OfType<TriangleMesh>().Any();
                var meshId = hasTriangleMeshes ? meshFileIdGenerator.GetNextId() : (ulong?)null;
                yield return new SectorInfo(
                    sectorId,
                    parentSectorId,
                    recursiveDepth,
                    $"{parentPath}/{sectorId}",
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
                        var geometries = group.SelectMany(x => x).ToList();
                        var hasInstanceMeshes = geometries.OfType<InstancedMesh>().Any();
                        var hasTriangleMeshes = geometries.OfType<TriangleMesh>().Any();
                        var meshId = hasTriangleMeshes ? meshFileIdGenerator.GetNextId() : (ulong?)null;
                        var sectorId = (uint)sectorIdGenerator.GetNextId();
                        var path = isRoot
                            ? $"{sectorId}"
                            : $"{parentPath}/{sectorId}";

                        parentPathForChildren = path;
                        parentSectorIdForChildren = sectorId;

                        yield return new SectorInfo(
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
                            group.SelectMany(x => x).ToList(),
                            instancedMeshesFileId,
                            recursiveDepth + 1,
                            parentPathForChildren,
                            parentSectorIdForChildren,
                            meshFileIdGenerator,
                            sectorIdGenerator);
                        foreach (var sector in sectors)
                        {
                            yield return sector;
                        }
                    }
                }
            }
        }

        public static void WriteSceneFile(ImmutableArray<SectorInfo> sectors, CadRevealComposerRunner.Parameters parameters, DirectoryInfo outputDirectory, ulong maxTreeIndex)
        {
            static Sector FromSector(SectorInfo sector)
            {
                return new Sector
                {
                    Id = sector.SectorId,
                    ParentId = sector.ParentSectorId.HasValue
                        ? sector.ParentSectorId.Value
                        : -1,
                    BoundingBox =
                        new BoundingBox(
                            Min: new BbVector3(sector.BoundingBox.Min.X, sector.BoundingBox.Min.Y, sector.BoundingBox.Min.Z),
                            Max: new BbVector3(sector.BoundingBox.Max.X, sector.BoundingBox.Max.Y, sector.BoundingBox.Max.Z)
                        ),
                    Depth = sector.Depth,
                    Path = sector.Path,
                    IndexFile = new IndexFile(
                        FileName: sector.Filename,
                        DownloadSize: sector.DownloadSize,
                        PeripheralFiles: sector.PeripheralFiles),
                    FacesFile = null, // Not implemented
                    EstimatedTriangleCount = sector.EstimatedTriangleCount,
                    EstimatedDrawCallCount = sector.EstimatedDrawCallCount
                };
            }

            var scene = new Scene
            {
                Version = 8,
                ProjectId = parameters.ProjectId,
                ModelId = parameters.ModelId,
                RevisionId = parameters.RevisionId,
                SubRevisionId = -1,
                MaxTreeIndex = maxTreeIndex,
                Unit = "Meters",
                Sectors = sectors.Select(FromSector).ToArray()
            };

            var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
            JsonSerializeToFile(scene, scenePath, Formatting.Indented);
        }

        public static void ExportSector(SectorInfo sector, DirectoryInfo outputDirectory)
        {
            var geometries = sector.Geometries;
            TriangleMesh[] exported = Array.Empty<TriangleMesh>();

            if (sector.MeshFileId.HasValue)
            {
                var objExportTimer = Stopwatch.StartNew();
                var triangleMeshes = sector.Geometries.OfType<TriangleMesh>().ToArray();
                exported = ExportMeshesToObjFile(outputDirectory, sector.MeshFileId.Value, triangleMeshes).ToArray();
                objExportTimer.Stop();
                Console.WriteLine($"Mesh .obj file exported (On background thread) in {objExportTimer.Elapsed}");

                geometries = geometries
                    .Except(triangleMeshes)
                    .Concat(exported)
                    .ToImmutableList();
            }


            var groupAttributesTimer = Stopwatch.StartNew();
            Console.WriteLine("Start Group Attributes and create i3d file structure");

            var attributesTimer = Stopwatch.StartNew();
            Console.WriteLine($"Attribute Grouping: {attributesTimer.Elapsed}");

            var colors = ImmutableSortedSet<Color>.Empty;
            var diagonals = ImmutableSortedSet<float>.Empty;
            var centerX = ImmutableSortedSet<float>.Empty;
            var centerY = ImmutableSortedSet<float>.Empty;
            var centerZ = ImmutableSortedSet<float>.Empty;
            var normals = ImmutableSortedSet<Vector3>.Empty;
            var deltas = ImmutableSortedSet<float>.Empty;
            var heights = ImmutableSortedSet<float>.Empty;
            var radii = ImmutableSortedSet<float>.Empty;
            var angles = ImmutableSortedSet<float>.Empty;
            var translationsX = ImmutableSortedSet<float>.Empty;
            var translationsY = ImmutableSortedSet<float>.Empty;
            var translationsZ = ImmutableSortedSet<float>.Empty;
            var scalesX = ImmutableSortedSet<float>.Empty;
            var scalesY = ImmutableSortedSet<float>.Empty;
            var scalesZ = ImmutableSortedSet<float>.Empty;
            var fileIds = Array.Empty<ulong>();
            var textures = Array.Empty<Texture>();

            var getAttributeValuesTimer = Stopwatch.StartNew();
            foreach (var attributeKind in Enum.GetValues<I3dfAttribute.AttributeType>())
            {
                switch (attributeKind)
                {
                    case I3dfAttribute.AttributeType.Null:
                        // Intentionally ignored
                        break;
                    case I3dfAttribute.AttributeType.Color:
                        // ReSharper disable once RedundantTypeArgumentsOfMethod
                        colors = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<Color>(
                            geometries, attributeKind, new RgbaColorComparer());
                        break;
                    case I3dfAttribute.AttributeType.Diagonal:
                        diagonals =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterX:
                        centerX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterY:
                        centerY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterZ:
                        centerZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Normal:
                        // ReSharper disable once RedundantTypeArgumentsOfMethod
                        normals =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<Vector3>(
                                geometries, attributeKind, new XyzVector3Comparer());
                        break;
                    case I3dfAttribute.AttributeType.Delta:
                        deltas =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Height:
                        heights =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Radius:
                        radii = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                            geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Angle:
                        angles = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                            geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationX:
                        translationsX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationY:
                        translationsY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationZ:
                        translationsZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleX:
                        scalesX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleY:
                        scalesY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleZ:
                        scalesZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.FileId:
                        fileIds =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<ulong>(
                                geometries, attributeKind).ToArray();
                        break;
                    case I3dfAttribute.AttributeType.Texture:
                        textures = Array.Empty<Texture>();
                        break;
                    case I3dfAttribute.AttributeType.Ignore:
                        // AttributeType.Ignore are intentionally ignored, and not exported.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(attributeKind), attributeKind, "Unexpected i3df attribute.");
                }
            }

            Console.WriteLine($"Retrieved all distinct attributes in: {getAttributeValuesTimer.Elapsed}");

            var primitiveCollections = new PrimitiveCollections();
            foreach (var geometriesByType in geometries.GroupBy(g => g.GetType()))
            {
                var elementType = geometriesByType.Key;
                if (elementType == typeof(ProtoMesh))
                    continue; // ProtoMesh is a temporary primitive, and should not be exported.
                var elements = geometriesByType.ToArray();
                if (elementType == typeof(TriangleMesh))
                    if (!Enumerable.SequenceEqual(exported, elements))
                        throw new Exception("Triangle mesh sequences are not equal");
                
                var fieldInfo = primitiveCollections.GetType().GetFields()
                    .First(pc => pc.FieldType.GetElementType() == elementType);
                var typedArray = Array.CreateInstance(elementType, elements.Length);
                Array.Copy(elements, typedArray, elements.Length);
                fieldInfo.SetValue(primitiveCollections, typedArray);
            }

            var file = new FileI3D
            {
                FileSector = new FileSector
                {
                    Header = new Header
                    {
                        // Constants
                        MagicBytes = I3DFMagicBytes,
                        FormatVersion = 8,
                        OptimizerVersion = 1,

                        // Arbitrary selected numbers
                        SectorId = sector.SectorId,
                        ParentSectorId = sector.ParentSectorId,
                        BboxMax = sector.BoundingBox.Max,
                        BboxMin = sector.BoundingBox.Min,
                        Attributes = new Attributes
                        {
                            Angle = angles,
                            CenterX = centerX,
                            CenterY = centerY,
                            CenterZ = centerZ,
                            Color = colors,
                            Normal = normals,
                            Delta = deltas,
                            Diagonal = diagonals,
                            ScaleX = scalesX,
                            ScaleY = scalesY,
                            ScaleZ = scalesZ,
                            TranslationX = translationsX,
                            TranslationY = translationsY,
                            TranslationZ = translationsZ,
                            Radius = radii,
                            FileId = fileIds,
                            Height = heights,
                            Texture = textures
                        }
                    },
                    PrimitiveCollections = primitiveCollections
                }
            };
            Console.WriteLine($"Group Attributes and create i3d file structure: {groupAttributesTimer.Elapsed}");

            var i3dTimer = Stopwatch.StartNew();
            var filepath = Path.Join(outputDirectory.FullName, $"sector_{file.FileSector.Header.SectorId}.i3d");
            using var i3dSectorFile = File.Create(filepath);
            I3dWriter.WriteSector(file.FileSector, i3dSectorFile);
            Console.WriteLine($"Finished writing i3d Sectors in {i3dTimer.Elapsed}");

            Console.WriteLine($"Total primitives {geometries.Count}/{PrimitiveCounter.pc}");
            Console.WriteLine($"Missing: {PrimitiveCounter.ToString()}");
        }

        public static List<InstancedMesh> ExportInstancedMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshFileId, IReadOnlyList<InstancedMesh> meshGeometries)
        {
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshFileId}.obj"));
            objExporter.StartObject("root");
            var exportedInstancedMeshes = new List<InstancedMesh>();
            uint triangleOffset = 0;

            var counter = 0;
            foreach (var instancedMeshesGroupedByMesh in meshGeometries.GroupBy(x => x.TempTessellatedMesh))
            {
                counter++;
                var mesh = instancedMeshesGroupedByMesh.Key;

                if (mesh == null)
                    throw new ArgumentException(
                        $"Expected meshGeometries to not have \"null\" meshes, was null on {instancedMeshesGroupedByMesh}",
                        nameof(meshGeometries));
                uint triangleCount = (uint)mesh.Triangles.Count / 3;

                objExporter.WriteMesh(mesh);

                // Create new InstancedMesh for all the InstancedMesh that were exported here.
                // This makes it possible to set the TriangleOffset
                IEnumerable<InstancedMesh> adjustedInstancedMeshes = instancedMeshesGroupedByMesh
                    .Select(instancedMesh => instancedMesh with
                    {
                        FileId = meshFileId,
                        TriangleOffset = triangleOffset,
                        TriangleCount = triangleCount,
                        TempTessellatedMesh = null // Remove this, no longer used.
                    })
                    .ToArray();

                exportedInstancedMeshes.AddRange(
                    adjustedInstancedMeshes);

                triangleOffset += triangleCount;
            }

            Console.WriteLine($"{counter} distinct instanced meshes exported to MeshFile{meshFileId}");

            return exportedInstancedMeshes;
        }

        private static IEnumerable<TriangleMesh> ExportMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshFileId, IReadOnlyList<TriangleMesh> meshGeometries)
        {
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshFileId}.obj"));
            objExporter.StartObject("root"); // Keep a single object in each file

            // Export Meshes
            foreach (var triangleMesh in meshGeometries)
            {
                if (triangleMesh.TempTessellatedMesh == null)
                    throw new ArgumentNullException(nameof(triangleMesh.TempTessellatedMesh),
                        "Expected all TriangleMeshes to have a temp mesh when exporting");

                objExporter.WriteMesh(triangleMesh.TempTessellatedMesh);

                yield return triangleMesh with
                {
                    FileId = meshFileId,
                    TempTessellatedMesh = null // Remove this, no longer used.
                };
            }
        }

        private static void JsonSerializeToFile<T>(T obj, string filename, Formatting formatting = Formatting.None)
        {
            using var stream = File.Create(filename);
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer);
            var jsonSerializer = new JsonSerializer { Formatting = formatting };
            jsonSerializer.Serialize(jsonWriter, obj);
        }
    }
}