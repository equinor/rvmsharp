namespace RvmSharp.Primitives;

using System;
using System.Numerics;

public record RvmSnout(
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    float RadiusBottom,
    float RadiusTop,
    float Height,
    float OffsetX,
    float OffsetY,
    float BottomShearX,
    float BottomShearY,
    float TopShearX,
    float TopShearY
) : RvmPrimitive(Version, RvmPrimitiveKind.Snout, Matrix, BoundingBoxLocal);
