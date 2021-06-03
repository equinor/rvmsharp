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
            if (axes.Length() < 0.01f) // Cross product of parallel vectors is 0-vector
            {
                var x2 = Vector3.Transform(Vector3.UnitX, rot);
                var a = MathF.Atan2(x2.Y, x2.X);
                var sameDirection = Vector3.Distance(Vector3.UnitZ, normal) < 0.1f;
                var rotationAngle = sameDirection ? a: -a;
                return (normal, rotationAngle);
            } else {
                // rot - combined rotation
                // rot1 - yaw rotation
                // rot2 - normal rotation
                // Find rot1:
                // 1. rot = rot2 * rot1
                // 2. rot1 = inv(rot2) * rot
                // 3. axes.x * rot1 = x vector without yaw rotation
                // 4. find vector between axes.x*rot and axes.x*rot1 to find yaw rotation
                var rot2 = Quaternion.Normalize(
                Quaternion.CreateFromAxisAngle(axes, MathF.Acos(Vector3.Dot(Vector3.UnitZ, normal))));
                var rot1 = Quaternion.Normalize(Quaternion.Inverse(rot2) * rot);
                var x1 = Vector3.Transform(Vector3.UnitX, rot1);

                var rotationAngle = MathF.Atan2(x1.Y, x1.X);
                return (normal, rotationAngle);
            }
        }
    }
}