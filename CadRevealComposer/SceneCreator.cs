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
            ulong MeshId,
            IReadOnlyList<APrimitive> Geometries,
            RvmBoundingBox BoundingBox
        );

        public static void ExportHierarchyDatabase(string databasePath, CadRevealNode[] allNodes)
        {
            var nodes = HierarchyComposerConverter.ConvertToHierarchyNodes(allNodes);

            ILogger<DatabaseComposer> databaseLogger = NullLogger<DatabaseComposer>.Instance;
            var exporter = new DatabaseComposer(databaseLogger);
            exporter.ComposeDatabase(nodes.ToList(), Path.GetFullPath(databasePath));

            // TODO: Export DatabaseId to some metadata available to the Model Service.
        }

        public static IEnumerable<SectorInfo> SplitIntoSectors(
            List<APrimitive> allGeometries,
            ulong instancedMeshesFileId,
            uint sectorId,
            int depth,
            string path,
            uint? parentSectorId,
            SequentialIdGenerator meshIdGenerator,
            SequentialIdGenerator sectorIdGenerator)
        {
            static int CalculateVoxelKey(RvmBoundingBox boundingBox, float midX, float midY, float midZ)
            {
                if (boundingBox.Min.X < midX && boundingBox.Max.X > midX ||
                    boundingBox.Min.Y < midY && boundingBox.Max.Y > midY ||
                    boundingBox.Min.Z < midZ && boundingBox.Max.Z > midZ)
                {
                    return 0; // crosses the mid boundary in either X,Y,Z
                }

                // categorize which of the 8 sub sectors it belongs to
                return (boundingBox.Min.X < midX, boundingBox.Min.Y < midY, boundingBox.Min.Z < midZ) switch
                {
                    (false, false, false) => 1,
                    (false, false, true) => 2,
                    (false, true, false) => 3,
                    (false, true, true) => 4,
                    (true, false, false) => 5,
                    (true, false, true) => 6,
                    (true, true, false) => 7,
                    (true, true, true) => 8
                };
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
                .GroupBy(x => CalculateVoxelKey(x.AxisAlignedBoundingBox, midX, midY, midZ))
                .OrderBy(x => x.Key)
                .ToImmutableList();

            var isRoot = sectorId == 0;
            var isLeaf = depth > 5 || allGeometries.Count < 10000 || grouped.Count == 1;

            if (isLeaf)
            {
                var meshId = meshIdGenerator.GetNextId();
                yield return new SectorInfo(
                    sectorId,
                    parentSectorId,
                    depth,
                    path,
                    $"sector_{sectorId}.i3d",
                    new[] { $"mesh_{meshId}.ctm" },
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
                var hasRootVoxel = grouped.Any(x => x.Key == 0); // TODO: fix this one, ERL
                foreach (IGrouping<int, APrimitive> group in grouped)
                {
                    if (!hasRootVoxel)
                    {
                        var meshId = meshIdGenerator.GetNextId();
                        yield return new SectorInfo(
                            sectorId,
                            parentSectorId,
                            depth,
                            path,
                            $"sector_{sectorId}.i3d",
                            Array.Empty<string>(),
                            0,
                            0,
                            meshId,
                            Array.Empty<APrimitive>(),
                            new RvmBoundingBox(
                                new Vector3(minX, minY, minZ),
                                new Vector3(maxX, maxY, maxZ)
                            ));
                    }
                    if (group.Key == 0)
                    {
                        var meshId = meshIdGenerator.GetNextId();
                        yield return new SectorInfo(
                            sectorId,
                            parentSectorId,
                            depth,
                            path,
                            $"sector_{sectorId}.i3d",
                            isRoot
                                ? new[] { $"mesh_{instancedMeshesFileId}.ctm", $"mesh_{meshId}.ctm" }
                                : new[] { $"mesh_{meshId}.ctm" },
                            1234, // TODO: calculate
                            1, // TODO: calculate
                            meshId,
                            group.ToList(),
                            new RvmBoundingBox(
                                new Vector3(minX, minY, minZ),
                                new Vector3(maxX, maxY, maxZ)
                            ));
                    }
                    else
                    {
                        var id = sectorIdGenerator.GetNextId();
                        var sectors = SplitIntoSectors(group.ToList(), instancedMeshesFileId, (uint)id, depth + 1, $"{path}/{id}", sectorId, meshIdGenerator, sectorIdGenerator);
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
                        DownloadSize: 0, // TODO: update scene file after generating ctm files
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
            var exportObjTask = Task.Run(() =>
            {
                var objExportTimer = Stopwatch.StartNew();
                ExportMeshesToObjFile(outputDirectory, sector.MeshId, sector.Geometries.OfType<TriangleMesh>().ToArray());
                Console.WriteLine($"Mesh .obj file exported (On background thread) in {objExportTimer.Elapsed}");
            });

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
                            sector.Geometries, attributeKind, new RgbaColorComparer());
                        break;
                    case I3dfAttribute.AttributeType.Diagonal:
                        diagonals =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterX:
                        centerX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterY:
                        centerY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterZ:
                        centerZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Normal:
                        // ReSharper disable once RedundantTypeArgumentsOfMethod
                        normals =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<Vector3>(
                                sector.Geometries, attributeKind, new XyzVector3Comparer());
                        break;
                    case I3dfAttribute.AttributeType.Delta:
                        deltas =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Height:
                        heights =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Radius:
                        radii = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                            sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Angle:
                        angles = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                            sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationX:
                        translationsX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationY:
                        translationsY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationZ:
                        translationsZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleX:
                        scalesX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleY:
                        scalesY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleZ:
                        scalesZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                                sector.Geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.FileId:
                        fileIds =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<ulong>(
                                sector.Geometries, attributeKind).ToArray();
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
            foreach (var geometriesByType in sector.Geometries.GroupBy(g => g.GetType()))
            {
                var elementType = geometriesByType.Key;
                if (elementType == typeof(ProtoMesh))
                    continue; // ProtoMesh is a temporary primitive, and should not be exported.
                var elements = geometriesByType.ToArray();
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

            Console.WriteLine($"Total primitives {sector.Geometries.Count}/{PrimitiveCounter.pc}");
            Console.WriteLine($"Missing: {PrimitiveCounter.ToString()}");

            // Wait until obj and hierarchy export is done
            Task.WaitAll(exportObjTask);
        }

        public static List<InstancedMesh> ExportInstancedMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshId, IReadOnlyList<InstancedMesh> meshGeometries)
        {
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshId}.obj"));
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
                        FileId = meshId,
                        TriangleOffset = triangleOffset,
                        TriangleCount = triangleCount,
                        TempTessellatedMesh = null // Remove this, no longer used.
                    })
                    .ToArray();

                exportedInstancedMeshes.AddRange(
                    adjustedInstancedMeshes);

                triangleOffset += triangleCount;
            }

            Console.WriteLine($"{counter} distinct instanced meshes exported to MeshFile{meshId}");

            return exportedInstancedMeshes;
        }

        private static void ExportMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshId, IReadOnlyList<TriangleMesh> meshGeometries)
        {
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshId}.obj"));
            objExporter.StartObject("root"); // Keep a single object in each file

            // Export Meshes
            foreach (var triangleMesh in meshGeometries)
            {
                if (triangleMesh.TempTessellatedMesh == null)
                    throw new ArgumentNullException(nameof(triangleMesh.TempTessellatedMesh),
                        "Expected all TriangleMeshes to have a temp mesh when exporting");
                objExporter.WriteMesh(triangleMesh.TempTessellatedMesh);
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