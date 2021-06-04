namespace CadRevealComposer.Utils
{
    using System;
    using System.Numerics;

    public static class VectorExtensions
    {
        /// <summary>
        /// Copy the items to a new array of 3 items in XYZ order.
        /// </summary>
        public static float[] CopyToNewArray(this Vector3 vector3)
        {
            var floats = new float[3];
            vector3.CopyTo(floats);
            return floats;
        }

        /// <summary>
        /// Copy the items to an Array of 4 items in XYZW order.
        /// </summary>
        public static float[] CopyToNewArray(this Vector4 vector4)
        {
            var floats = new float[4];
            vector4.CopyTo(floats);
            return floats;
        }

        /// <summary>
        /// Check if X == Y == Z, within a given tolerance.
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="tolerance">Tolerance. For example 0.0001</param>
        /// <returns></returns>
        public static bool IsUniform(this Vector3 vector, float tolerance = 0.00001f)
        {
            return Math.Abs(vector.X - vector.Y) < tolerance && Math.Abs(vector.X - vector.Z) < tolerance;
        }
    }
}
