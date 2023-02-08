namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Numerics;

public record ProtoSector(
    uint SectorId,
    uint? ParentSectorId,
    int Depth,
    string Path,
    APrimitive[] Geometries,
    BoundingBox SubtreeBoundingBox,
    BoundingBox GeometryBoundingBox
);