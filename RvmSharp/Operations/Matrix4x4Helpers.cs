namespace RvmSharp.Operations;

using System.Numerics;
using System.Runtime.CompilerServices;

// ReSharper disable once InconsistentNaming
public static class Matrix4x4Helpers
{
    /// <summary>
    /// Calculate a transform matrix based on position, rotation and scale.
    /// Often named a TRS matrix (translation, rotation, scale).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CalculateTransformMatrix(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);
    }

    /// <summary>
    /// Checks if all values of the matrix are finite. Infinite makes no sense and breaks later code.
    /// This guard exists due to a issue on Oseberg C which contained 3 nodes with infinite values.
    /// </summary>
    public static bool MatrixContainsInfiniteValue(Matrix4x4 matrix)
    {
        return !float.IsFinite(matrix.M11)
            || !float.IsFinite(matrix.M12)
            || !float.IsFinite(matrix.M13)
            || !float.IsFinite(matrix.M14)
            || !float.IsFinite(matrix.M21)
            || !float.IsFinite(matrix.M22)
            || !float.IsFinite(matrix.M23)
            || !float.IsFinite(matrix.M24)
            || !float.IsFinite(matrix.M31)
            || !float.IsFinite(matrix.M32)
            || !float.IsFinite(matrix.M33)
            || !float.IsFinite(matrix.M34)
            || !float.IsFinite(matrix.M41)
            || !float.IsFinite(matrix.M42)
            || !float.IsFinite(matrix.M43)
            || !float.IsFinite(matrix.M44);
    }
}
