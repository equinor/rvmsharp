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
            var distinctDiagonals = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.Diagonal).Distinct();
            var distinctCenterX = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.CenterX).Distinct();
            var distinctCenterY = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.CenterY).Distinct();
            var distinctCenterZ = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.CenterZ).Distinct();
            var distinctNormals = geometries.CollectProperties<float[]>(I3dfAttribute.AttributeType.Normal)
                .WhereNotNull().Select(x => new Vector3(x[0], x[1], x[2])).Distinct().Select(y => y.CopyToNewArray());
            var distinctDelta = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.Delta)
                .Distinct();
            var height = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.Height).Distinct();
            var radius = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.Radius).Distinct();
            var angle = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.Angle).Distinct();
            var translationX = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.TranslationX).Distinct();
            var translationY = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.TranslationY).Distinct();
            var translationZ = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.TranslationZ).Distinct();
            var scaleX = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.ScaleX).Distinct();
            var scaleY = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.ScaleY).Distinct();
            var scaleZ = geometries.CollectProperties<float>(I3dfAttribute.AttributeType.ScaleZ).Distinct();
            var fileId = geometries.CollectProperties<ulong>(I3dfAttribute.AttributeType.FileId).Distinct();

            var gpas = geometries
                .SelectMany(g => g.GetType().GetProperties()
                    .Where(p => p.GetCustomAttributes(true).OfType<I3dfAttribute>().Any())
                    .Select(p => (g, p, p.GetCustomAttributes(true).OfType<I3dfAttribute>().First())).ToArray())
                .GroupBy(gpa => gpa.Item3.Type);
            foreach (var gpa in gpas)
            {
                switch (gpa.Key)
                {
                    case I3dfAttribute.AttributeType.Null:
                        break;
                    case I3dfAttribute.AttributeType.Color:
                        break;
                    case I3dfAttribute.AttributeType.Diagonal:
                        break;
                    case I3dfAttribute.AttributeType.CenterX:
                        break;
                    case I3dfAttribute.AttributeType.CenterY:
                        break;
                    case I3dfAttribute.AttributeType.CenterZ:
                        break;
                    case I3dfAttribute.AttributeType.Normal:
                        break;
                    case I3dfAttribute.AttributeType.Delta:
                        break;
                    case I3dfAttribute.AttributeType.Height:
                        break;
                    case I3dfAttribute.AttributeType.Radius:
                        break;
                    case I3dfAttribute.AttributeType.Angle:
                        break;
                    case I3dfAttribute.AttributeType.TranslationX:
                        break;
                    case I3dfAttribute.AttributeType.TranslationY:
                        break;
                    case I3dfAttribute.AttributeType.TranslationZ:
                        break;
                    case I3dfAttribute.AttributeType.ScaleX:
                        break;
                    case I3dfAttribute.AttributeType.ScaleY:
                        break;
                    case I3dfAttribute.AttributeType.ScaleZ:
                        break;
                    case I3dfAttribute.AttributeType.FileId:
                        break;
                    case I3dfAttribute.AttributeType.Texture:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            
            // TODO texture

            var color = geometries.CollectProperties<int[]>(I3dfAttribute.AttributeType.Color)
                .WhereNotNull()
                .Select(x => new Vector4(x[0], x[1], x[2], x[3])).Distinct()
                .Select(x => new int[] {(byte)x.X, (byte)x.Y, (byte)x.Z, (byte)x.W});

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
                            Angle = angle.ToArray(),
                            CenterX = distinctCenterX.ToArray(),
                            CenterY = distinctCenterY.ToArray(),
                            CenterZ = distinctCenterZ.ToArray(),
                            Color = color.ToArray(),
                            Normal = distinctNormals.ToArray(),
                            Delta = distinctDelta.ToArray(),
                            Diagonal = distinctDiagonals.ToArray(),
                            ScaleX = scaleX.ToArray(),
                            ScaleY = scaleY.ToArray(),
                            ScaleZ = scaleZ.ToArray(),
                            TranslationX = translationX.ToArray(),
                            TranslationY = translationY.ToArray(),
                            TranslationZ = translationZ.ToArray(),
                            Radius = radius.ToArray(),
                            FileId = fileId.ToArray(),
                            Height = height.ToArray(),
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