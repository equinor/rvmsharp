namespace RvmSharp.Primitives
{
    using System;
    using System.Linq;
    using System.Numerics;

    public record RvmBoundingBox (Vector3 Min, Vector3 Max)
    {
        /// <summary>
        /// Generate all 8 corners of the bounding box.
        /// </summary>
        public Vector3[] GenerateBoxVertexes()
        {
            // make longer to distinguish form from "min"
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

            // Some objects are flat (Zero in multiple dimensions
            // if (cube.Distinct().Count() != 8)
            //     throw new ArgumentException("A cube should have exactly 8 unique corners"); // This is unexpected.
            
            return cube;
        }
    };
}