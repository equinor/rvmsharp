namespace CadRevealComposer.Utils;

using System;
using System.Collections.Generic;
using Operations.Tessellating;
using Primitives;

public class GeometryDistributionNodeStats : IGeometryDistributionNodeStats
{
    public GeometryDistributionNodeStats(List<CadRevealNode> nodes)
    {
        foreach (CadRevealNode node in nodes)
        {
            foreach (APrimitive primitive in node.Geometries)
            {
                switch (primitive)
                {
                    case InstancedMesh instancedMesh:
                        TriangleCountInInstancedMeshes += instancedMesh.TemplateMesh.TriangleCount;
                        break;
                    case TriangleMesh triangleMesh:
                        TriangleCountInTriangleMeshes += triangleMesh.Mesh.TriangleCount;
                        break;
                    case Trapezium:
                        TriangleCountInTrapeziums += 2;
                        break;
                    case TorusSegment torusSegment:
                        TriangleCountInTorusSegments +=
                            TorusSegmentTessellator.Tessellate(torusSegment)?.Mesh.TriangleCount ?? 0;
                        break;
                    case Quad:
                        TriangleCountInQuads += 2;
                        break;
                    case Nut:
                        TriangleCountInNuts += 24;
                        break;
                    case GeneralRing generalRing:
                        TriangleCountInGeneralRings +=
                            GeneralRingTessellator.Tessellate(generalRing)?.Mesh.TriangleCount ?? 0;
                        break;
                    case EllipsoidSegment:
                        TriangleCountInEllipsoidSegments += 4;
                        break;
                    case Cone cone:
                        TriangleCountInCones += ConeTessellator.Tessellate(cone)?.Mesh.TriangleCount ?? 0;
                        break;
                    case Circle circle:
                        TriangleCountInCircles += CircleTessellator.Tessellate(circle)?.Mesh.TriangleCount ?? 0;
                        break;
                    case Box box:
                        TriangleCountInBoxes += BoxTessellator.Tessellate(box)?.Mesh.TriangleCount ?? 0;
                        break;
                    case EccentricCone eccentricCone:
                        TriangleCountInEccentricCones +=
                            EccentricConeTessellator.Tessellate(eccentricCone)?.Mesh.TriangleCount ?? 0;
                        break;
                }
            }

            var distribution = new GeometryDistributionStats(node.Geometries);
            CountTriangleMesh += distribution.TriangleMeshes;
            CountInstancedMesh += distribution.InstancedMeshes;
            CountTrapezium += distribution.Trapeziums;
            CountTorusSegment += distribution.TorusSegments;
            CountQuad += distribution.Quads;
            CountNut += distribution.Nuts;
            CountGeneralRing += distribution.GeneralRings;
            CountEllipsoidSegment += distribution.EllipsoidSegments;
            CountCone += distribution.Cones;
            CountCircle += distribution.Circles;
            CountBox += distribution.Boxes;
            CountEccentricCone += distribution.EccentricCones;
        }

        SumPrimitiveCount =
            CountTriangleMesh
            + CountInstancedMesh
            + CountTrapezium
            + CountTorusSegment
            + CountQuad
            + CountNut
            + CountGeneralRing
            + CountEllipsoidSegment
            + CountCone
            + CountCircle
            + CountBox
            + CountEccentricCone;
        SumTriangleCount =
            TriangleCountInInstancedMeshes
            + TriangleCountInTriangleMeshes
            + TriangleCountInTrapeziums
            + TriangleCountInTorusSegments
            + TriangleCountInQuads
            + TriangleCountInNuts
            + TriangleCountInGeneralRings
            + TriangleCountInEllipsoidSegments
            + TriangleCountInCones
            + TriangleCountInCircles
            + TriangleCountInBoxes
            + TriangleCountInEccentricCones;
    }

    public void PrintStatistics(string heading = "")
    {
        // csharpier-ignore-start -- We allow longer lines here for better readability
        Console.WriteLine($"Geometry statistics: {heading}");
        Console.WriteLine("+====================+=======================+=======================+");
        Console.WriteLine("| Primitive          | Primitive count       | Triangle count        |");
        Console.WriteLine("+--------------------+-----------------------+-----------------------+");
        Console.WriteLine($" Instanced mesh       {CountInstancedMesh, 24:N0}{TriangleCountInInstancedMeshes, 24:N0}");
        Console.WriteLine($" Triangle mesh        {CountTriangleMesh, 24:N0}{TriangleCountInTriangleMeshes, 24:N0}");
        Console.WriteLine($" Trapezium            {CountTrapezium, 24:N0}{TriangleCountInTrapeziums, 24:N0}");
        Console.WriteLine($" Torus segment        {CountTorusSegment, 24:N0}{TriangleCountInTorusSegments, 24:N0}");
        Console.WriteLine($" Quad                 {CountQuad, 24:N0}{TriangleCountInQuads, 24:N0}");
        Console.WriteLine($" Nut                  {CountNut, 24:N0}{TriangleCountInNuts, 24:N0}");
        Console.WriteLine($" General ring         {CountGeneralRing, 24:N0}{TriangleCountInGeneralRings, 24:N0}");
        Console.WriteLine($" Ellipsoid segment    {CountEllipsoidSegment, 24:N0}{TriangleCountInEllipsoidSegments, 24:N0}");
        Console.WriteLine($" Cone                 {CountCone, 24:N0}{TriangleCountInCones, 24:N0}");
        Console.WriteLine($" Circle               {CountCircle, 24:N0}{TriangleCountInCircles, 24:N0}");
        Console.WriteLine($" Box                  {CountBox, 24:N0}{TriangleCountInBoxes, 24:N0}");
        Console.WriteLine($" Eccentric cone       {CountEccentricCone, 24:N0}{TriangleCountInEccentricCones, 24:N0}");
        Console.WriteLine("---------------------------------------------------------------------+");
        Console.WriteLine($" SUM                  {SumPrimitiveCount, 24:N0}{SumTriangleCount, 24:N0}");
        Console.WriteLine("+====================================================================+");
        // csharpier-ignore-end
    }

    public int TriangleCountInInstancedMeshes { get; }
    public int TriangleCountInTriangleMeshes { get; }
    public int TriangleCountInTrapeziums { get; }
    public int TriangleCountInTorusSegments { get; }
    public int TriangleCountInQuads { get; }
    public int TriangleCountInNuts { get; }
    public int TriangleCountInGeneralRings { get; }
    public int TriangleCountInEllipsoidSegments { get; }
    public int TriangleCountInCones { get; }
    public int TriangleCountInCircles { get; }
    public int TriangleCountInBoxes { get; }
    public int TriangleCountInEccentricCones { get; }

    public int CountTriangleMesh { get; }
    public int CountInstancedMesh { get; }
    public int CountTrapezium { get; }
    public int CountTorusSegment { get; }
    public int CountQuad { get; }
    public int CountNut { get; }
    public int CountGeneralRing { get; }
    public int CountEllipsoidSegment { get; }
    public int CountCone { get; }
    public int CountCircle { get; }
    public int CountBox { get; }
    public int CountEccentricCone { get; }

    public int SumPrimitiveCount { get; }
    public int SumTriangleCount { get; }
}
