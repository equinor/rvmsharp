namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Numerics;

public record Node(
    ulong NodeId,
    APrimitive[] Geometries,
    long EstimatedByteSize,
    Vector3 BoundingBoxMin,
    Vector3 BoundingBoxMax,
    float Diagonal
);