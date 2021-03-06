using System.Numerics;

namespace rvmsharp.Rvm
{
    internal class RvmPyramid : RvmPrimitive
    {
        public readonly float BottomX;
        public readonly  float BottomY;
        public readonly  float TopX;
        public readonly  float TopY;
        public readonly  float OffsetX;
        public readonly  float OffsetY;
        public readonly  float Height;

        public RvmPyramid(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal,
            float bottomX, float bottomY, float topX, float topY, float offsetX, float offsetY, float height) 
            : base(version, kind, matrix, bBoxLocal)
        {
            BottomX = bottomX;
            BottomY = bottomY;
            TopX = topX;
            TopY = topY;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Height = height;
        }
    }
}