namespace CadRevealComposer.Primitives
{
    using Newtonsoft.Json;
    using RvmSharp.Primitives;
    using System;
    using System.Linq;
    using System.Numerics;

    public abstract class APrimitive
    {
        [JsonProperty("node_id")]
        public ulong NodeId { get; set; }

        [JsonProperty("tree_index")]
        public ulong TreeIndex { get; set; }
        
        protected static float CalculateDiagonal(RvmBoundingBox boundingBoxLocal, Vector3 scale, Quaternion rot)
        {
            var halfBox = Vector3.Multiply(scale, (boundingBoxLocal.Max - boundingBoxLocal.Min)) / 2;
            var boxVertice = Vector3.Abs(Vector3.Transform(halfBox, rot)) * 2;
            var diagonal = MathF.Sqrt(boxVertice.X * boxVertice.X + boxVertice.Y * boxVertice.Y + boxVertice.Z * boxVertice.Z);
            return diagonal;
        }

        protected static int[] GetColor(RvmNode container)
        {
            return PdmsColors.GetColorAsBytesByCode(container.MaterialId < 50 ? container.MaterialId : 1).Select(x => (int)x).ToArray();
        }
    }
}