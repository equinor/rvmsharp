namespace CadRevealComposer
{
    using IdProviders;
    using Newtonsoft.Json;
    using Primitives;
    using RvmSharp.BatchUtils;
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
    using System.Reflection;
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
        static readonly TreeIndexGenerator TreeIndexGenerator = new();
        static readonly NodeIdProvider NodeIdGenerator = new();
        private static readonly SequentialIdGenerator MeshIdGenerator = new SequentialIdGenerator();

        public record Parameters(ProjectId ProjectId, ModelId ModelId, RevisionId RevisionId);

        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once CognitiveComplexity
        public static void Process(DirectoryInfo inputRvmFolderPath, DirectoryInfo outputDirectory,
            Parameters parameters)
        {
            var workload = Workload.CollectWorkload(new[] { inputRvmFolderPath.FullName });


            Console.WriteLine("Reading RvmData");
            var rvmTimer = Stopwatch.StartNew();
            var progressReport = new Progress<(string fileName, int progress, int total)>((x) =>
            {
                Console.WriteLine(x.fileName + $" ({x.progress}/{x.total})");
            });
            var rvmStore = Workload.ReadRvmData(workload, progressReport);
            Console.WriteLine("Read RvmData in " + rvmTimer.Elapsed);

            Console.WriteLine("Generating i3d");
            // Project name og project parameters tull from Cad Control Center
            var rootNode =
                new CadRevealNode
                {
                    NodeId = NodeIdGenerator.GetNodeId(null),
                    TreeIndex = TreeIndexGenerator.GetNextId(),
                    Parent = null,
                    Group = null,
                    Children = null,
                };


            var meshId = MeshIdGenerator.GetNextId();
            rootNode.Children = rvmStore.RvmFiles.SelectMany(f => f.Model.Children)
                .Select(root => CollectGeometryNodesRecursive(root, rootNode)).ToArray();

            rootNode.BoundingBoxAxisAligned =
                BoundingBoxEncapsulate(rootNode.Children.Select(x => x.BoundingBoxAxisAligned).WhereNotNull()
                    .ToArray());

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null,
                "Root node has no bounding box. Are there any meshes in the input?");
            var boundingBox = rootNode.BoundingBoxAxisAligned!;

            var allNodes = GetAllNodesFlat(rootNode).ToArray();

            var geometryConversionTimer = Stopwatch.StartNew();
            // AsOrdered is important. And I dont like it...
            //  - Its important  of the "TriangleMesh TriangleCount" is "sequential-additive".
            // So the position offset in the mesh is determined on the TriangleCount of all items in the Sequence "12"+"16"+"10", and needs the identical order.
            var geometries = allNodes.AsParallel().AsOrdered().SelectMany(x => x.RvmGeometries.Select(primitive =>
                    APrimitive.FromRvmPrimitive(x, x.Group as RvmNode ?? throw new InvalidOperationException(),
                        primitive)))
                .WhereNotNull().ToArray();
            Console.WriteLine("Geometry Conversion: " + geometryConversionTimer.Elapsed);

            var exportObjTask = Task.Run(() =>
            {
                var objExportTimer = Stopwatch.StartNew();
                ExportMeshesToObjFile(outputDirectory, meshId, geometries.OfType<TriangleMesh>().ToArray());
                Console.WriteLine("Mesh .obj file exported (On background thread) in " + objExportTimer.Elapsed);
            });

            var infoNodesTask = Task.Run(() =>
            {
                var infoNodesTimer = Stopwatch.StartNew();
                var infoNodes = allNodes
                    .Where(n => n.Group is RvmNode).Select(n =>
                {
                    var rvmNode = (RvmNode)n.Group!;
                    return new CadInfoNode
                    {
                        TreeIndex = n.TreeIndex,
                        Name = rvmNode.Name,
                        Geometries = rvmNode.Children.OfType<RvmPrimitive>().Select(g =>
                        {
                            Matrix4x4.Decompose(g.Matrix, out var scale, out var rotation, out var transform);

                            return new CadGeometry
                            {
                                TypeName = g.GetType().ToString(),
                                Scale = scale,
                                Location = transform,
                                Rotation = rotation,
                                Properties = g.GetType().GetProperties()
                                    .ToDictionary(f => f.Name, f => f.GetValue(g)?.ToString())
                            };
                        }).ToList()
                    };
                }).ToArray();

                var outputFileName2 = Path.Combine(outputDirectory.FullName, "cadnodeinfo.json");
                JsonSerializeToFile(infoNodes, outputFileName2);
                Console.WriteLine("Serialized infonodes (On background thread) in " + infoNodesTimer.Elapsed);
            });

            var groupAttributesTimer = Stopwatch.StartNew();
            Console.WriteLine("Start Group Attributes and create i3d file structure");

            var attributeGrouping = Array.Empty<APrimitive>()
                .SelectMany(g => g.GetType().GetProperties().Select(p => (g, p)))
                .Where(gp => gp.p.GetCustomAttributes(true).OfType<I3dfAttribute>().Any())
                .Select(gp => (gp.g, gp.p, gp.p.GetCustomAttributes(true).OfType<I3dfAttribute>().First()))
                .GroupBy(gpa => gpa.Item3.Type).ToArray();

            var attributesTimer = Stopwatch.StartNew();
            var attributeLookup = typeof(APrimitive)
                .Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(APrimitive)) && !t.IsAbstract)
                .Distinct()
                .SelectMany(pt => pt.GetProperties().Select(p => (pt, p)))
                .Select(p => (p.pt, p.p, p.p.GetCustomAttributes(true).OfType<I3dfAttribute>().FirstOrDefault()))
                .Where(p => p.Item3 != null)
                .GroupBy(ptp => ptp.pt)
                .ToDictionary(
                    x => x.Key,
                    x => x.GroupBy(x => x.Item3!.Type).ToDictionary(x => x.Key, y => y.Select(x => x.p)));

            Console.WriteLine("Attribute Grouping: " + attributesTimer.Elapsed);

            ImmutableSortedSet<Color> colors = ImmutableSortedSet<Color>.Empty;
            ImmutableSortedSet<float> diagonals = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> centerX = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> centerY = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> centerZ = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<Vector3> normals = ImmutableSortedSet<Vector3>.Empty;
            ImmutableSortedSet<float> deltas = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> heights = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> radii = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> angles = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> translationsX = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> translationsY = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> translationsZ = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> scalesX = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> scalesY = ImmutableSortedSet<float>.Empty;
            ImmutableSortedSet<float> scalesZ = ImmutableSortedSet<float>.Empty;
            ulong[] fileIds = Array.Empty<ulong>();
            Texture[] textures = Array.Empty<Texture>();

            var getAttributeValuesTimer = Stopwatch.StartNew();
            foreach (var attributeType in (I3dfAttribute.AttributeType[])Enum.GetValues(typeof(I3dfAttribute.AttributeType)))
            {
                switch (attributeType)
                {
                    case I3dfAttribute.AttributeType.Null:
                        // Intentionally ignored
                        break;
                    case I3dfAttribute.AttributeType.Color:
                        colors = GetDistinctValuesOfAllPropertiesMatchingKind<Color>(geometries, attributeType, attributeLookup, new RgbaColorComparer());
                        break;
                    case I3dfAttribute.AttributeType.Diagonal:
                        diagonals = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.CenterX:
                        centerX = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.CenterY:
                        centerY = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.CenterZ:
                        centerZ = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.Normal:
                        normals = GetDistinctValuesOfAllPropertiesMatchingKind<Vector3>(geometries, attributeType, attributeLookup, new XyzVector3Comparer());
                        break;
                    case I3dfAttribute.AttributeType.Delta:
                        deltas = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.Height:
                        heights = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.Radius:
                        radii = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.Angle:
                        angles = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.TranslationX:
                        translationsX = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.TranslationY:
                        translationsY = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.TranslationZ:
                        translationsZ = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.ScaleX:
                        scalesX = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.ScaleY:
                        scalesY = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.ScaleZ:
                        scalesZ = GetDistinctValuesOfAllPropertiesMatchingKind<float>(geometries, attributeType, attributeLookup);
                        break;
                    case I3dfAttribute.AttributeType.FileId:
                        fileIds = GetDistinctValuesOfAllPropertiesMatchingKind<ulong>(geometries, attributeType, attributeLookup).ToArray();
                        break;
                    case I3dfAttribute.AttributeType.Texture:
                        textures = Array.Empty<Texture>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(attributeType), attributeType, "Unexpected i3df attribute.");
                }
            }

            Console.WriteLine("Retrieved all distinct attributes in: " + getAttributeValuesTimer.Elapsed);

            var primitiveCollections = new PrimitiveCollections();
            foreach (var geometriesByType in geometries.GroupBy(g => g.GetType()))
            {
                var elementType = geometriesByType.Key;
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
            Console.WriteLine("Group Attributes and create i3d file structure: " + groupAttributesTimer.Elapsed);

            var i3dTimer = Stopwatch.StartNew();
            using var i3dSectorFile = File.Create(
                Path.Join(
                    outputDirectory.FullName,
                    $"sector_{file.FileSector.Header.SectorId}.i3d"
                ));
            I3dWriter.WriteSector(file.FileSector, i3dSectorFile);
            Console.WriteLine("Finished writing i3d Sectors in " + i3dTimer.Elapsed);

            var sectorFileHeader = file.FileSector.Header;
            var scene = new Scene()
            {
                Version = 8,
                ProjectId = parameters.ProjectId,
                ModelId = parameters.ModelId,
                RevisionId = parameters.RevisionId,
                SubRevisionId = -1,
                MaxTreeIndex = TreeIndexGenerator.CurrentMaxGeneratedIndex,
                Unit = "Meters",
                Sectors = new[]
                {
                    new Sector()
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
                            FileName: "sector_" + sectorFileHeader.SectorId + ".i3d",
                            DownloadSize: 500001, // TODO: Find a real download size
                            PeripheralFiles: new[] {$"mesh_{meshId}.ctm"}),
                        FacesFile = null, // Not implemented
                        EstimatedTriangleCount =
                            geometries.OfType<TriangleMesh>()
                                .Sum(x => Convert.ToInt64(x
                                    .TriangleCount)), // This is a guesstimate, not sure how to calculate non-mesh triangle count,
                        EstimatedDrawCallCount = 1337 // Not calculated
                    }
                }
            };

            var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
            JsonSerializeToFile(scene, scenePath, Formatting.Indented);

            Console.WriteLine($"Total primitives {geometries.Count()}/{PrimitiveCounter.pc}");
            Console.WriteLine($"Missing: {PrimitiveCounter.ToString()}");

            // Wait until obj export is done (Its probably finished ages ago)
            Task.WaitAll(exportObjTask, infoNodesTask);

            Console.WriteLine(
                    $"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        }


        private static ImmutableSortedSet<T> GetDistinctValuesOfAllPropertiesMatchingKind<T>(IEnumerable<APrimitive> x, I3dfAttribute.AttributeType kind,
            Dictionary<Type, Dictionary<I3dfAttribute.AttributeType, IEnumerable<PropertyInfo>>> attributeLookupDict, IComparer<T>? comparer = null)
        {
            var data = x
                .AsParallel()
                .SelectMany(y => GetAllValuesOfAttributeKind<T>(y, kind, attributeLookupDict))
                .ToImmutableSortedSet(comparer);

            return data;
        }

        private static IEnumerable<T> GetAllValuesOfAttributeKind<T>(APrimitive x, I3dfAttribute.AttributeType attributeKind, Dictionary<Type, Dictionary<I3dfAttribute.AttributeType, IEnumerable<PropertyInfo>>> attributeLookup)
        {
            var type = x.GetType();
            if (!attributeLookup[type].TryGetValue(attributeKind, out var propertyInfos))
            {
                return ArraySegment<T>.Empty;
            }
            IEnumerable<T> i3dfAttributes = propertyInfos!.Select(pi => (T)(pi.GetValue(x) ?? throw new InvalidOperationException()));

            return i3dfAttributes;
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

        private static RvmBoundingBox? BoundingBoxEncapsulate(
            RvmBoundingBox[] boundingBoxes)
        {
            var boxes = boundingBoxes;

            if (!boxes.Any())
                return null;

            // Find the min and max values for each of x,y, and z dimensions.
            var min = boxes.Select(x => x.Min).Aggregate(Vector3.Min);
            var max = boxes.Select(x => x.Max).Aggregate(Vector3.Max);
            return new RvmBoundingBox(Min: min, Max: max);
        }
    }
}