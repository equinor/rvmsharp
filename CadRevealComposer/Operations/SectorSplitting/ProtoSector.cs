namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;

public record ProtoSector(
    uint SectorId,
    uint? ParentSectorId,
    int Depth,
    string Path,
    float MinNodeDiagonal,
    float MaxNodeDiagonal,
    APrimitive[] Geometries,
    BoundingBox SubtreeBoundingBox,
    BoundingBox GeometryBoundingBox
);