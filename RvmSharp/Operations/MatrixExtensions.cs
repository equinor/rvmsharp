namespace RvmSharp.Operations
{
    using System.Numerics;

    // ReSharper disable once InconsistentNaming
    public static class Matrix4x4Helpers
    {
        public static Matrix4x4 CalculateTransformMatrix(Vector3 pos, Quaternion rot, Vector3 scale)
        {
            return Matrix4x4.CreateScale(scale)
                   * Matrix4x4.CreateFromQuaternion(rot)
                   * Matrix4x4.CreateTranslation(pos);
        }
    }
}