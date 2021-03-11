namespace RvmSharp.Primitives
{
    using System.Numerics;

    public class RvmCylinder : RvmPrimitive
    {
        public readonly float Radius;
        public readonly float Height;
        public RvmCylinder(uint version, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float radius, float height)
            : base(version, RvmPrimitiveKind.Cylinder, matrix, bBoxLocal)
        {
            Radius = radius;
            Height = height;
        }
    }
}