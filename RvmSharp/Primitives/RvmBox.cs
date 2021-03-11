namespace RvmSharp.Primitives
{
    using System.Numerics;

    public class RvmBox : RvmPrimitive
    {
        public readonly float LengthX;
        public readonly float LengthY;
        public readonly float LengthZ;

        public RvmBox(uint version, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float lengthX, float lengthY, float lengthZ)
            : base(version, RvmPrimitiveKind.Box, matrix, bBoxLocal)
        {
            LengthX = lengthX;
            LengthY = lengthY;
            LengthZ = lengthZ;
        }
    }
}