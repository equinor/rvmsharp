namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;

/// <summary>
/// A node for use in splitting
/// Is a collection for one or more geometries, connected to a tree index.
/// The same tree index can be present in multiple nodes.
/// </summary>
public record Node(
    ulong TreeIndex,
    APrimitive[] Geometries,
    long EstimatedByteSize,
    long EstimatedTriangleCount,
    BoundingBox BoundingBox
)
{
    public float Diagonal => BoundingBox.Diagonal;
};
