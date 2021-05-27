namespace CadRevealComposer.Utils
{
    using System;
    using System.Numerics;

    public static class AlgebraUtils
    {
        public static (float roll, float pitch, float yaw) DecomposeQuaternion(Quaternion q)
        {
            var roll = MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z), 1f - 2f * (q.X * q.X + q.Y * q.Y));
            var pitch = MathF.Asin(2f * (q.W * q.Y - q.Z * q.X));
            var yaw = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y), 1f - 2f * (q.Y * q.Y + q.Z * q.Z));
            return (roll, pitch, yaw);
        }
    }
}