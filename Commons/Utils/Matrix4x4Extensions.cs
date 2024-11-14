namespace Commons.Utils;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class Matrix4x4Extensions
{
    /// <summary>
    /// Check if the matrix is decomposable. Used to validate matrices for use in 3D. Ensures all data is finite and that its decomposable.
    ///
    /// This uses Matrix4x4.Decompose but discards the element output. Use <see cref="Matrix4x4.Decompose" /> if you need the output
    /// </summary>
    public static bool IsDecomposable(this Matrix4x4 m)
    {
        return m.AsEnumerableRowMajor().All(float.IsFinite) && Matrix4x4.Decompose(m, out _, out _, out _);
    }

    /// <summary>
    /// Decomposes the matrix into scale, rotation and translation if possible.
    /// Identical to <see cref="Matrix4x4"/>.<see cref="Matrix4x4.Decompose"/>
    /// </summary>
    /// <param name="m">This matrix</param>
    /// <returns>The Scale, Rotation and Translation components</returns>
    public static (Vector3 scale, Quaternion rotation, Vector3 translation)? TryDecompose(this Matrix4x4 m)
    {
        if (!m.AsEnumerableRowMajor().All(float.IsFinite))
            return null;
        var success = Matrix4x4.Decompose(m, out var scale, out var rotation, out var translation);
        if (!success)
            return null;
        return (scale, rotation, translation);
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
}
