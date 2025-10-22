namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;

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
    bool IsPrioritizedSector = false,
    SplitReason SplitReason = SplitReason.None,
    int PrimitiveCount = 0,
    int MeshCount = 0,
    int InstanceMeshCount = 0
);

