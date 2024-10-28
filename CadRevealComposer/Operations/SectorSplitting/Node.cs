namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;

public record Node(
    ulong NodeId,
    APrimitive[] Geometries,
    long EstimatedByteSize,
    long EstimatedTriangleCount,
    BoundingBox BoundingBox,
    NodePriority Priority
)
{
    public float Diagonal => BoundingBox.Diagonal;
};
