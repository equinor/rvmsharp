namespace CadRevealComposer
{
    using Newtonsoft.Json;
    using Primitives;
    using RvmSharp.Primitives;
    using System;
    using System.Linq;
    using System.Numerics;

    public class Box : APrimitive
    {
        public static Box FromRvmPrimitive(CadRevealNode revealNode, RvmNode container, RvmBox rvmBox)
        {
            if (!Matrix4x4.Decompose(rvmBox.Matrix, out var scale, out var rot, out var pos))
            {
                throw new Exception("Failed to decompose matrix." + rvmBox.Matrix);
            }

            float diagonal = CalculateDiagonal(rvmBox.BoundingBoxLocal, scale, rot);

            var unitBoxScale = Vector3.Multiply(scale, new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

            var colors = PdmsColors.GetColorAsBytesByCode(container.MaterialId < 50 ? container.MaterialId : 1).Select(x => (int)x).ToArray();

            // TODO: Verify that this gives expected diagonal for scaled sizes.

            var normal = Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, rot));
            // var r = rot * Quaternion.Inverse(Quaternion.A)

            var q = rot;

            var phi = MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z), 1f - 2f * (q.X * q.X + q.Y * q.Y));
            var theta = MathF.Asin(2f * (q.W * q.Y - q.Z * q.X));
            var psi = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));

            var rotAngle = psi; ;

            Box box = new Box()
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

            return box;
        }

        [JsonProperty]
        public string Type => nameof(Box);

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