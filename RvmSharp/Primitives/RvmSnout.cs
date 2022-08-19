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
    float TopShearY) : RvmPrimitive(Version,
    RvmPrimitiveKind.Snout,
    Matrix,
    BoundingBoxLocal)
{
    public bool HasShear()
    {
        return BottomShearX != 0 ||
               BottomShearY != 0 ||
               TopShearX != 0 ||
               TopShearY != 0;
    }

    public bool IsEccentric()
    {
        return OffsetX != 0 ||
               OffsetY != 0;
    }

    public (float semiMinorAxis, float semiMajorAxis) GetTopRadii()
    {
        var slope = GetTopSlope().slope;
        var semiMajorRadius = slope != 0 ? RadiusTop / MathF.Cos(slope) : RadiusTop;

        return (RadiusTop, semiMajorRadius);
    }

    public (float semiMinorAxis, float semiMajorAxis) GetBottomRadii()
    {
        var slope = GetBottomSlope().slope;
        var semiMajorRadius = slope != 0 ? RadiusBottom / MathF.Cos(slope) : RadiusBottom;

        return (RadiusBottom, semiMajorRadius);
    }

    public (Quaternion rotation, Vector3 normal, float slope) GetTopSlope()
    {
        return TranslateShearToSlope(TopShearX, TopShearY);
    }

    public (Quaternion rotation, Vector3 normal, float slope) GetBottomSlope()
    {
        return TranslateShearToSlope(BottomShearX, BottomShearY);
    }

    private (Quaternion rotation, Vector3 normal, float slope) TranslateShearToSlope(float shearX, float shearY)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, shearY);
        var rotation = rotationAroundX * rotationAroundY;
        var normal = Vector3.Transform(Vector3.UnitZ, rotation);
        var slope = MathF.PI / 2f - MathF.Atan2(normal.Z, MathF.Sqrt(normal.X * normal.X + normal.Y * normal.Y));

        return (rotation, normal, slope);
    }
};