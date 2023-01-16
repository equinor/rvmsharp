namespace RvmSharp.Primitives;

using System.Numerics;

/// <summary>
/// RvmLine is not yet well understood. Contributions welcome.
/// </summary>
/// <param name="Version"></param>
/// <param name="Matrix"></param>
/// <param name="BoundingBoxLocal"></param>
/// <param name="A">Unknown, maybe (A,0,0) in a Vec3 and then apply the matrix</param>
/// <param name="B">Unknown, maybe (B,0,0) in a Vec3 and then apply the matrix</param>
public record RvmLine(
        uint Version,
        Matrix4x4 Matrix,
        RvmBoundingBox BoundingBoxLocal,
        float A,
        float B)
    : RvmPrimitive(Version, RvmPrimitiveKind.Line, Matrix, BoundingBoxLocal);