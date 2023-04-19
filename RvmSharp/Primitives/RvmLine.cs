namespace RvmSharp.Primitives;

using System.Numerics;

/// <summary>
/// RvmLine is not yet well understood. Contributions welcome.
///
/// NIH: The RvmLines I have found were all very strange. The A and B data could maybe be a description of the line,
/// but I'm not sure how to handle them. The RvmLines I have found in Echo data are A=5 and B=0.
///
/// A theory is that A and B are the X positions in a vec3, and that the line is aligned with the matrix. In the data
/// I found that did not align with the Bounding Box data. So something is weird.
/// </summary>
/// <param name="Version"></param>
/// <param name="Matrix"></param>
/// <param name="BoundingBoxLocal"></param>
/// <param name="A">Unknown, maybe (A,0,0) in a Vec3 and then apply the matrix</param>
/// <param name="B">Unknown, maybe (B,0,0) in a Vec3 and then apply the matrix</param>
public record RvmLine(uint Version, Matrix4x4 Matrix, RvmBoundingBox BoundingBoxLocal, float A, float B)
    : RvmPrimitive(Version, RvmPrimitiveKind.Line, Matrix, BoundingBoxLocal);
