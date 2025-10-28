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
    SectorDiagnostics? Diagnostics = null
);

public static class InternalSectorExtensions
{
    public static SectorDiagnostics GetDiagnostics(this InternalSector sector)
    {
        return sector.Diagnostics ?? new SectorDiagnostics(SplitReason.None, 0, 0, 0);
    }
}
