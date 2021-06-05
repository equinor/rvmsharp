namespace RvmSharp.Primitives
{
    using System.Numerics;

    public record RvmLine(
            uint Version,
            Matrix4x4 Matrix,
            RvmBoundingBox BoundingBoxLocal,
            float A,
            float B)
        : RvmPrimitive(Version, RvmPrimitiveKind.Line, Matrix, BoundingBoxLocal);
}