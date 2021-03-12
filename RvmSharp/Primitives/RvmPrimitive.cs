namespace RvmSharp.Primitives
{
    using Containers;
    using System.Numerics;

    public abstract class RvmPrimitive : RvmGroup
    {
        public readonly uint Version;
        public readonly RvmPrimitiveKind Kind;
        public readonly Matrix4x4 Matrix;
        public readonly RvmBoundingBox BoundingBoxLocal;
        public readonly RvmConnection[] Connections = { null, null, null, null, null, null};
        internal float SampleStartAngle;

        public RvmPrimitive(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal) : base(version)
        {
            Version = version;
            Kind = kind;
            Matrix = matrix;
            BoundingBoxLocal = bBoxLocal;
        }
    }
}