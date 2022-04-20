namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmCircularTorus(
        uint Version,
        Matrix4x4 Matrix,
        RvmBoundingBox BoundingBoxLocal,
        float Offset,
        float Radius,
        float Angle)
    : RvmPrimitive(Version, RvmPrimitiveKind.CircularTorus, Matrix, BoundingBoxLocal);