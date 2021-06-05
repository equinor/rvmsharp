namespace RvmSharp.Primitives
{
    using System.Numerics;


    public record RvmPyramid(
            uint Version,
            Matrix4x4 Matrix,
            RvmBoundingBox BoundingBoxLocal,
            float BottomX,
            float BottomY,
            float TopX,
            float TopY,
            float OffsetX,
            float OffsetY,
            float Height)
        : RvmPrimitive(Version, RvmPrimitiveKind.Pyramid, Matrix, BoundingBoxLocal);


}