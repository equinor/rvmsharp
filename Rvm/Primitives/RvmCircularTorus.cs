using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmCircularTorus : RvmPrimitive
    {
        public readonly float _offset;
        public readonly float _radius;
        public readonly float _angle;

        public RvmCircularTorus(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float offset, float radius, float angle)
            : base(version, kind, matrix, bBoxLocal)
        {
            _offset = offset;
            _radius = radius;
            _angle = angle;
        }
    }
}