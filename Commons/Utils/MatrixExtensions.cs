namespace Commons.Utils;

using System;
using System.Collections.Generic;
using System.Numerics;

public static class MatrixExtensions
{
    /// <summary>
    /// Check if the matrix is decomposable. Used to validate matrices for use in 3D. Ensures all data is finite and that its decomposable.
    ///
    /// This uses Matrix4x4.Decompose but discards the element output. Use <see cref="Matrix4x4.Decompose" /> if you need the output
    /// </summary>
    public static bool IsDecomposable(this Matrix4x4 m)
    {
        return m.All(float.IsFinite) && Matrix4x4.Decompose(m, out _, out _, out _);
    }

    /// <summary>
    /// Returns all the scalars in the matrix. 1 row at a time.
    /// </summary>
    public static IEnumerable<float> AsEnumerableRowMajor(this Matrix4x4 m)
    {
        yield return m.M11;
        yield return m.M12;
        yield return m.M13;
        yield return m.M14;
        yield return m.M21;
        yield return m.M22;
        yield return m.M23;
        yield return m.M24;
        yield return m.M31;
        yield return m.M32;
        yield return m.M33;
        yield return m.M34;
        yield return m.M41;
        yield return m.M42;
        yield return m.M43;
        yield return m.M44;
    }

    /// <summary>
    /// Returns true if all elements in matrix satisfy the predicate.
    /// </summary>
    private static bool All(this Matrix4x4 m, Func<float, bool> predicate)
    {
        return predicate(m.M11)
            && predicate(m.M12)
            && predicate(m.M13)
            && predicate(m.M14)
            && predicate(m.M21)
            && predicate(m.M22)
            && predicate(m.M23)
            && predicate(m.M24)
            && predicate(m.M31)
            && predicate(m.M32)
            && predicate(m.M33)
            && predicate(m.M34)
            && predicate(m.M41)
            && predicate(m.M42)
            && predicate(m.M43)
            && predicate(m.M44);
    }
}
