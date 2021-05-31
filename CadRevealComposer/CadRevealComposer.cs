namespace CadRevealComposer
{
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
    using Utils;

    public static class CadRevealComposer
    {
        static readonly TreeIndexGenerator TreeIndexGenerator = new();
        static readonly NodeIdProvider NodeIdGenerator = new();

        // ReSharper disable once UnusedParameter.Local
        public static void Process(string inputRvmFolderPath)
        {
            var workload = Workload.CollectWorkload(new[] {inputRvmFolderPath});


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

            Debug.Assert(rootNode.BoundingBoxAxisAligned != null, "Root node has no bounding box. Are there any meshes in the input?");
            var boundingBox = rootNode.BoundingBoxAxisAligned!;

            var allNodes = GetAllNodesFlat(rootNode).ToArray();

            var geometries = allNodes.SelectMany(x => x.Geometries).ToArray();

            var distinctDiagonals = geometries.CollectProperties<float, APrimitive>("Diagonal").Distinct();
            var distinctCenterX = geometries.CollectProperties<float, APrimitive>("CenterX").Distinct();
            var distinctCenterY = geometries.CollectProperties<float, APrimitive>("CenterY").Distinct();
            var distinctCenterZ = geometries.CollectProperties<float, APrimitive>("CenterZ").Distinct();


            var distinctNormals = geometries.CollectProperties<float[], APrimitive>("Normal", "CenterAxis")
                .Select(x => new Vector3(x[0], x[1], x[2])).Distinct().Select(y => y.CopyToNewArray());

            var distinctDelta = geometries.CollectProperties<float, APrimitive>("DeltaX", "DeltaY", "DeltaZ")
                .Distinct();

            var height = geometries.CollectProperties<float, APrimitive>("Height").Distinct();
            var radius = geometries.CollectProperties<float, APrimitive>("Radius", "TubeRadius").Distinct();
            var angle = geometries.CollectProperties<float, APrimitive>("RotationAngle", "ArcAngle").Distinct();

            var color = geometries.CollectProperties<int[], APrimitive>("Color")
                .Select(x => new Vector4(x[0], x[1], x[2], x[3])).Distinct()
                .Select(x => new int[] {(byte)x.X, (byte)x.Y, (byte)x.Z, (byte)x.W});

            var file = new FileI3D()
            {
                FileSector = new FileSector
                {
                    Header = new Header()
                    {
                        // Constants
                        MagicBytes = 1178874697,
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
                            ScaleX = new object[0],
                            ScaleY = new object[0],
                            ScaleZ = new object[0],
                            TranslationX = new object[0],
                            TranslationY = new object[0],
                            TranslationZ = new object[0],
                            Radius = radius.ToArray(),
                            FileId = new object[0],
                            Height = height.ToArray(),
                            Texture = new object[0]
                        }
                    },
                    PrimitiveCollections = new Dictionary<string, APrimitive[]>()
                    {
                        {"box_collection", geometries.OfType<Box>().OfType<APrimitive>().ToArray()},
                        {"circle_collection", new APrimitive[0]},
                        {"closed_cone_collection", new APrimitive[0]},
                        {
                            "closed_cylinder_collection",
                            geometries.OfType<ClosedCylinder>().OfType<APrimitive>().ToArray()
                        },
                        {"closed_eccentric_cone_collection", new APrimitive[0]},
                        {"closed_ellipsoid_segment_collection", new APrimitive[0]},
                        {"closed_extruded_ring_segment_collection", new APrimitive[0]},
                        {"closed_spherical_segment_collection", new APrimitive[0]},
                        {
                            "closed_torus_segment_collection",
                            geometries.OfType<ClosedTorusSegment>().OfType<APrimitive>().ToArray()
                        },
                        {"ellipsoid_collection", new APrimitive[0]},
                        {"extruded_ring_collection", new APrimitive[0]},
                        {"nut_collection", new APrimitive[0]},
                        {"open_cone_collection", new APrimitive[0]},
                        {
                            "open_cylinder_collection",
                            geometries.OfType<OpenCylinder>().OfType<APrimitive>().ToArray()
                        },
                        {"open_eccentric_cone_collection", new APrimitive[0]},
                        {"open_ellipsoid_segment_collection", new APrimitive[0]},
                        {"open_extruded_ring_segment_collection", new APrimitive[0]},
                        {"open_spherical_segment_collection", new APrimitive[0]},
                        {
                            "open_torus_segment_collection",
                            geometries.OfType<OpenTorusSegment>().OfType<APrimitive>().ToArray()
                        },
                        {"ring_collection", new APrimitive[0]},
                        {"sphere_collection", new APrimitive[0]},
                        {"torus_collection", geometries.OfType<Torus>().OfType<APrimitive>().ToArray()},
                        {"open_general_cylinder_collection", new APrimitive[0]},
                        {"closed_general_cylinder_collection", new APrimitive[0]},
                        {"solid_open_general_cylinder_collection", new APrimitive[0]},
                        {"solid_closed_general_cylinder_collection", new APrimitive[0]},
                        {"open_general_cone_collection", new APrimitive[0]},
                        {"closed_general_cone_collection", new APrimitive[0]},
                        {"solid_open_general_cone_collection", new APrimitive[0]},
                        {"solid_closed_general_cone_collection", new APrimitive[0]},
                        {"triangle_mesh_collection", new APrimitive[0]},
                        {"instanced_mesh_collection", new APrimitive[0]}
                    }
                }
            };


            string outputPath = "output.json";
            File.WriteAllText(outputPath, JsonConvert.SerializeObject(file));


            Console.WriteLine($"Wrote i3d file to \"{Path.GetFullPath(outputPath)}\"");
            // TODO: Nodes must be generated for implicit geometry like implicit pipes
            // BOX treeIndex, transform -> cadreveal, 

            // TODO: For each CadRevealNode -> Collect CadRevealGeometries -> 
            // TODO: Translate Rvm
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

            var childrenCadNodes = root.Children.OfType<RvmNode>()
                .Select(n => CollectGeometryNodesRecursive(n, newNode))
                .ToArray();

            if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
            {
                // TODO: Implicit Pipes
                // TODO: Keep Child order when implicit pipes.
            }

            var geometries = root.Children.OfType<RvmPrimitive>()
                .Select(x => APrimitive.FromRvmPrimitive(newNode, root, x)).Where(g => g != null);


            newNode.Geometries = geometries.ToArray()!;
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