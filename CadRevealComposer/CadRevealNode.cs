namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using RvmSharp.Containers;
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    public class CadRevealNode
    {
        public ulong NodeId;
        public ulong TreeIndex;
        // TODO support Store, Model, File and maybe not RVM
        public RvmGroup? Group; // PDMS inside, children inside
        public CadRevealNode? Parent;
        public CadRevealNode[]? Children;

        public ACadRevealGeometry[] Geometries;
        // Bounding box
        // Depth
        // Subtree size
    }


    public partial class FileI3D
    {
        [JsonProperty("FileSector")]
        public FileSector FileSector { get; set; }
    }

    public partial class FileSector
    {
        [JsonProperty("header")]
        public Header Header { get; set; }

        [JsonProperty("primitive_collections")]
        public Dictionary<string, ACadRevealGeometry[]> PrimitiveCollections { get; set; } = new Dictionary<string, ACadRevealGeometry[]>();
    }

    public partial class Header
    {
        [JsonProperty("magic_bytes")]
        public long MagicBytes { get; set; }

        [JsonProperty("format_version")]
        public long FormatVersion { get; set; }

        [JsonProperty("optimizer_version")]
        public long OptimizerVersion { get; set; }

        [JsonProperty("sector_id")]
        public long SectorId { get; set; }

        [JsonProperty("parent_sector_id")]
        public long? ParentSectorId { get; set; }

        [JsonProperty("bbox_min")]
        public double[] BboxMin { get; set; }

        [JsonProperty("bbox_max")]
        public double[] BboxMax { get; set; }

        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }
    }

    public partial class Attributes
    {
        [JsonProperty("color")]
        public int[][] Color { get; set; }

        [JsonProperty("diagonal")]
        public float[] Diagonal { get; set; }

        [JsonProperty("center_x")]
        public float[] CenterX { get; set; }

        [JsonProperty("center_y")]
        public float[] CenterY { get; set; }

        [JsonProperty("center_z")]
        public float[] CenterZ { get; set; }

        [JsonProperty("normal")]
        public float[][] Normal { get; set; }

        [JsonProperty("delta")]
        public float[] Delta { get; set; }

        [JsonProperty("height")]
        public object[] Height { get; set; }

        [JsonProperty("radius")]
        public object[] Radius { get; set; }

        [JsonProperty("angle")]
        public float[] Angle { get; set; }

        [JsonProperty("translation_x")]
        public object[] TranslationX { get; set; }

        [JsonProperty("translation_y")]
        public object[] TranslationY { get; set; }

        [JsonProperty("translation_z")]
        public object[] TranslationZ { get; set; }

        [JsonProperty("scale_x")]
        public object[] ScaleX { get; set; }

        [JsonProperty("scale_y")]
        public object[] ScaleY { get; set; }

        [JsonProperty("scale_z")]
        public object[] ScaleZ { get; set; }

        [JsonProperty("file_id")]
        public object[] FileId { get; set; }

        [JsonProperty("texture")]
        public object[] Texture { get; set; }
    }

    public abstract class ACadRevealGeometry
    {

    }

    public class BoxACadRevealGeometry : ACadRevealGeometry
    {
        public static BoxACadRevealGeometry FromPrimitive(CadRevealNode revealNode, RvmNode container, RvmBox rvmBox)
        {
            if (!Matrix4x4.Decompose(rvmBox.Matrix, out var scale, out var rot, out var pos))
            {
                throw new Exception("Failed to decompose matrix." + rvmBox.Matrix);
            }

            float diagonal = CalculateDiagonal(rvmBox.BoundingBoxLocal, scale, rot);

            var unitBoxScale = Vector3.Multiply(scale, new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

            //Console.WriteLine(container.MaterialId);
            var colors = PdmsColors.GetColorAsBytesByCode(container.MaterialId < 50 ? container.MaterialId : 1).Select(x => (int)x).ToArray();

            // TODO: Verify that this gives expected diagonal for scaled sizes.

            var normal = Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, rot));
            // var r = rot * Quaternion.Inverse(Quaternion.A)

            var q = rot;

            var phi = MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z), 1f - 2f * (q.X * q.X + q.Y * q.Y));
            var theta = MathF.Asin(2f * (q.W * q.Y - q.Z * q.X));
            var psi = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));

            var rotAngle = psi; ;

            BoxACadRevealGeometry boxACadRevealGeometry = new BoxACadRevealGeometry()
            {
                CenterX = pos.X,
                CenterY = pos.Y,
                CenterZ = pos.Z,
                DeltaX = unitBoxScale.X,
                DeltaY = unitBoxScale.Y,
                DeltaZ = unitBoxScale.Z,
                Normal = new[] { normal.X, normal.Y, normal.Z },
                RotationAngle = rotAngle,
                Color = colors,
                Diagonal = diagonal,
                NodeId = revealNode.NodeId,
                TreeIndex = revealNode.TreeIndex
            };

            return boxACadRevealGeometry;
        }

        private static float CalculateDiagonal(RvmBoundingBox boundingBoxLocal, Vector3 scale, Quaternion rot)
        {
            var halfBox = Vector3.Multiply(scale, (boundingBoxLocal.Max - boundingBoxLocal.Min)) / 2;
            var boxVertice = Vector3.Abs(Vector3.Transform(halfBox, rot)) * 2;
            var diagonal = MathF.Sqrt(boxVertice.X * boxVertice.X + boxVertice.Y * boxVertice.Y + boxVertice.Z * boxVertice.Z);
            return diagonal;
        }

        [JsonProperty]
        public string Type => nameof(BoxACadRevealGeometry);

        public const string Key = "box_collection";

        [JsonProperty("node_id")]
        public ulong NodeId { get; set; }

        [JsonProperty("tree_index")]
        public ulong TreeIndex { get; set; }

        [JsonProperty("color")]
        public int[] Color { get; set; }

        [JsonProperty("diagonal")]
        public float Diagonal { get; set; }

        [JsonProperty("center_x")]
        public float CenterX { get; set; }

        [JsonProperty("center_y")]
        public float CenterY { get; set; }

        [JsonProperty("center_z")]
        public float CenterZ { get; set; }

        [JsonProperty("normal")]
        public float[] Normal { get; set; }

        [JsonProperty("delta_x")]
        public float DeltaX { get; set; }

        [JsonProperty("delta_y")]
        public float DeltaY { get; set; }

        [JsonProperty("delta_z")]
        public float DeltaZ { get; set; }

        [JsonProperty("rotation_angle")]
        public float RotationAngle { get; set; }
    }
}
