namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmCircularTorus(
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    float Offset,
    float Radius,
    float Angle
) : RvmPrimitive(Version, RvmPrimitiveKind.CircularTorus, Matrix, BoundingBoxLocal)
{
    /// <summary>
    /// Returns an equivalent torus with a deterministic tessellation seam.
    /// The sample-start angle only affects the sampled vertex phase, not the
    /// underlying circular-torus surface.
    /// </summary>
    public RvmCircularTorus WithSampleStartAngle(float sampleStartAngle)
    {
        var copy = this with { };
        copy.SampleStartAngle = sampleStartAngle;
        return copy;
    }
}
