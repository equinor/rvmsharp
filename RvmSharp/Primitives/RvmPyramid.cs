namespace RvmSharp.Primitives;

using System.Numerics;

/// <summary>
/// RvmPyramid Primitive
/// </summary>
/// <param name="Version"></param>
/// <param name="Matrix"></param>
/// <param name="BoundingBoxLocal"></param>
/// <param name="BottomX">The width at bottom X</param>
/// <param name="BottomY">The depth at bottom Y</param>
/// <param name="TopX">The width at the top X</param>
/// <param name="TopY">The depth at the top Y</param>
/// <param name="OffsetX">The offset from center bottom to center top X</param>
/// <param name="OffsetY">The offset from center bottom to center top Y</param>
/// <param name="Height">The Height difference between bottom and top plane</param>
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
    float Height
) : RvmPrimitive(Version, RvmPrimitiveKind.Pyramid, Matrix, BoundingBoxLocal);
