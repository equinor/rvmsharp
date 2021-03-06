using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmLine : RvmPrimitive
    {
        public readonly float A;
        public readonly float B;

        public RvmLine(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float a, float b)
            : base(version, kind, matrix, bBoxLocal)
        {
            A = a;
            B = b;
        }
    }
}