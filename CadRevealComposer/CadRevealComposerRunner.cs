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
        private const int I3DFMagicBytes = 1178874697; // I3DF chars as bytes.
        private static readonly TreeIndexGenerator TreeIndexGenerator = new();
        private static readonly NodeIdProvider NodeIdGenerator = new();
        private static readonly SequentialIdGenerator MeshIdGenerator = new SequentialIdGenerator();

        public record Parameters(ProjectId ProjectId, ModelId ModelId, RevisionId RevisionId);

        public static void Process(
            DirectoryInfo inputRvmFolderPath,
            DirectoryInfo outputDirectory,
            Parameters parameters)
        {
            var workload = Workload.CollectWorkload(new[] { inputRvmFolderPath.FullName });

            Console.WriteLine("Reading RvmData");
            var rvmTimer = Stopwatch.StartNew();
            var progressReport = new Progress<(string fileName, int progress, int total)>((x) =>
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






            var exportInstancedMeshes = Stopwatch.StartNew();
            var instancedMeshesFileId = MeshIdGenerator.GetNextId();

            // The following code should be refactored, i'm just not sure how
            // We need to remove all instancedMeshes, and the re-add them.
            //  The reason for this is that they are immutable, and we actually add new copies with altered data.
            var notYetExportedInstancedMeshes = geometries.OfType<InstancedMesh>().ToArray();

            geometries = geometries.Except(notYetExportedInstancedMeshes).ToList();

            var protoMeshes = geometries.OfType<ProtoMesh>().ToArray();
            var meshInstanceDictionary = RvmFacetGroupMatcher.MatchAll(protoMeshes.Select(p => p.SourceMesh).ToArray());
            geometries = geometries.Where(g => g is not ProtoMesh).ToList();

            var exportedInstancedMeshes = ExportInstancedMeshesToObjFile(outputDirectory, instancedMeshesFileId, notYetExportedInstancedMeshes);
            geometries.AddRange(exportedInstancedMeshes);

            Console.WriteLine($"Exported instances in {exportInstancedMeshes.Elapsed}");

            Console.WriteLine($"Finished Geometry Conversion in: {geometryConversionTimer.Elapsed}");







            var sectors = SplitIntoSectors(geometries, allNodes, rootNode.BoundingBoxAxisAligned!);
            var sectorFiles = sectors
                    .Select(s => ExportSector(outputDirectory, MeshIdGenerator.GetNextId(), s.Geometries, s.Nodes, s.BoundingBox))
                    .ToArray();
            WriteSceneFile(sectorFiles, parameters, outputDirectory);

            Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        }

        private static IEnumerable<(List<APrimitive> Geometries, CadRevealNode[] Nodes, RvmBoundingBox BoundingBox)> SplitIntoSectors(List<APrimitive> allGeometries, CadRevealNode[] allNodes, RvmBoundingBox boundingBox)
        {
            // TODO: sort by size
            // TODO: add to sector based until limits on triangle/draw count
            // TODO: 8 sub sectors
            // TODO: group by NodeId?z

            yield return (allGeometries, allNodes, boundingBox);
        }

        private static void WriteSceneFile(IEnumerable<FileI3D> file, Parameters parameters, DirectoryInfo outputDirectory)
        {
            static Sector FromFileSector(FileSector fileSector)
            {
                var sectorFileHeader = fileSector.Header;
                return new Sector()
                {
                    Id = sectorFileHeader.SectorId,
                    ParentId = sectorFileHeader.ParentSectorId ?? -1,
                    BoundingBox =
                        new BoundingBox(
                            Min: new BbVector3(sectorFileHeader.BboxMin.X, sectorFileHeader.BboxMin.Y,
                                sectorFileHeader.BboxMin.Z),
                            Max: new BbVector3(sectorFileHeader.BboxMax.X, sectorFileHeader.BboxMax.Y,
                                sectorFileHeader.BboxMax.Z)
                        ),
                    Depth = sectorFileHeader.ParentSectorId == null ? 1 : throw new NotImplementedException(),
                    Path = sectorFileHeader.SectorId == 0 ? "0/" : throw new NotImplementedException(),
                    IndexFile = new IndexFile(
                        FileName: $"sector_{sectorFileHeader.SectorId}.i3d",
                        DownloadSize: 500001, // TODO: Find a real download size
                        PeripheralFiles: new[] { $"mesh_{meshId}.ctm", $"mesh_{instancedMeshesFileId}.ctm" }),
                    FacesFile = null, // Not implemented
                    EstimatedTriangleCount = fileSector.PrimitiveCollections.TriangleMeshCollection
                            .Sum(x => Convert.ToInt64(x.TriangleCount)), // This is a guesstimate, not sure how to calculate non-mesh triangle count,
                    EstimatedDrawCallCount = 1337 // Not calculated
                };
            }

            var scene = new Scene()
            {
                Version = 8,
                ProjectId = parameters.ProjectId,
                ModelId = parameters.ModelId,
                RevisionId = parameters.RevisionId,
                SubRevisionId = -1,
                MaxTreeIndex = TreeIndexGenerator.CurrentMaxGeneratedIndex,
                Unit = "Meters",
                Sectors = file.Select(x => FromFileSector(x.FileSector)).ToArray()
            };

            var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
            JsonSerializeToFile(scene, scenePath, Formatting.Indented);
        }

        private static FileI3D ExportSector(
            DirectoryInfo outputDirectory,
            ulong meshId,
            List<APrimitive> geometries,
            CadRevealNode[] allNodes,
            RvmBoundingBox boundingBox)
        {
            var exportObjTask = Task.Run(() =>
            {
                var objExportTimer = Stopwatch.StartNew();
                ExportMeshesToObjFile(outputDirectory, meshId, geometries.OfType<TriangleMesh>().ToArray());
                Console.WriteLine($"Mesh .obj file exported (On background thread) in {objExportTimer.Elapsed}");
            });

            var exportHierarchyTask = Task.Run(() =>
            {
                var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));

                var nodes = HierarchyComposerConverter.ConvertToHierarchyNodes(allNodes);

                ILogger<DatabaseComposer> databaseLogger = NullLogger<DatabaseComposer>.Instance;
                var exporter = new DatabaseComposer(databaseLogger);
                exporter.ComposeDatabase(nodes.ToList(), Path.GetFullPath(databasePath));

                // TODO: Export DatabaseId to some metadata available to the Model Service.
                Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
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
                            geometries, attributeKind, new RgbaColorComparer());
                        break;
                    case I3dfAttribute.AttributeType.Diagonal:
                        diagonals =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterX:
                        centerX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterY:
                        centerY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.CenterZ:
                        centerZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Normal:
                        // ReSharper disable once RedundantTypeArgumentsOfMethod
                        normals = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<Vector3>(
                            geometries, attributeKind, new XyzVector3Comparer());
                        break;
                    case I3dfAttribute.AttributeType.Delta:
                        deltas = APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(
                            geometries, attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.Height:
                        heights =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
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
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationY:
                        translationsY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.TranslationZ:
                        translationsZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleX:
                        scalesX =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleY:
                        scalesY =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.ScaleZ:
                        scalesZ =
                            APrimitiveReflectionHelpers.GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries,
                                attributeKind);
                        break;
                    case I3dfAttribute.AttributeType.FileId:
                        fileIds = APrimitiveReflectionHelpers
                            .GetDistinctValuesOfAllPropertiesMatchingKind<ulong>(geometries, attributeKind).ToArray();
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
                var fieldInfo = primitiveCollections.GetType().GetFields()
                    .First(pc => pc.FieldType.GetElementType() == elementType);
                var typedArray = Array.CreateInstance(elementType, elements.Length);
                Array.Copy(elements, typedArray, elements.Length);
                fieldInfo.SetValue(primitiveCollections, typedArray);
            }

            var file = new FileI3D()
            {
                FileSector = new FileSector
                {
                    Header = new Header()
                    {
                        // Constants
                        MagicBytes = I3DFMagicBytes,
                        FormatVersion = 8,
                        OptimizerVersion = 1,

                        // Arbitrary selected numbers
                        SectorId = 0,
                        ParentSectorId = null,
                        BboxMax = boundingBox.Max,
                        BboxMin = boundingBox.Min,
                        Attributes = new Attributes()
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

            // Wait until obj and hierarchy export is done
            Task.WaitAll(exportObjTask, exportHierarchyTask);

            return file;
        }

        private static List<InstancedMesh> ExportInstancedMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshFileId, IReadOnlyList<InstancedMesh> meshGeometries)
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

        private static void ExportMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshId, IReadOnlyList<TriangleMesh> meshGeometries)
        {
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshId}.obj"));
            objExporter.StartObject($"root"); // Keep a single object in each file

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