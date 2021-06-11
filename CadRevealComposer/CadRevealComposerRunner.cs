namespace CadRevealComposer
{
    using IdProviders;
    using Newtonsoft.Json;
    using Primitives;
    using RvmSharp.BatchUtils;
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;
    using System.Threading.Tasks;
    using Utils;

    public static class CadRevealComposerRunner
    {
        private const int I3DFMagicBytes = 1178874697; // I3DF chars as bytes.
        static readonly TreeIndexGenerator TreeIndexGenerator = new();
        static readonly NodeIdProvider NodeIdGenerator = new();

        // ReSharper disable once UnusedParameter.Local
        public static void Process(DirectoryInfo inputRvmFolderPath, DirectoryInfo outputDirectory)
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

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null,
                "Root node has no bounding box. Are there any meshes in the input?");
            var boundingBox = rootNode.BoundingBoxAxisAligned!;

            var allNodes = GetAllNodesFlat(rootNode).ToArray();

            var geometries = allNodes.SelectMany(x => x.Geometries).ToArray();

            var gpas = geometries
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

            Parallel.ForEach(gpas, gpa =>
                    //foreach (var gpa in gpas)
                {
                    switch (gpa.Key)
                    {
                        case I3dfAttribute.AttributeType.Null:
                            break;
                        case I3dfAttribute.AttributeType.Color:
                            colors = gpa.Select(gpa => gpa.g.GetProperty<int[]>(gpa.p.Name)).WhereNotNull()
                                .Select(x => new Vector4(x[0], x[1], x[2], x[3])).Distinct()
                                .Select(x => new int[] {(byte)x.X, (byte)x.Y, (byte)x.Z, (byte)x.W}).ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Diagonal:
                            diagonals = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.CenterX:
                            centerX = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.CenterY:
                            centerY = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.CenterZ:
                            centerZ = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Normal:
                            normals = gpa.Select(gpa => gpa.g.GetProperty<float[]>(gpa.p.Name))
                                .WhereNotNull().Select(x => new Vector3(x[0], x[1], x[2])).Distinct()
                                .Select(y => y.CopyToNewArray()).ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Delta:
                            deltas = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Height:
                            heights = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Radius:
                            radii = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Angle:
                            angles = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.TranslationX:
                            translationsX = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct()
                                .ToArray();
                            break;
                        case I3dfAttribute.AttributeType.TranslationY:
                            translationsY = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct()
                                .ToArray();
                            break;
                        case I3dfAttribute.AttributeType.TranslationZ:
                            translationsZ = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct()
                                .ToArray();
                            break;
                        case I3dfAttribute.AttributeType.ScaleX:
                            scalesX = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.ScaleY:
                            scalesY = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.ScaleZ:
                            scalesZ = gpa.Select(gpa => gpa.g.GetProperty<float>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.FileId:
                            fileIds = gpa.Select(gpa => gpa.g.GetProperty<ulong>(gpa.p.Name)).Distinct().ToArray();
                            break;
                        case I3dfAttribute.AttributeType.Texture:
                            // TODO
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            );

            
            // TODO texture

            

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
                            Texture = Array.Empty<object>()
                        }
                    },
                    PrimitiveCollections = primitiveCollections
                }
            };


            var outputFileName = Path.Combine(outputDirectory.FullName, "output.json");
            JsonSerializeToFile(file, outputFileName);


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

            var sectorFileHeader = file.FileSector.Header;
            var scene = new Scene()
            {
                Version = 8,
                ProjectId = 1337,
                ModelId = 13337,
                RevisionId = 31337,
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
                            DownloadSize: 500001,
                            PeripheralFiles: Array.Empty<string>()),
                        FacesFile = null, // Not implemented
                        EstimatedTriangleCount = 1337, // Not calculated,
                        EstimatedDrawCallCount = 1337 // Not calculated
                    }
                }
            };

            var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
            JsonSerializeToFile(scene, scenePath, Formatting.Indented);
            
            Console.WriteLine($"Total primitives {geometries.Length}/{PrimitiveCounter.pc}");
            Console.WriteLine($"Missing: {PrimitiveCounter.ToString()}");

            Console.WriteLine($"Wrote json files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
            // TODO: Nodes must be generated for implicit geometry like implicit pipes
            // BOX treeIndex, transform -> cadreveal, 

            // TODO: For each CadRevealNode -> Collect CadRevealGeometries -> 
            // TODO: Translate Rvm
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

        public static IEnumerable<CadRevealNode> GetAllNodesFlat(CadRevealNode root)
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