using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmSphere : RvmPrimitive
    {
        public readonly float Diameter;
        
        public RvmSphere(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float diameter)
        : base(version, kind, matrix, bBoxLocal)
        {
            Diameter = diameter;
        }
    }
}