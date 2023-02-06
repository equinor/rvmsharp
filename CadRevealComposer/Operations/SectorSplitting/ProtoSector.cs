namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Numerics;

public record ProtoSector(
    uint SectorId,
    uint? ParentSectorId,
    int Depth,
    string Path,
    APrimitive[] Geometries,
    Vector3 SubtreeBoundingBoxMin,
    Vector3 SubtreeBoundingBoxMax,
    Vector3 GeometryBoundingBoxMin,
    Vector3 GeometryBoundingBoxMax
);