namespace CadRevealComposer.Utils;

public interface IGeometryDistributionNodeStats
{
    int TriangleCountInInstancedMeshes { get; }
    int TriangleCountInTriangleMeshes { get; }
    int TriangleCountInTrapeziums { get; }
    int TriangleCountInTorusSegments { get; }
    int TriangleCountInQuads { get; }
    int TriangleCountInNuts { get; }
    int TriangleCountInGeneralRings { get; }
    int TriangleCountInEllipsoidSegments { get; }
    int TriangleCountInCones { get; }
    int TriangleCountInCircles { get; }
    int TriangleCountInBoxes { get; }
    int TriangleCountInEccentricCones { get; }

    int CountTriangleMesh { get; }
    int CountInstancedMesh { get; }
    int CountTrapezium { get; }
    int CountTorusSegment { get; }
    int CountQuad { get; }
    int CountNut { get; }
    int CountGeneralRing { get; }
    int CountEllipsoidSegment { get; }
    int CountCone { get; }
    int CountCircle { get; }
    int CountBox { get; }
    int CountEccentricCone { get; }

    int SumPrimitiveCount { get; }
    int SumTriangleCount { get; }
}
