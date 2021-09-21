namespace CadRevealComposer
{
    using HierarchyComposer.Functions;
    using IdProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using Primitives;
    using Primitives.Converters;
    using Primitives.Instancing;
    using Primitives.Reflection;
    using RvmSharp.BatchUtils;
    using RvmSharp.Containers;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
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

    public record ProjectId(long Value);

    public record ModelId(long Value);

    public record RevisionId(long Value);

    public static class CadRevealComposerRunner
    {
        private record SectorInfo(
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

        private const int I3DFMagicBytes = 1178874697; // I3DF chars as bytes.
        private static readonly TreeIndexGenerator TreeIndexGenerator = new();
        private static readonly NodeIdProvider NodeIdGenerator = new();
        private static readonly SequentialIdGenerator MeshIdGenerator = new SequentialIdGenerator();
        private static readonly SequentialIdGenerator SectorIdGenerator = new SequentialIdGenerator();

        public record Parameters(ProjectId ProjectId, ModelId ModelId, RevisionId RevisionId);

        public static void Process(
            DirectoryInfo inputRvmFolderPath,
            DirectoryInfo outputDirectory,
            Parameters parameters)
        {
            var workload = Workload.CollectWorkload(new[] { inputRvmFolderPath.FullName });

            Console.WriteLine("Reading RvmData");
            var rvmTimer = Stopwatch.StartNew();
            var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
            {
                Console.WriteLine($"{x.fileName} ({x.progress}/{x.total})");
            });
            var rvmStore = Workload.ReadRvmData(workload, progressReport);
            Console.WriteLine($"Read RvmData in {rvmTimer.Elapsed}");

            ProcessRvmStore(rvmStore, outputDirectory, parameters);
        }

        private static void ProcessRvmStore(RvmStore rvmStore, DirectoryInfo outputDirectory, Parameters parameters)
        {
            Console.WriteLine("Generating i3d");

            var rootNode = new CadRevealNode
            {
                NodeId = NodeIdGenerator.GetNodeId(null),
                TreeIndex = TreeIndexGenerator.GetNextId(),
                Parent = null,
                Group = null,
                Children = null
            };

            rootNode.Children = rvmStore.RvmFiles
                .SelectMany(f => f.Model.Children)
                .Select(root => CollectGeometryNodesRecursive(root, rootNode))
                .ToArray();

            rootNode.BoundingBoxAxisAligned = BoundingBoxEncapsulate(rootNode.Children
                .Select(x => x.BoundingBoxAxisAligned)
                .WhereNotNull()
                .ToArray());

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null, "Root node has no bounding box. Are there any meshes in the input?");

            var allNodes = GetAllNodesFlat(rootNode).ToArray();

            var pyramidInstancingTimer = Stopwatch.StartNew();
            PyramidInstancingHelper pyramidInstancingHelper = new PyramidInstancingHelper(allNodes);
            Console.WriteLine($"Prepared Pyramids in {pyramidInstancingTimer.Elapsed}");

            var geometryConversionTimer = Stopwatch.StartNew();
            // AsOrdered is important. And I dont like it...
            //  - Its important  of the "TriangleMesh TriangleCount" is "sequential-additive".
            // So the position offset in the mesh is determined on the TriangleCount of all items in the Sequence "12"+"16"+"10", and needs the identical order.
            var geometries = allNodes
                .AsParallel()
                .AsOrdered()
                .SelectMany(x => x.RvmGeometries.Select(primitive =>
                    APrimitive.FromRvmPrimitive(x, x.Group as RvmNode ?? throw new InvalidOperationException(),
                        primitive, pyramidInstancingHelper)))
                .WhereNotNull()
                .ToList();







            var exportHierarchyDatabaseTask = Task.Run(() =>
            {
                var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
                ExportHierarchyDatabase(databasePath, allNodes);
                Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
            });

            var exportInstancedMeshes = Stopwatch.StartNew();
            var instancedMeshesFileId = MeshIdGenerator.GetNextId();

            // The following code should be refactored, i'm just not sure how
            // We need to remove all instancedMeshes, and the re-add them.
            //  The reason for this is that they are immutable, and we actually add new copies with altered data.
            var instancedMeshes = geometries.OfType<InstancedMesh>().ToArray();
            var protoMeshes = geometries.OfType<ProtoMesh>().ToArray();

            static IEnumerable<InstancedMesh> ToInstanceMesh(IGrouping<RvmFacetGroup, KeyValuePair<ProtoMesh, (RvmFacetGroup template, Matrix4x4 transform)>> group)
            {
                var template = group.Key;
                var mesh = TessellatorBridge.Tessellate(template, -1f); // tolerance unused for RvmFacetGroup
                foreach (var primitive in group)
                {
                    Matrix4x4.Decompose(primitive.Value.transform, out var scale, out var rotation, out var translation);
                    var (rollX, pitchY, yawZ) = rotation.ToEulerAngles();
                    yield return new InstancedMesh(
                        new CommonPrimitiveProperties(primitive.Key.NodeId, primitive.Key.TreeIndex, translation, rotation, scale, primitive.Key.Diagonal, primitive.Key.AxisAlignedBoundingBox, primitive.Key.Color, (Vector3.One, 0f)), // TODO: fix
                        0, 0, 0,
                        translation.X, translation.Y, translation.Z,
                        rollX, pitchY, yawZ,
                        scale.X, scale.Y, scale.Z)
                    {
                        TempTessellatedMesh = mesh
                    };
                }
            }

            var instancedMeshesFromProtoMeshes = RvmFacetGroupMatcher.MatchAll(protoMeshes)
                .GroupBy(x => x.Value.template)
                .SelectMany(ToInstanceMesh)
                .ToImmutableList();
            
            var allInstancedMeshes = instancedMeshes.Concat(instancedMeshesFromProtoMeshes).ToList();
            var exportedInstancedMeshes = ExportInstancedMeshesToObjFile(outputDirectory, instancedMeshesFileId, allInstancedMeshes);

            geometries = geometries
                .Except(instancedMeshes)
                .Except(protoMeshes)
                .Concat(exportedInstancedMeshes)
                .ToList();

            Console.WriteLine($"Exported instances in {exportInstancedMeshes.Elapsed}");

            Console.WriteLine($"Finished Geometry Conversion in: {geometryConversionTimer.Elapsed}");

            var sectors = SplitIntoSectors(geometries, instancedMeshesFileId, (uint)SectorIdGenerator.GetNextId(), 0, "0", null)
                .OrderBy(x => x.SectorId)
                .ToImmutableArray();
            foreach (var sector in sectors)
            {
                ExportSector(sector, outputDirectory);
            }
            WriteSceneFile(sectors, parameters, outputDirectory);

            Task.WaitAll(exportHierarchyDatabaseTask);

            Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        }

        private static void ExportHierarchyDatabase(string databasePath, CadRevealNode[] allNodes)
        {
            var nodes = HierarchyComposerConverter.ConvertToHierarchyNodes(allNodes);

            ILogger<DatabaseComposer> databaseLogger = NullLogger<DatabaseComposer>.Instance;
            var exporter = new DatabaseComposer(databaseLogger);
            exporter.ComposeDatabase(nodes.ToList(), Path.GetFullPath(databasePath));

            // TODO: Export DatabaseId to some metadata available to the Model Service.
        }

        private static IEnumerable<SectorInfo> SplitIntoSectors(List<APrimitive> allGeometries, ulong instancedMeshesFileId, uint sectorId, int depth, string path, uint? parentSectorId)
        {
            static int CalculateVoxelKey(RvmBoundingBox boundingBox, float midX, float midY, float midZ)
            {
                if (boundingBox.Min.X < midX && boundingBox.Max.X > midX ||
                    boundingBox.Min.Y < midY && boundingBox.Max.Y > midY ||
                    boundingBox.Min.Z < midZ && boundingBox.Max.Z > midZ )
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
            var isLeaf = depth > 5 || allGeometries.Count < 5000 || grouped.Count == 1;

            if (isLeaf)
            {
                var meshId = MeshIdGenerator.GetNextId();
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
                        var meshId = MeshIdGenerator.GetNextId();
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
                        var meshId = MeshIdGenerator.GetNextId();
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
                        var id = SectorIdGenerator.GetNextId();
                        var sectors = SplitIntoSectors(group.ToList(), instancedMeshesFileId, (uint)id, depth + 1, $"{path}/{id}", sectorId);
                        foreach (var sector in sectors)
                        {
                            yield return sector;
                        }
                    }
                }
            }
        }

        private static void WriteSceneFile(ImmutableArray<SectorInfo> sectors, Parameters parameters, DirectoryInfo outputDirectory)
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
                MaxTreeIndex = TreeIndexGenerator.CurrentMaxGeneratedIndex,
                Unit = "Meters",
                Sectors = sectors.Select(FromSector).ToArray()
            };

            var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
            JsonSerializeToFile(scene, scenePath, Formatting.Indented);
        }

        private static void ExportSector(SectorInfo sector, DirectoryInfo outputDirectory)
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

        private static List<InstancedMesh> ExportInstancedMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshId, IReadOnlyList<InstancedMesh> meshGeometries)
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

        private static IEnumerable<CadRevealNode> GetAllNodesFlat(CadRevealNode root)
        {
            yield return root;

            if (root.Children != null)
            {
                foreach (CadRevealNode cadRevealNode in root.Children)
                {
                    foreach (CadRevealNode revealNode in GetAllNodesFlat(cadRevealNode))
                    {
                        yield return revealNode;
                    }
                }
            }
        }

        public static CadRevealNode CollectGeometryNodesRecursive(RvmNode root, CadRevealNode parent)
        {
            var newNode = new CadRevealNode
            {
                NodeId = NodeIdGenerator.GetNodeId(null),
                TreeIndex = TreeIndexGenerator.GetNextId(),
                Group = root,
                Parent = parent,
                Children = null
            };

            CadRevealNode[] childrenCadNodes;
            RvmPrimitive[] rvmGeometries = Array.Empty<RvmPrimitive>();


            if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
            {
                childrenCadNodes = root.Children.Select(child =>
                {
                    switch (child)
                    {
                        case RvmPrimitive rvmPrimitive:
                            return CollectGeometryNodesRecursive(
                                new RvmNode(2, "Implicit geometry", root.Translation, root.MaterialId)
                                {
                                    Children = { rvmPrimitive }
                                }, newNode);
                        case RvmNode rvmNode:
                            return CollectGeometryNodesRecursive(rvmNode, newNode);
                        default:
                            throw new Exception();
                    }
                }).ToArray();
            }
            else
            {
                childrenCadNodes = root.Children.OfType<RvmNode>()
                    .Select(n => CollectGeometryNodesRecursive(n, newNode))
                    .ToArray();
                rvmGeometries = root.Children.OfType<RvmPrimitive>().ToArray();
            }

            newNode.RvmGeometries = rvmGeometries;
            newNode.Children = childrenCadNodes;

            var primitiveBoundingBoxes = root.Children.OfType<RvmPrimitive>()
                .Select(x => x.CalculateAxisAlignedBoundingBox()).ToArray();
            var childrenBounds = newNode.Children.Select(x => x.BoundingBoxAxisAligned)
                .WhereNotNull();

            var primitiveAndChildrenBoundingBoxes = primitiveBoundingBoxes.Concat(childrenBounds).ToArray();
            newNode.BoundingBoxAxisAligned = BoundingBoxEncapsulate(primitiveAndChildrenBoundingBoxes);

            return newNode;
        }

        private static RvmBoundingBox? BoundingBoxEncapsulate(RvmBoundingBox[] boundingBoxes)
        {
            if (!boundingBoxes.Any())
                return null;

            // Find the min and max values for each of x,y, and z dimensions.
            var min = boundingBoxes.Select(x => x.Min).Aggregate(Vector3.Min);
            var max = boundingBoxes.Select(x => x.Max).Aggregate(Vector3.Max);
            return new RvmBoundingBox(Min: min, Max: max);
        }
    }
}