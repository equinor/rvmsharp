namespace RvmSharp.Primitives
{
    using Containers;
    using System.Numerics;

    public abstract class RvmPrimitive : RvmGroup
    {
        public readonly RvmPrimitiveKind Kind;
        public readonly Matrix4x4 Matrix;
        public readonly RvmBoundingBox BoundingBoxLocal;
        public readonly RvmConnection?[] Connections = new RvmConnection[6]; // One Connection in each direction.
        internal float SampleStartAngle;

        public RvmPrimitive(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal) : base(version)
        {
            Kind = kind;
            Matrix = matrix;
            BoundingBoxLocal = bBoxLocal;
        }
    }
}