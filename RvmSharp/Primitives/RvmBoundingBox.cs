namespace RvmSharp.Primitives
{
    using System;
    using System.Linq;
    using System.Numerics;

    public record RvmBoundingBox (Vector3 Min, Vector3 Max)
    {
        /// <summary>
        /// Generate all 8 corners of the bounding box.
        /// Remark: This can be "Flat" (Zero width) in one or more dimensions.
        /// </summary>
        public Vector3[] GenerateBoxVertexes()
        {
            var cube = new[]
            {
                Max, 
                Min,
                new Vector3(Min.X, Min.Y, Max.Z),
                new Vector3(Min.X, Max.Y, Min.Z),
                new Vector3(Max.X, Min.Y, Min.Z),
                new Vector3(Max.X, Max.Y, Min.Z),
                new Vector3(Max.X, Min.Y, Max.Z),
                new Vector3(Min.X, Max.Y, Max.Z)
            };

            return cube;
        }

        /// <summary>
        /// Calculate the diagonal size (distance between "min" and "max")
        /// </summary>
        public float Diagonal => Vector3.Distance(Min, Max);
    };
}