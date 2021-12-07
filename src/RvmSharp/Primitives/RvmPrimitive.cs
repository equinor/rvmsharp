namespace RvmSharp.Primitives
{
    using Containers;
    using System.Linq;
    using System.Numerics;

    public abstract record RvmPrimitive(uint Version,
            RvmPrimitiveKind Kind,
            Matrix4x4 Matrix,
            RvmBoundingBox BoundingBoxLocal)
        : RvmGroup(Version)
    {
        public RvmConnection?[]
            Connections { get; } =
        {
            null, null, null, null, null, null
        }; // Up to six connections. Connections depend on primitive type.

        public virtual bool Equals(RvmPrimitive? other)
        {
            return this.GetHashCode() == other?.GetHashCode();
        }

        public override int GetHashCode()
        {
            // As connections is an array, we use this for comparison.
            var connectionsHash = (Connections[0],Connections[1],Connections[2],Connections[3],Connections[4],Connections[5]).GetHashCode();
            return (base.GetHashCode(), connectionsHash, SampleStartAngle, (int) Kind, Matrix, BoundingBoxLocal).GetHashCode();
        }

        /// <summary>
        /// Temporary value for the Sample Start Angle.
        /// </summary>
        internal float SampleStartAngle { get; set; }

        /// <summary>
        /// Use the BoundingBox and align with the rotation to make the best fitting axis aligned Bounding Box
        /// </summary>
        /// <returns>Bounding box in World Space.</returns>
        public RvmBoundingBox CalculateAxisAlignedBoundingBox()
        {
            var box = BoundingBoxLocal.GenerateBoxVertexes();

            var rotatedBox = box.Select(vertex => Vector3.Transform(vertex, this.Matrix)).ToArray();

            var min = rotatedBox.Aggregate(Vector3.Min);
            var max = rotatedBox.Aggregate(Vector3.Max);
            return new RvmBoundingBox(Min: min, Max: max);
        }
    }
}