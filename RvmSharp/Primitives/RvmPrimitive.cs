namespace RvmSharp.Primitives
{
    using Containers;
    using System.Numerics;

    public abstract class RvmPrimitive : RvmGroup
    {
        public RvmPrimitiveKind Kind { get; }
        public Matrix4x4 Matrix { get; }
        public RvmBoundingBox BoundingBoxLocal { get; }

        public RvmConnection?[]
            Connections { get; } =
        {
            null, null, null, null, null, null
        }; // Up to six connections. Connections depend on primitive type.

        internal float SampleStartAngle;

        protected RvmPrimitive(uint version, RvmPrimitiveKind kind, Matrix4x4 matrix, RvmBoundingBox bBoxLocal) :
            base(version)
        {
            Kind = kind;
            Matrix = matrix;
            BoundingBoxLocal = bBoxLocal;
        }

        /// <summary>
        /// Use the BoundingBox and align with the rotation to make the best fitting axis aligned Bounding Box
        /// </summary>
        /// <returns>Bounding box in World Space.</returns>
        public RvmBoundingBox CalculateAxisAlignedBoundingBox()
        {
            var rotatedMax = Vector3.Transform(BoundingBoxLocal.Max, this.Matrix);
            var rotatedMin = Vector3.Transform(BoundingBoxLocal.Min, this.Matrix);
            var min = Vector3.Min(rotatedMax, rotatedMin);
            var max = Vector3.Max(rotatedMax, rotatedMin);
            return new RvmBoundingBox(Min: min, Max: max);
        }
    }
}