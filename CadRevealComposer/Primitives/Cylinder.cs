namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using RvmSharp.Primitives;
    using System;
    using System.Numerics;

    public class Cylinder : APrimitive
    {
        public static Cylinder FromRvmPrimitive(CadRevealNode revealNode, RvmNode container, RvmCylinder rvmCylinder)
        {
            if (!Matrix4x4.Decompose(rvmCylinder.Matrix, out var scale, out var rot, out var pos))
            {
                throw new Exception("Failed to decompose matrix." + rvmCylinder.Matrix);
            }

            float diagonal = CalculateDiagonal(rvmCylinder.BoundingBoxLocal, scale, rot);
            var colors = GetColor(container);
            var normal = Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, rot));

            var height = rvmCylinder.Height * scale.Z;
            
            // FIXME: if scale is not uniform on X,Y, we should create something else
            var radius = rvmCylinder.Radius * scale.X;

            if (scale.X != scale.Y)
            {
                //throw new Exception("Not implemented!");
            }

            return new Cylinder()
            {
                NodeId = revealNode.NodeId,
                TreeIndex = revealNode.TreeIndex,
                Color = colors,
                Diagonal = diagonal,
                CenterX = pos.X,
                CenterY = pos.Y,
                CenterZ = pos.Z,
                CenterAxis = new []{normal.X, normal.Y, normal.Z},
                Height = height,
                Radius = radius
            };
        }

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

        [JsonProperty("center_axis")]
        public float[] CenterAxis { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }
    }
}