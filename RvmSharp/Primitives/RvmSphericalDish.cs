namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmSphericalDish(
        uint Version,
        Matrix4x4 Matrix,
        RvmBoundingBox BBoxLocal,
        float BaseRadius,
        float Height)
    : RvmPrimitive(Version, RvmPrimitiveKind.SphericalDish, Matrix, BBoxLocal);