namespace RvmSharp.Primitives
{
    using System.Numerics;

    public class RvmSphericalDish : RvmPrimitive
    {
        public readonly float BaseRadius;
        public readonly float Height;

        public RvmSphericalDish(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float baseRadius, float height)
            : base(version, kind, matrix, bBoxLocal)
        {
            BaseRadius = baseRadius;
            Height = height;
        }
    }
}