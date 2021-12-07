﻿namespace CadRevealComposer.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public static class VectorExtensions
    {
        /// <summary>
        /// Copy the items to a new array of 3 items in XYZ order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] CopyToNewArray(this Vector3 vector3)
        {
            return new[] { vector3.X, vector3.Y, vector3.Z };
        }

        /// <summary>
        /// Yield return the 3 items in XYZ order.
        /// </summary>
        public static IEnumerable<float> AsEnumerable(this Vector3 v)
        {
            yield return v.X;
            yield return v.Y;
            yield return v.Z;
        }

        /// <summary>
        /// Copy the items to an Array of 4 items in XYZW order.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float[] CopyToNewArray(this Vector4 vector4)
        {
            return new[] {vector4.X, vector4.Y, vector4.Z, vector4.W};
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ApproximatelyEquals(this Vector3 vector, Vector3 other, float tolerance = 0.00001f)
        {
            return Math.Abs(vector.X - other.X) < tolerance
                && Math.Abs(vector.Y - other.Y) < tolerance
                && Math.Abs(vector.Z - other.Z) < tolerance;
        }
    }
}