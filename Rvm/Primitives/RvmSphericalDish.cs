using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmSphericalDish : RvmPrimitive
    {
        public readonly float _baseRadius;
        public readonly float _height;

        public RvmSphericalDish(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float baseRadius, float height)
            : base(version, kind, matrix, bBoxLocal)
        {
            _baseRadius = baseRadius;
            _height = height;
        }
    }
}