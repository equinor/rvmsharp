using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmRectangularTorus : RvmPrimitive
    {
        public readonly float _radiusInner;
        public readonly float _radiusOuter;
        public readonly float _height;
        public readonly float _angle;

        public RvmRectangularTorus(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float radiusInner, float radiusOuter, float height, float angle)
            : base(version, kind, matrix, bBoxLocal)
        {
            _radiusInner = radiusInner;
            _radiusOuter = radiusOuter;
            _height = height;
            _angle = angle;
        }
    }
}