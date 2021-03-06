using System.Numerics;

namespace rvmsharp.Rvm
{
    internal class RvmBox : RvmPrimitive
    {
        public readonly float LengthX;
        public readonly float LengthY;
        public readonly float LengthZ;

        public RvmBox(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float lengthX, float lengthY, float lengthZ)
            : base(version, kind, matrix, bBoxLocal)
        {
            LengthX = lengthX;
            LengthY = lengthY;
            LengthZ = lengthZ;
        }
    }
}