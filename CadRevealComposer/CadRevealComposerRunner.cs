namespace CadRevealComposer
{
    using IdProviders;
    using Newtonsoft.Json;
    using Primitives;
    using Primitives.Converters;
    using RvmSharp.BatchUtils;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;
    using Utils;
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
        public static void Process(DirectoryInfo inputRvmFolderPath, DirectoryInfo outputDirectory, Parameters parameters)
        {
            var workload = Workload.CollectWorkload(new[] {inputRvmFolderPath.FullName});


            Console.WriteLine("Reading RvmData");
            var progressReport = new Progress<(string fileName, int progress, int total)>((x) =>
            {
                Console.WriteLine(x.fileName + $" ({x.progress}/{x.total})");
            });
            var rvmStore = Workload.ReadRvmData(workload, progressReport);
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
            
            rootNode.Children = rvmStore.RvmFiles.SelectMany(f => f.Model.Children)
                .Select(root => CollectGeometryNodesRecursive(root, rootNode)).ToArray();

            rootNode.BoundingBoxAxisAligned =
                BoundingBoxEncapsulate(rootNode.Children.Select(x => x.BoundingBoxAxisAligned).WhereNotNull()
                    .ToArray());
            
            var rvmNodes = rvmStore.RvmFiles.SelectMany(x => x.Model.Children.SelectMany(GetAllNodesFlat)).ToArray();

            const ulong meshId = 0; // TODO: Unhardcode this.
            var meshes = ExportMeshesToFile(outputDirectory, rvmNodes);

            Console.WriteLine("TriangleMeshesCount: " + meshes[meshId].Count);

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null,
                "Root node has no bounding box. Are there any meshes in the input?");
            var boundingBox = rootNode.BoundingBoxAxisAligned!;

            var allNodes = GetAllNodesFlat(rootNode).ToArray();
            
            var geometries = allNodes.SelectMany(x => x.Geometries).ToList();
            
            // TODO: Unhack the triangleMeshes. They should be added in the Recursive process to avoid having separate tree-indexes.
            geometries.AddRange(meshes[meshId]);
            
            
            var attributeGrouping = geometries
                .SelectMany(g => g.GetType().GetProperties().Select(p => (g, p)))
                .Where(gp => gp.p.GetCustomAttributes(true).OfType<I3dfAttribute>().Any())
                .Select(gp => (gp.g, gp.p, gp.p.GetCustomAttributes(true).OfType<I3dfAttribute>().First()))
                .GroupBy(gpa => gpa.Item3.Type).ToArray();

            int[][] colors = Array.Empty<int[]>();
            float[] diagonals = Array.Empty<float>();
            float[] centerX = Array.Empty<float>();
            float[] centerY = Array.Empty<float>();
            float[] centerZ = Array.Empty<float>();
            float[][] normals = Array.Empty<float[]>();
            float[] deltas = Array.Empty<float>();
            float[] heights = Array.Empty<float>();
            float[] radii = Array.Empty<float>();
            float[] angles = Array.Empty<float>();
            float[] translationsX = Array.Empty<float>();
            float[] translationsY = Array.Empty<float>();
            float[] translationsZ = Array.Empty<float>();
            float[] scalesX = Array.Empty<float>();
            float[] scalesY = Array.Empty<float>();
            float[] scalesZ = Array.Empty<float>();
            ulong[] fileIds = Array.Empty<ulong>();
            TriangleMesh.Texture[] textures = Array.Empty<TriangleMesh.Texture>();

            Parallel.ForEach(attributeGrouping, gpas =>
                {
                    switch (gpas.Key)
                    {
                        case I3dfAttribute.AttributeType.Null:
                            break;
                        case I3dfAttribute.AttributeType.Color:
                            colors = gpas.Select(gpa => gpa.g.GetProperty<int[]>(gpa.p.Name)).WhereNotNull()
                                .Select(x => new Vector4(x[0], x[1], x[2], x[3])).Distinct()
                                .Select(x => new int[] {(byte)x.X, (byte)x.Y, (byte)x.Z, (byte)x.W}).ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Diagonal:
                            diagonals = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.CenterX:
                            centerX = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.CenterY:
                            centerY = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.CenterZ:
                            centerZ = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Normal:
                            normals = gpas.Select(gpa => gpa.g.GetProperty<float[]>(gpa.p.Name))
                                .WhereNotNull().Select(x => new Vector3(x[0], x[1], x[2])).Distinct()
                                .Select(y => y.CopyToNewArray()).ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Delta:
                            deltas = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Height:
                            heights = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Radius:
                            radii = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Angle:
                            angles = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.TranslationX:
                            translationsX = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct()
                                .ToArray();
                            break;
                        case I3dfAttribute.AttributeType.TranslationY:
                            translationsY = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct()
                                .ToArray();
                            break;
                        case I3dfAttribute.AttributeType.TranslationZ:
                            translationsZ = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct()
                                .ToArray();
                            break;
                        case I3dfAttribute.AttributeType.ScaleX:
                            scalesX = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.ScaleY:
                            scalesY = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.ScaleZ:
                            scalesZ = gpas.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.FileId:
                            fileIds = gpas.Select(gpa => gpa.g.GetProperty<ulong>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Texture:
                            textures = gpas.Select(gpa => gpa.g.GetProperty<TriangleMesh.Texture>(gpa.p.Name)).WhereNotNull().ToArray();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            );


            var primitiveCollections = new PrimitiveCollections();
            foreach (var geometriesByType in geometries.GroupBy(g => g.GetType()))
            {
                var elementType = geometriesByType.Key;
                var elements = geometriesByType.ToArray();
                var fieldInfo = primitiveCollections.GetType().GetFields().First(pc => pc.FieldType.GetElementType() == elementType);
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
                        BboxMax = boundingBox.Max.CopyToNewArray(),
                        BboxMin = boundingBox.Min.CopyToNewArray(),
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

            var i3dTimer = Stopwatch.StartNew();
            using var i3dSectorFile = File.Create(
                Path.Join(
                    outputDirectory.FullName, 
                    $"sector_{file.FileSector.Header.SectorId}.i3d"
                    ));
            I3dWriter.WriteSector(file.FileSector, i3dSectorFile);
            Console.WriteLine("Finished writing i3d Sectors in " + i3dTimer.Elapsed);


            var infoNodesTimer = Stopwatch.StartNew();
            var infoNodes = allNodes.Where(n => n.Group is RvmNode).Select(n =>
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
            Console.WriteLine("Done serializing infonodes in " + infoNodesTimer.Elapsed);

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
                                Min: new BbVector3(sectorFileHeader.BboxMin[0], sectorFileHeader.BboxMin[1], sectorFileHeader.BboxMin[2]),
                                Max: new BbVector3(sectorFileHeader.BboxMax[0], sectorFileHeader.BboxMax[1], sectorFileHeader.BboxMax[2])
                            ),
                        Depth = sectorFileHeader.ParentSectorId == null ? 1 : throw new NotImplementedException(),
                        Path = sectorFileHeader.SectorId == 0 ? "0/" : throw new NotImplementedException(),
                        IndexFile = new IndexFile(
                            FileName: "sector_" + sectorFileHeader.SectorId + ".i3d",
                            DownloadSize: 500001, // TODO: Find a real download size
                            PeripheralFiles: new[]
                            {
                                $"mesh_{meshId}.ctm"
                            }),
                        FacesFile = null, // Not implemented
                        EstimatedTriangleCount = meshes[meshId].Sum(x => (long)x.TriangleCount), // This is a guesstimate, not sure how to calculate non-mesh triangle count,
                        EstimatedDrawCallCount = 1337 // Not calculated
                    }
                }
            };

            var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
            JsonSerializeToFile(scene, scenePath, Formatting.Indented);

            Console.WriteLine($"Total primitives {geometries.Count}/{PrimitiveCounter.pc}");
            Console.WriteLine($"Missing: {PrimitiveCounter.ToString()}");

            Console.WriteLine(
                $"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        }

        private static Dictionary<ulong, IReadOnlyCollection<TriangleMesh>> ExportMeshesToFile(DirectoryInfo outputDirectory, IReadOnlyCollection<RvmNode> rvmNodes)
        {
            
            var rvmNodesWithFacetGroups = rvmNodes.ToDictionary(x => x, x => x.Children.OfType<RvmFacetGroup>());

            var tessellatedFacetGroups = rvmNodesWithFacetGroups
                .AsParallel()
                .AsOrdered()
                .Select(kvp =>
            {
                return kvp.Value.Select(facetGroup =>
                {
                    const float minimumDiagonalToExport = -10f;
                    const float tolerance = 0.1f;
                    bool shouldExport = facetGroup.CalculateAxisAlignedBoundingBox().Diagonal > minimumDiagonalToExport;
                    return shouldExport ? (kvp.Key, facetGroup, Mesh: TessellatorBridge.Tessellate(facetGroup, tolerance)) : ((RvmNode Key, RvmFacetGroup facetGroup, Mesh? Mesh)?) null;
                });
            }).SelectMany(x => x).WhereNotNull().Where(x => x.Mesh != null).AsSequential();
            

            var meshId = MeshIdGenerator.GetNextId();
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshId}.obj"));
            objExporter.StartObject($"root"); // Keep a single object in each file
            var triangleMeshes = new List<TriangleMesh>();

            foreach ((var rvmNode, var facetGroup, Mesh? mesh) in tessellatedFacetGroups)
            {
                if (mesh == null)
                    continue;

                var commonProps = facetGroup.GetCommonProps(rvmNode,
                    new CadRevealNode()
                    {
                        NodeId = NodeIdGenerator.GetNodeId(null), TreeIndex = TreeIndexGenerator.GetNextId()
                    });
                
                var triangleMesh = new TriangleMesh(
                    commonProps, meshId, (uint) mesh.Triangles.Length);
                objExporter.WriteMesh(mesh);

                triangleMeshes.Add(triangleMesh);
            }

            return new Dictionary<ulong, IReadOnlyCollection<TriangleMesh>>(){{meshId, triangleMeshes}};
        }

        private static void JsonSerializeToFile<T>(T obj, string filename, Formatting formatting = Formatting.None)
        {
            using var stream = File.Create(filename);
            using var writer = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(writer);
            var jsonSerializer = new JsonSerializer {Formatting = formatting};
            jsonSerializer.Serialize(jsonWriter, obj);
            jsonWriter.Flush();
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

        private static IEnumerable<RvmNode> GetAllNodesFlat(RvmNode root)
        {
            yield return root;

            foreach (RvmNode rvmNode in root.Children.OfType<RvmNode>())
            {
                foreach (RvmNode revealNode in GetAllNodesFlat(rvmNode))
                {
                    yield return revealNode;
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
            APrimitive[] geometries = Array.Empty<APrimitive>();

            if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
            {
                childrenCadNodes = root.Children.Select(child =>
                {
                    switch (child)
                    {
                        case RvmPrimitive rvmPrimitive:
                            return CollectGeometryNodesRecursive(new RvmNode(1, "Implicit geometry", root.Translation, root.MaterialId) { Children = {rvmPrimitive}}, newNode);
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
                geometries = root.Children.OfType<RvmPrimitive>()
                    .Select(x => APrimitive.FromRvmPrimitive(newNode, root, x)).Where(g => g != null).ToArray()!;
            }

            newNode.Geometries = geometries;
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