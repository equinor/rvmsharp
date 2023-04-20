namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmRectangularTorus(
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    float RadiusInner,
    float RadiusOuter,
    float Height,
    float Angle
) : RvmPrimitive(Version, RvmPrimitiveKind.RectangularTorus, Matrix, BoundingBoxLocal);
