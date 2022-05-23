namespace CadRevealComposer.Utils;

using System;
using System.Diagnostics;
using System.Numerics;

public static class AlgebraUtils
{
    public static float NormalizeRadians(float value)
    {
        var twoPi = MathF.PI + MathF.PI;
        while (value <= -Math.PI) value += twoPi;
        while (value > Math.PI) value -= twoPi;
        return value;
    }

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
    /// Euler Angle (1,2,3) sequence
    /// Returns roll, pitch, yaw angle rotations in radians
    /// Roll - around X axis
    /// Pitch - around Y axis
    /// Yaw - around Z axis
    /// Must be applied in the same order
    /// Based on (quaternion to rotation matrix decomposition, and Euler angle sequence)
    /// https://www.astro.rug.nl/software/kapteyn-beta/_downloads/attitude.pdf
    /// and (singularity solution)
    /// https://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.371.6578
    /// </summary>
    public static (float rollX, float pitchY, float yawZ) ToEulerAngles(this Quaternion q)
    {
        // this value should give under 1 mm error per 1 m on all rotations in HDA
        // maybe we should consider using decimals
        const double gimbalLockLimit = 0.999_999_9;
        var q0 = (double)q.W;
        var q1 = (double)q.X;
        var q2 = (double)q.Y;
        var q3 = (double)q.Z;

        // Rotation matrix components
        var r11 = q0 * q0 + q1 * q1 - q2 * q2 - q3 * q3; // cos(pitchY) * cos(yawZ)
        var r12 = 2 * q1 * q2 - 2 * q0 * q3; // sin(rollX) * sin(pitchY) * cos(yawZ) - cos(rollX) * sin(yawZ)
        var r13 = 2 * q1 * q3 + 2 * q0 * q2; // cos(rollX) * sin(pitchY) * cos(yawZ) + sin(rollX) * sin(yawZ)
        var r21 = 2 * q1 * q2 + 2 * q0 * q3; // cos(pitchY) * sin(yawZ)
        var r31 = 2 * q1 * q3 - 2 * q0 * q2; // - sin(pitchY)
        var r32 = 2 * q2 * q3 + 2 * q0 * q1; // sin(rollX) * cos(pitchY)
        var r33 = q0 * q0 - q1 * q1 - q2 * q2 + q3 * q3; // cos(rollX) * cos(pitchY)


        if (Math.Abs(r31) < gimbalLockLimit) { // Gimbal lock/singularity check
            var pitchY = -Math.Asin(r31);
            var rollX = Math.Atan2(r32 / Math.Cos(pitchY), r33 / Math.Cos(pitchY));
            var yawZ = Math.Atan2(r21 / Math.Cos(pitchY), r11 / Math.Cos(pitchY));
            return ((float)rollX, (float)pitchY, (float)yawZ);
        }

        // Lock detected
        if (r31 < 0)
        {
            return ((float)Math.Atan2(r12, r13), MathF.PI / 2, 0);
        }
        else
        {
            return ((float)Math.Atan2(-r12, -r13), -MathF.PI / 2, 0);
        }
    }

    public static void AssertEulerAnglesCorrect((float rollX, float pitchY, float yawZ) eulerAngles, Quaternion rotation)
    {
        (float rollX, float pitchY, float yawZ) = eulerAngles;
        // Assert that converting to euler angels and back gives the same transformation (but not necessarily the same quaternion)
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, rollX);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, pitchY);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, yawZ);
        var qc = qz * qy * qx;
        var v1 = Vector3.Transform(Vector3.One, rotation);
        var v2 = Vector3.Transform(Vector3.One, qc);
        Debug.Assert(rotation.Length().ApproximatelyEquals(1f));
        //Debug.Assert(v1.EqualsWithinFactor(v2, 0.001f)); // 0.1%
        // TODO: fix assert
        // TODO: fix assert
        // TODO: fix assert
        // TODO: fix assert
        // TODO: fix assert
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
    /// Decompose transformation matrix in scale, rotation and translation. The difference between this
    /// and inbuilt Decompose method is that this one will normalize rotation. Default decompose method
    /// reconstructs rotation from matrix elements and may introduce imprecision in quaternion. This will
    /// have a negative effect on Euler angle decomposition
    /// </summary>
    /// <param name="transform">Transform to decompose</param>
    /// <param name="scale">Output scale vector</param>
    /// <param name="rotation">Normalized rotation quaternion</param>
    /// <param name="translation">Vector translation</param>
    /// <returns>True if transform can be decomposed</returns>
    public static bool DecomposeAndNormalize(this Matrix4x4 transform, out Vector3 scale, out Quaternion rotation,
        out Vector3 translation)
    {
        if (Matrix4x4.Decompose(transform, out scale, out rotation, out translation))
        {
            rotation = Quaternion.Normalize(rotation);
            if (rotation.X.ApproximatelyEquals(Quaternion.Identity.X, 0.000_005) &&
                rotation.Y.ApproximatelyEquals(Quaternion.Identity.Y, 0.000_005) &&
                rotation.Z.ApproximatelyEquals(Quaternion.Identity.Z, 0.000_005) &&
                rotation.W.ApproximatelyEquals(Quaternion.Identity.W, 0.000_005))
            {
                rotation = Quaternion.Identity;
            }

            return true;
        }

        return false;
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
        if (!dist.ApproximatelyEquals(0, 0.001f))
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
        }

        var va12Scaled = va12 * scale;
        var va13Scaled = va13 * scale;

        // 2 rotation va'1,va'2 -> vb1,vb2
        var vaNormal = Vector3.Normalize(Vector3.Cross(va12Scaled, va13Scaled));
        var vbNormal = Vector3.Normalize(Vector3.Cross(vb12, vb13));
        var rot1 = vaNormal.FromToRotation(vbNormal);

        // 3 axis rotation: axis=vb2-vb1 va'3-va'1
        var va12r1 = Vector3.Transform(va12Scaled, rot1);
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

        const float OneMillimeter = 0.001f; // assumption: the data is in meters
        return pb1.EqualsWithinTolerance(Vector3.Transform(pa1, transform), OneMillimeter) &&
               pb2.EqualsWithinTolerance(Vector3.Transform(pa2, transform), OneMillimeter) &&
               pb3.EqualsWithinTolerance(Vector3.Transform(pa3, transform), OneMillimeter) &&
               pb4.EqualsWithinTolerance(Vector3.Transform(pa4, transform), OneMillimeter);
    }
}