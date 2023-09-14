﻿namespace CadRevealComposer.Utils;

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
        return new[] { vector4.X, vector4.Y, vector4.Z, vector4.W };
    }

    /// <summary>
    /// Check if X == Y == Z, within a given tolerance.
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="tolerance">Tolerance. For example 0.0001</param>
    /// <returns></returns>
    public static bool IsUniform(this Vector3 vector, float tolerance = 0.000_01f)
    {
        return Math.Abs(vector.X - vector.Y) < tolerance && Math.Abs(vector.X - vector.Z) < tolerance;
    }

    /// <summary>
    /// Checks that each vector component from both vectors are within the given tolerance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsWithinTolerance(this Vector3 vector, Vector3 other, float tolerance)
    {
        return Math.Abs(vector.X - other.X) < tolerance
            && Math.Abs(vector.Y - other.Y) < tolerance
            && Math.Abs(vector.Z - other.Z) < tolerance;
    }

    /// <summary>
    /// An equals function that aligns components within a "Grid" cell.
    /// That means that you cannot have an equality list of many nearly equal Vec3s in a row,
    /// since when one leaves the "grid cell" its in its own cell.
    /// </summary>
    /// <param name="v">this</param>
    /// <param name="other">Other</param>
    /// <param name="precisionDigits">The number of fractional digits to keep</param>
    /// <returns>True if equal given the precision</returns>
    public static bool EqualsWithinGridTolerance(this Vector3 v, Vector3 other, int precisionDigits)
    {
        return v.RoundInPlace(precisionDigits).Equals(other.RoundInPlace(precisionDigits));
    }

    /// <summary>
    /// Rounds XYZ elements of the vector to specified number of fractional digits, and rounds midpoint values to the nearest even number.
    /// </summary>
    /// <param name="v">The vertex to be rounded.</param>
    /// <param name="precisionDigits">The number of fractional digits to keep</param>
    /// <returns>The same Vertex</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref Vector3 RoundInPlace(this ref Vector3 v, int precisionDigits)
    {
        v.X = MathF.Round(v.X, precisionDigits);
        v.Y = MathF.Round(v.Y, precisionDigits);
        v.Z = MathF.Round(v.Z, precisionDigits);
        return ref v;
    }

    /// <summary><inheritdoc cref="RoundInPlace"/>
    /// Returns a new Vector3, and does not modify the input
    /// </summary>
    /// <param name="v">The vertex to be rounded.</param>
    /// <param name="precisionDigits">The number of fractional digits to keep</param>
    /// <returns>A new vertex</returns>
    public static Vector3 Round(this Vector3 v, int precisionDigits)
    {
        return v.RoundInPlace(precisionDigits);
    }

    /// <summary>
    /// Checks that each vector component from both vectors are within the given factor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsWithinFactor(this Vector3 vector, Vector3 other, float factor)
    {
        var upperTolerance = 1.0f + factor;
        var lowerTolerance = 1.0f - factor;
        var divided = vector / other;

        // with protection against zero cases, then compare using a small tolerance of epsilon
        // - 0f/0f = NaN
        // - epsilon/0f = Infinity
        const float tolerance = 1E-10f;
        return (
                float.IsNaN(divided.X) || float.IsInfinity(divided.X)
                    ? Math.Abs(vector.X - other.X) < tolerance
                    : (divided.X >= lowerTolerance && divided.X <= upperTolerance)
            )
            && (
                float.IsNaN(divided.Y) || float.IsInfinity(divided.Y)
                    ? Math.Abs(vector.Y - other.Y) < tolerance
                    : (divided.Y >= lowerTolerance && divided.Y <= upperTolerance)
            )
            && (
                float.IsNaN(divided.Z) || float.IsInfinity(divided.Z)
                    ? Math.Abs(vector.Z - other.Z) < tolerance
                    : (divided.Z >= lowerTolerance && divided.Z <= upperTolerance)
            );
    }
}
