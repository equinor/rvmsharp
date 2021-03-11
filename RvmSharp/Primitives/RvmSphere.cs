namespace RvmSharp.Primitives
{
    using System.Numerics;

    public class RvmSphere : RvmPrimitive
    {
        public readonly float Diameter;
        
        public RvmSphere(uint version, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float diameter)
        : base(version, RvmPrimitiveKind.Sphere, matrix, bBoxLocal)
        {
            Diameter = diameter;
        }
    }
}