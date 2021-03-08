using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmRectangularTorus : RvmPrimitive
    {
        public readonly float RadiusInner;
        public readonly float RadiusOuter;
        public readonly float Height;
        public readonly float Angle;

        public RvmRectangularTorus(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float radiusInner, float radiusOuter, float height, float angle)
            : base(version, kind, matrix, bBoxLocal)
        {
            RadiusInner = radiusInner;
            RadiusOuter = radiusOuter;
            Height = height;
            Angle = angle;
        }
    }
}