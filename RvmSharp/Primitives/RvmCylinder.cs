namespace RvmSharp.Primitives;

using System.Numerics;

public record RvmCylinder(uint Version, Matrix4x4 Matrix, RvmBoundingBox BoundingBoxLocal, float Radius, float Height)
    : RvmPrimitive(Version, RvmPrimitiveKind.Cylinder, Matrix, BoundingBoxLocal);
