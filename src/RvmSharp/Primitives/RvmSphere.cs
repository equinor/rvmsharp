namespace RvmSharp.Primitives
{
    using System.Numerics;

    public record RvmSphere(
            uint Version,
            Matrix4x4 Matrix,
            RvmBoundingBox BoundingBoxLocal,
            float Diameter)
        : RvmPrimitive(
            Version,
            RvmPrimitiveKind.Sphere,
            Matrix,
            BoundingBoxLocal);

}