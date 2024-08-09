namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;

public enum SplittingReason
{
    None = 0b00000000,
    RootSector = 0b00000001,
    ByteSize = 0b00000010,
    NumPrimitives = 0b00000100,
    MinSectorDiagonal = 0b00001000,
    TriangleCount = 0b00010000
}

public record SectorSplittingMetadata(
    long ByteSizeCost,
    long EstimatedTriangleCount,
    int NumNodes,
    int NumPrimitives,
    int NumInstancedMeshes,
    int NumMeshes,
    SplittingReason SplittingReason
);

public record InternalSector(
    uint SectorId,
    uint? ParentSectorId,
    int Depth,
    string Path,
    float MinNodeDiagonal,
    float MaxNodeDiagonal,
    APrimitive[] Geometries,
    BoundingBox SubtreeBoundingBox,
    BoundingBox? GeometryBoundingBox,
    SectorSplittingMetadata Metadata
);
