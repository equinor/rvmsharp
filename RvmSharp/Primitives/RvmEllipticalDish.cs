namespace RvmSharp.Primitives
{
    using System.Numerics;

    public class RvmEllipticalDish : RvmPrimitive
    {
        public readonly float BaseRadius;
        public readonly float Height;

        public RvmEllipticalDish(uint version, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float baseRadius, float height)
            : base(version, RvmPrimitiveKind.EllipticalDish, matrix, bBoxLocal)
        {
            BaseRadius = baseRadius;
            Height = height;
        }
    }
}