using System.Numerics;

namespace rvmsharp.Rvm
{
    public class RvmSnout : RvmPrimitive
    {
        public readonly float RadiusBottom;
        public readonly float RadiusTop;
        public readonly float Height;
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float BottomShearX;
        public readonly float BottomShearY;
        public readonly float TopShearX;
        public readonly float TopShearY;

        public RvmSnout(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal, float radiusBottom, float radiusTop, float height, float offsetX, float offsetY, float bottomShearX, float bottomShearY, float topShearX, float topShearY)
            : base(version, kind, matrix, bBoxLocal)
        {
            RadiusBottom = radiusBottom;
            RadiusTop = radiusTop;
            Height = height;
            OffsetX = offsetX;
            OffsetY = offsetY;
            BottomShearX = bottomShearX;
            BottomShearY = bottomShearY;
            TopShearX = topShearX;
            TopShearY = topShearY;
        }
    }
}