namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;

public record Node(
    ulong NodeId,
    APrimitive[] Geometries,
    long EstimatedByteSize,
    BoundingBox BoundingBox
)
{
    public float Diagonal => BoundingBox.Diagonal;
};