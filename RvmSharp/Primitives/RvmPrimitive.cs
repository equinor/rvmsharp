namespace RvmSharp.Primitives
{
    using Containers;
    using System.Numerics;

    public abstract class RvmPrimitive : RvmGroup
    {
        public RvmPrimitiveKind Kind { get; }
        public Matrix4x4 Matrix { get; }
        public RvmBoundingBox BoundingBoxLocal { get; }
        public RvmConnection?[] Connections { get; } = {null, null, null, null, null, null}; // Up to six connections. Connections depend on primitive type.

        internal float SampleStartAngle;

        public RvmPrimitive(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal) :
            base(version)
        {
            Kind = kind;
            Matrix = matrix;
            BoundingBoxLocal = bBoxLocal;
        }
    }
}