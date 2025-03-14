namespace RvmSharp.Primitives;

using System.Linq;
using System.Numerics;
using Containers;

/// <summary>
/// RvmPrimitive is the base class for all primitives in the RVM file format.
///
/// Primitives is a base-class for all the actual geometry in a RvmFile.
/// A primitive is always a child of a <see cref="RvmNode"/>, and is a leaf node (primitives do not have children).
/// </summary>
/// <param name="Version">RvmVersion, unknown usecase.</param>
/// <param name="Kind">Primitive Kind</param>
/// <param name="Matrix">The Matrix for where the primitive is placed.</param>
/// <param name="BoundingBoxLocal">The Local bounding box of the RvmPrimitive.</param>
public abstract record RvmPrimitive(
    uint Version,
    RvmPrimitiveKind Kind,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal
) : RvmGroup(Version)
{
    public RvmConnection?[] Connections { get; } = { null, null, null, null, null, null }; // Up to six connections. Connections depend on primitive type.

    public virtual bool Equals(RvmPrimitive? other)
    {
        return this.GetHashCode() == other?.GetHashCode();
    }

    public override int GetHashCode()
    {
        // As connections is an array, we use this for comparison.
        var connectionsHash = (
            Connections[0],
            Connections[1],
            Connections[2],
            Connections[3],
            Connections[4],
            Connections[5]
        ).GetHashCode();
        return (
            base.GetHashCode(),
            connectionsHash,
            SampleStartAngle,
            (int)Kind,
            Matrix,
            BoundingBoxLocal
        ).GetHashCode();
    }

    /// <summary>
    /// Temporary value for the Sample Start Angle.
    /// </summary>
    internal float SampleStartAngle { get; set; }

    /// <summary>
    /// Use the BoundingBox and align with the rotation to make the best fitting axis aligned Bounding Box.
    /// Returns null for RvmLine, as bounding boxes are all over the place for that primitive.
    /// </summary>
    /// <returns>Bounding box in World Space.</returns>
    public RvmBoundingBox? CalculateAxisAlignedBoundingBox()
    {
        if (this is RvmLine)
        {
            return null;
        }

        var box = BoundingBoxLocal.GenerateBoxVertexes();

        var rotatedBox = box.Select(vertex => Vector3.Transform(vertex, this.Matrix)).ToArray();

        var min = rotatedBox.Aggregate(Vector3.Min);
        var max = rotatedBox.Aggregate(Vector3.Max);
        return new RvmBoundingBox(Min: min, Max: max);
    }
}
