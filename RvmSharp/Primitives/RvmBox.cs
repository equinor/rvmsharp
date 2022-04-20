namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmBox(
        uint Version,
        Matrix4x4 Matrix,
        RvmBoundingBox BoundingBoxLocal,
        float LengthX,
        float LengthY,
        float LengthZ)
    : RvmPrimitive(Version, RvmPrimitiveKind.Box, Matrix, BoundingBoxLocal);