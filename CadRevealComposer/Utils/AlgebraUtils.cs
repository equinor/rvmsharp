namespace CadRevealComposer.Utils
{
    using System;
    using System.Diagnostics.Contracts;
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

            var test = q.W * q.Y - q.Z * q.X;
            var test2 = q.W < 0.7f;
            if (test > 0.499f && test2)
            { // singularity at north pole
                var heading = 2f * MathF.Atan2(q.Y, q.X);
                var attitude = MathF.PI / 2f;
                return (heading, attitude, 0f);
            }
            if (test < -0.499f && test2)
            { // singularity at south pole
                var heading = -2f * MathF.Atan2(q.Y, q.X);
                var attitude = -MathF.PI / 2f;
                return (heading, attitude, 0f);
            }

            // roll (x-axis rotation)
            var sinRollCosPitch = 2f * (q.W * q.X + q.Y * q.Z);
            var cosRollCosPitch = 1f - 2f * (q.X * q.X + q.Y * q.Y);
            var roll = MathF.Atan2(sinRollCosPitch, cosRollCosPitch);

            // pitch (y-axis rotation)
            var sinPitch = 2 * (q.W * q.Y - q.Z * q.X);
            var pitch = MathF.Abs(sinPitch) >= 0.999f
                ? MathF.CopySign(MathF.PI / 2f, sinPitch)
                : MathF.Asin(sinPitch);

            // yaw (z-axis rotation)
            var sinYawCosPitch = 2f * (q.W * q.Z + q.X * q.Y);
            var cosYawCosPitch = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
            var yaw = MathF.Atan2(sinYawCosPitch, cosYawCosPitch);

            return (roll, pitch, yaw);
        }

        /// <summary>
        /// Returns angle in radians between two vectors or NaN if length of any vector is zero
        /// </summary>
        /// <param name="from">From vector</param>
        /// <param name="to">To vector</param>
        /// <returns>Angle in radians</returns>
        public static float AngleTo(this Vector3 from, Vector3 to)
        {
            return MathF.Acos(Vector3.Dot(from, to) / (from.Length() * to.Length()));
        }

        /// <summary>
        /// Returns quaternion that specifies a rotation from "from" vector to "to" vector
        /// </summary>
        /// <param name="from">from vector</param>
        /// <param name="to">to vector</param>
        /// <returns></returns>
        public static Quaternion FromToRotation(this Vector3 from, Vector3 to)
        {
            var cross = Vector3.Cross(from, to);
            if (cross.LengthSquared().ApproximatelyEquals(0f)) // true if vectors are parallel
            {
                var dot = Vector3.Dot(from, to);
                if (dot < 0) // Vectors point in opposite directions
                {
                    // We need to find an orthogonal to (to), non-zero vector (v)
                    // such as dot product of (v) and (to) is 0
                    // or satisfies following equation: to.x * v.x + to.y * v.y + to.z + v.z = 0
                    // below some variants depending on which components of (to) is 0
                    var xZero = to.X.ApproximatelyEquals(0);
                    var yZero = to.Y.ApproximatelyEquals(0);
                    var zZero = to.Z.ApproximatelyEquals(0);
                    Vector3 axes;
                    if (xZero && yZero)
                        axes = new Vector3(to.Z, 0, 0);
                    else if (xZero && zZero)
                        axes = new Vector3(to.Y, 0, 0);
                    else if (yZero && zZero)
                        axes = new Vector3(0, to.X, 0);
                    else if (xZero)
                        axes = new Vector3(0, -to.Z, -to.Y);
                    else if (yZero)
                        axes = new Vector3(-to.Z, 0, -to.X);
                    else
                        axes = new Vector3(-to.Y, -to.Z, 0);
                    return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axes), MathF.PI * 2);
                }
                return Quaternion.Identity;
            }

            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(cross), from.AngleTo(to));
        }

        /// <summary>
        /// This method will extract transformation matrix such as
        /// (PAi) * Mt = (PBi)
        /// The extracted matrix will de decomposable into scale, rotation and translation matrices
        /// </summary>
        /// <param name="pa1"></param>
        /// <param name="pa2"></param>
        /// <param name="pa3"></param>
        /// <param name="pa4"></param>
        /// <param name="pb1"></param>
        /// <param name="pb2"></param>
        /// <param name="pb3"></param>
        /// <param name="pb4"></param>
        /// <param name="transform">output transformation matrix</param>
        /// <returns>true if there is such matrix</returns>
        public static bool GetTransform(Vector3 pa1, Vector3 pa2, Vector3 pa3, Vector3 pa4, Vector3 pb1, Vector3 pb2, Vector3 pb3, Vector3 pb4, out Matrix4x4 transform)
        {
            var va12 = pa2 - pa1;
            var va13 = pa3 - pa1;
            var va14 = pa4 - pa1;
            var vb12 = pb2 - pb1;
            var vb13 = pb3 - pb1;
            var vb14 = pb4 - pb1;

            var squaredBLengths = new Vector3(vb12.LengthSquared(), vb13.LengthSquared(), vb14.LengthSquared());
            var squaredALengths = new Vector3(va12.LengthSquared(), va13.LengthSquared(), va14.LengthSquared());
            var dist = (squaredALengths - squaredBLengths).Length();
            var scale = Vector3.One;
            if (!dist.ApproximatelyEquals(0))
            {
                var vaMatrix = new Matrix4x4(
                    va12.X * va12.X, va12.Y * va12.Y, va12.Z * va12.Z, 0,
                    va13.X * va13.X, va13.Y * va13.Y, va13.Z * va13.Z, 0,
                    va14.X * va14.X, va14.Y * va14.Y, va14.Z * va14.Z, 0,
                    0, 0, 0, 1);
                if (!Matrix4x4.Invert(vaMatrix, out var vaMatrixInverse))
                {
                    transform = default;
                    return false;
                }

                var scaleSquared = Vector3.Transform(squaredBLengths, Matrix4x4.Transpose(vaMatrixInverse));
                scale = new Vector3(MathF.Sqrt(scaleSquared.X), MathF.Sqrt(scaleSquared.Y), MathF.Sqrt(scaleSquared.Z));
                va12 = va12 * scale;
                va13 = va13 * scale;
            }

            // 2 rotation va'1,va'2 -> vb1,vb2
            var vaNormal = Vector3.Normalize(Vector3.Cross(va12, va13));
            var vbNormal = Vector3.Normalize(Vector3.Cross(vb12, vb13));
            var rot1 = vaNormal.FromToRotation(vbNormal);

            // 3 axis rotation: axis=vb2-vb1 va'3-va'1
            var va12r1 = Vector3.Transform(va12, rot1);
            var angle2 = va12r1.AngleTo(vb12);

            var va12r1vb12cross = Vector3.Cross(va12r1, vb12);
            var rotationNormal = Vector3.Normalize(Vector3.Cross(va12r1, vb12));
            var rot2 = va12r1vb12cross.LengthSquared().ApproximatelyEquals(0)
                ? Quaternion.Identity
                : Quaternion.CreateFromAxisAngle(rotationNormal, angle2);

            var rotation = Quaternion.Normalize(rot2 * rot1);

            // translation
            var translation = pb1 - Vector3.Transform(pa1 * scale, rotation);

            transform =
                Matrix4x4.CreateScale(scale)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(translation);

            return pb1.ApproximatelyEquals(Vector3.Transform(pa1, transform), 0.001f) &&
                   pb2.ApproximatelyEquals(Vector3.Transform(pa2, transform), 0.001f) &&
                   pb3.ApproximatelyEquals(Vector3.Transform(pa3, transform), 0.001f) &&
                   pb4.ApproximatelyEquals(Vector3.Transform(pa4, transform), 0.001f);
        }
    }
}