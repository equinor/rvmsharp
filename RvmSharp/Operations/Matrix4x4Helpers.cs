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
}