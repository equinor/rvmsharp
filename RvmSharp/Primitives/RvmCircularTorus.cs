namespace RvmSharp.Primitives
{
    using System.Numerics;

    public class RvmCircularTorus : RvmPrimitive
    {
        public readonly float Offset;
        public readonly float Radius;
        public readonly float Angle;

        public RvmCircularTorus(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float offset, float radius, float angle)
            : base(version, kind, matrix, bBoxLocal)
        {
            Offset = offset;
            Radius = radius;
            Angle = angle;
        }
    }
}