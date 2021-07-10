namespace CadRevealComposer.Utils
{
    using System;
    using System.Numerics;

    public static class AlgebraUtils
    {
        public static (Vector3 normal, float rotationAngle) DecomposeQuaternion(this Quaternion rot)
        {
            var normal = Vector3.Normalize(Vector3.Transform(Vector3.UnitZ, rot));

            var axes = Vector3.Cross(Vector3.UnitZ, normal);
            // 0.001f should account for less than a 0.1 degree error
            if (axes.Length() < 0.001f) // Cross product of parallel vectors is 0-vector
            {
                // in case where the primitive is kept upright, or upside-down
                // initial x-axes and transformed x-axes will be in the x,y plane
                // just calculate the angle between and take transformed z-direction
                // into considerations
                var x2 = Vector3.Transform(Vector3.UnitX, rot);
                var a = MathF.Atan2(x2.Y, x2.X);
                var sameDirection = Vector3.Distance(Vector3.UnitZ, normal) < 0.1f;
                var rotationAngle = sameDirection ? a : -a;
                return (normal, rotationAngle);
            }
            else
            {
                // rot - combined rotation
                // rot1 - yaw rotation
                // rot2 - normal rotation
                // Find rot1:
                // 1. rot = rot2 * rot1
                // 2. rot1 = inv(rot2) * rot
                // 3. axes.x * rot1 = x vector without yaw rotation
                // 4. find vector between axes.x*rot and axes.x*rot1 to find yaw rotation
                var rot2 = Quaternion.Normalize(
                    Quaternion.CreateFromAxisAngle(Vector3.Normalize(axes),
                        MathF.Acos(Vector3.Dot(Vector3.UnitZ, normal))));
                var rot1 = Quaternion.Normalize(Quaternion.Inverse(rot2) * rot);
                var x1 = Vector3.Transform(Vector3.UnitX, rot1);

                var rotationAngle = MathF.Atan2(x1.Y, x1.X);
                return (normal, rotationAngle);
            }
        }

        /// <summary>
        /// Returns roll, pitch, yaw angle rotations in radians
        /// Roll - around X axis
        /// Pitch - around Y axis
        /// Yaw - around Z axis
        /// Must be applied in the same order
        /// Source https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
        /// </summary>
        public static (float rollX, float pitchY, float yawZ) ToEulerAngles(this Quaternion quaternion)
        {
            var q = quaternion; // shorter name for readability
            // roll (x-axis rotation)
            var sinRollCosPitch = 2 * (q.W * q.X + q.Y * q.Z);
            var cosRollCosPitch = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            var roll = MathF.Atan2(sinRollCosPitch, cosRollCosPitch);

            // pitch (y-axis rotation)
            var sinPitch = 2 * (q.W * q.Y - q.Z * q.X);
            var pitch = MathF.Abs(sinPitch) >= 1 ? MathF.CopySign(MathF.PI / 2, sinPitch) : MathF.Asin(sinPitch);

            // yaw (z-axis rotation)
            var sinYawCosPitch = 2 * (q.W * q.Z + q.X * q.Y);
            var cosYawCosPitch = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            var yaw = MathF.Atan2(sinYawCosPitch, cosYawCosPitch);

            return (roll, pitch, yaw);
        }
    }
}