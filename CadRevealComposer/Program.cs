using System;

namespace CadRevealComposer
{
    using System.Collections.Generic;
    using System.IO;
    using RvmSharp.BatchUtils;
    using RvmSharp.Primitives;
    using System.Linq;
    using System.Numerics;
    using Newtonsoft.Json;

    public static class Program
    {
        static readonly TreeIndexGenerator TreeIndexGenerator = new TreeIndexGenerator();
        static readonly NodeIdProvider NodeIdGenerator = new NodeIdProvider();

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            var workload = Workload.CollectWorkload(new[] { @"C:\Users\nhals\GitRepos\rvmsharp\TestData\HDA_RVM - Copy\" });
            var progressReport = new Progress<(string fileName, int progress, int total)>((x) =>
            {
                Console.WriteLine(x.fileName);
            });

            var rvmStore = Workload.ReadRvmData(workload, progressReport);
            // Project name og project parameters tull from Cad Control Center
            var rootNode =
                new CadRevealNode
                {
                    NodeId = NodeIdGenerator.GetNodeId(null),
                    TreeIndex = TreeIndexGenerator.GetNextId(),
                    Parent = null,
                    Group = null,
                    Children = null
                };

            rootNode.Children = rvmStore.RvmFiles.SelectMany(f => f.Model.Children)
                .Select(root => CollectGeometryNodesRecursive(root, rootNode)).ToArray();

            var allNodes = GetAllNodesFlat(rootNode).ToArray();

            var boxes = allNodes.SelectMany(x => x.Geometries ?? new ACadRevealGeometry[0]).OfType<BoxACadRevealGeometry>().ToArray();

            var distinctDiagonals = boxes.Select(x => x.Diagonal).Distinct();
            var distinctCenterX = boxes.Select(x => x.CenterX).Distinct();
            var distinctCenterY = boxes.Select(x => x.CenterY).Distinct();
            var distinctCenterZ = boxes.Select(x => x.CenterZ).Distinct();
            var distinctNormals = boxes.Select(x => new Vector3(x.Normal[0], x.Normal[1], x.Normal[2])).Distinct().Select(y => new[] { y.X, y.Y, y.Z });
            var distinctDelta = boxes.SelectMany(x => new[] { x.DeltaX, x.DeltaY, x.DeltaZ }).Distinct();
            var height = new object[0];
            var radius = new object[0];
            var angle = boxes.Select(x => x.RotationAngle).Distinct();
            var color = boxes.Select(x => x.Color).Select(c => new Vector4(c[0], c[1], c[2], c[3])).Distinct()
                .Select(x => new int[] { (byte) x.X, (byte)x.Y, (byte)x.Z, (byte)x.W });


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
                        BboxMax = new[] {1000, 1000, 1000.0},
                        BboxMin = new[] {0, 0, 0.0},
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
                            Radius = new object[0],
                            FileId = new object[0],
                            Height = new object[0],
                            Texture = new object[0]
                        }
                    },
                    PrimitiveCollections = new Dictionary<string, ACadRevealGeometry[]>()
                    {
                        {"box_collection", boxes.OfType<ACadRevealGeometry>().ToArray()},
                        {"circle_collection", new ACadRevealGeometry[0]},
                        {"closed_cone_collection", new ACadRevealGeometry[0]},
                        {"closed_cylinder_collection", new ACadRevealGeometry[0]},
                        {"closed_eccentric_cone_collection", new ACadRevealGeometry[0]},
                        {"closed_ellipsoid_segment_collection", new ACadRevealGeometry[0]},
                        {"closed_extruded_ring_segment_collection", new ACadRevealGeometry[0]},
                        {"closed_spherical_segment_collection", new ACadRevealGeometry[0]},
                        {"closed_torus_segment_collection", new ACadRevealGeometry[0]},
                        {"ellipsoid_collection", new ACadRevealGeometry[0]},
                        {"extruded_ring_collection", new ACadRevealGeometry[0]},
                        {"nut_collection", new ACadRevealGeometry[0]},
                        {"open_cone_collection", new ACadRevealGeometry[0]},
                        {"open_cylinder_collection", new ACadRevealGeometry[0]},
                        {"open_eccentric_cone_collection", new ACadRevealGeometry[0]},
                        {"open_ellipsoid_segment_collection", new ACadRevealGeometry[0]},
                        {"open_extruded_ring_segment_collection", new ACadRevealGeometry[0]},
                        {"open_spherical_segment_collection", new ACadRevealGeometry[0]},
                        {"open_torus_segment_collection", new ACadRevealGeometry[0]},
                        {"ring_collection", new ACadRevealGeometry[0]},
                        {"sphere_collection", new ACadRevealGeometry[0]},
                        {"torus_collection", new ACadRevealGeometry[0]},
                        {"open_general_cylinder_collection", new ACadRevealGeometry[0]},
                        {"closed_general_cylinder_collection", new ACadRevealGeometry[0]},
                        {"solid_open_general_cylinder_collection", new ACadRevealGeometry[0]},
                        {"solid_closed_general_cylinder_collection", new ACadRevealGeometry[0]},
                        {"open_general_cone_collection", new ACadRevealGeometry[0]},
                        {"closed_general_cone_collection", new ACadRevealGeometry[0]},
                        {"solid_open_general_cone_collection", new ACadRevealGeometry[0]},
                        {"solid_closed_general_cone_collection", new ACadRevealGeometry[0]},
                        {"triangle_mesh_collection", new ACadRevealGeometry[0]},
                        {"instanced_mesh_collection", new ACadRevealGeometry[0]}
                    }
                }
            };


            File.WriteAllText("output.json", JsonConvert.SerializeObject(file));


            // TODO: Nodes must be generated for implicit geometry like implicit pipes
            // BOX treeIndex, transform -> cadreveal, 

            // TODO: For each CadRevealNode -> Collect CadRevealGeometries -> 
            // TODO: Translate Rvm

            Console.WriteLine("Hello World!!");
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
            var node = new CadRevealNode
            {
                NodeId = NodeIdGenerator.GetNodeId(null),
                TreeIndex = TreeIndexGenerator.GetNextId(),
                Group = root,
                Parent = parent,
                Children = null
            };

            var childrenCadNodes = root.Children.OfType<RvmNode>().Select(n => CollectGeometryNodesRecursive(n, node)).ToArray();
            if (root.Children.OfType<RvmPrimitive>().Any() && root.Children.OfType<RvmNode>().Any())
            {
                // TODO: Implicit Pipes
                // TODO: Keep Child order when implicit pipes.
            }

            var geometries = new List<ACadRevealGeometry>();
            var boxes = root.Children.OfType<RvmBox>().Select(x => BoxACadRevealGeometry.FromPrimitive(node, root, x));
            // TODO: I think the order is important, process all children in correct order.
            geometries.AddRange(boxes);

            node.Geometries = geometries.ToArray();

            node.Children = childrenCadNodes;
            return node;
        }
    }
}