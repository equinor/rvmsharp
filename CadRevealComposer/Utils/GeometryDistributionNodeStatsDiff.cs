namespace CadRevealComposer.Utils;

using System;
using System.Collections.Generic;
using Operations.Tessellating;
using Primitives;

public class GeometryDistributionNodeStatsDiff(
    IGeometryDistributionNodeStats statsBefore,
    IGeometryDistributionNodeStats statsAfter
)
{
    public void PrintStatistics(string haeding = "")
    {
        double percentIncrCountInstancedMesh = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountInstancedMesh, statsBefore.CountInstancedMesh)
        );
        double percentIncrCountTriangleMesh = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountTriangleMesh, statsBefore.CountTriangleMesh)
        );
        double percentIncrCountTrapezium = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountTrapezium, statsBefore.CountTrapezium)
        );
        double percentIncrCountTorusSegment = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountTorusSegment, statsBefore.CountTorusSegment)
        );
        double percentIncrCountQuad = ZeroIfNotFinite(CalcIncreaseInPercent(DiffCountQuad, statsBefore.CountQuad));
        double percentIncrCountNut = ZeroIfNotFinite(CalcIncreaseInPercent(DiffCountNut, statsBefore.CountNut));
        double percentIncrCountGeneralRing = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountGeneralRing, statsBefore.CountGeneralRing)
        );
        double percentIncrCountEllipsoidSegment = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountEllipsoidSegment, statsBefore.CountEllipsoidSegment)
        );
        double percentIncrCountCone = ZeroIfNotFinite(CalcIncreaseInPercent(DiffCountCone, statsBefore.CountCone));
        double percentIncrCountCircle = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountCircle, statsBefore.CountCircle)
        );
        double percentIncrCountBox = ZeroIfNotFinite(CalcIncreaseInPercent(DiffCountBox, statsBefore.CountBox));
        double percentIncrCountEccentricCone = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffCountEccentricCone, statsBefore.CountEccentricCone)
        );
        double percentIncrSumPrimitiveCount = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffSumPrimitiveCount, statsBefore.SumPrimitiveCount)
        );

        double percentIncrTriangleCountInInstancedMeshes = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInInstancedMeshes, statsBefore.TriangleCountInInstancedMeshes)
        );
        double percentIncrTriangleCountInTriangleMeshes = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInTriangleMeshes, statsBefore.TriangleCountInTriangleMeshes)
        );
        double percentIncrTriangleCountInTrapeziums = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInTrapeziums, statsBefore.TriangleCountInTrapeziums)
        );
        double percentIncrTriangleCountInTorusSegments = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInTorusSegments, statsBefore.TriangleCountInTorusSegments)
        );
        double percentIncrTriangleCountInQuads = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInQuads, statsBefore.TriangleCountInQuads)
        );
        double percentIncrTriangleCountInNuts = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInNuts, statsBefore.TriangleCountInNuts)
        );
        double percentIncrTriangleCountInGeneralRing = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInGeneralRing, statsBefore.TriangleCountInGeneralRings)
        );
        double percentIncrTriangleCountInEllipsoidSegment = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInEllipsoidSegment, statsBefore.TriangleCountInEllipsoidSegments)
        );
        double percentIncrTriangleCountInCone = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInCone, statsBefore.TriangleCountInCones)
        );
        double percentIncrTriangleCountInCircle = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInCircle, statsBefore.TriangleCountInCircles)
        );
        double percentIncrTriangleCountInBox = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInBox, statsBefore.TriangleCountInBoxes)
        );
        double percentIncrTriangleCountInEccentricCone = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffTriangleCountInEccentricCone, statsBefore.TriangleCountInEccentricCones)
        );
        double percentIncrSumTriangleCount = ZeroIfNotFinite(
            CalcIncreaseInPercent(DiffSumTriangleCount, statsBefore.SumTriangleCount)
        );
        // csharpier-ignore-start -- Easier to read the table formatting
        Console.WriteLine($"Geometry statistics: {haeding}");
        Console.WriteLine("+====================+=======================+=======================+=======================+=======================+");
        Console.WriteLine("| Primitive          | Primitive count diff. | Increase. in percent  | Triangle count diff.  | Increase. in percent  +");
        Console.WriteLine("+--------------------+-----------------------+-----------------------+-----------------------+-----------------------+");
        Console.WriteLine($" Instanced mesh       {DiffCountInstancedMesh, 24:N0}{percentIncrCountInstancedMesh, 24:P1}{DiffTriangleCountInInstancedMeshes, 24:N0}{percentIncrTriangleCountInInstancedMeshes, 24:P1}");
        Console.WriteLine($" Triangle mesh        {DiffCountTriangleMesh, 24:N0}{percentIncrCountTriangleMesh, 24:P1}{DiffTriangleCountInTriangleMeshes, 24:N0}{percentIncrTriangleCountInTriangleMeshes, 24:P1}");
        Console.WriteLine($" Trapezium            {DiffCountTrapezium, 24:N0}{percentIncrCountTrapezium, 24:P1}{DiffTriangleCountInTrapeziums, 24:N0}{percentIncrTriangleCountInTrapeziums, 24:P1}");
        Console.WriteLine($" Torus segment        {DiffCountTorusSegment, 24:N0}{percentIncrCountTorusSegment, 24:P1}{DiffTriangleCountInTorusSegments, 24:N0}{percentIncrTriangleCountInTorusSegments, 24:P1}");
        Console.WriteLine($" Quad                 {DiffCountQuad, 24:N0}{percentIncrCountQuad, 24:P1}{DiffTriangleCountInQuads, 24:N0}{percentIncrTriangleCountInQuads, 24:P1}");
        Console.WriteLine($" Nut                  {DiffCountNut, 24:N0}{percentIncrCountNut, 24:P1}{DiffTriangleCountInNuts, 24:N0}{percentIncrTriangleCountInNuts, 24:P1}");
        Console.WriteLine($" General ring         {DiffCountGeneralRing, 24:N0}{percentIncrCountGeneralRing, 24:P1}{DiffTriangleCountInGeneralRing, 24:N0}{percentIncrTriangleCountInGeneralRing, 24:P1}");
        Console.WriteLine($" Ellipsoid segment    {DiffCountEllipsoidSegment, 24:N0}{percentIncrCountEllipsoidSegment, 24:P1}{DiffTriangleCountInEllipsoidSegment, 24:N0}{percentIncrTriangleCountInEllipsoidSegment, 24:P1}");
        Console.WriteLine($" Cone                 {DiffCountCone, 24:N0}{percentIncrCountCone, 24:P1}{DiffTriangleCountInCone, 24:N0}{percentIncrTriangleCountInCone, 24:P1}");
        Console.WriteLine($" Circle               {DiffCountCircle, 24:N0}{percentIncrCountCircle, 24:P1}{DiffTriangleCountInCircle, 24:N0}{percentIncrTriangleCountInCircle, 24:P1}");
        Console.WriteLine($" Box                  {DiffCountBox, 24:N0}{percentIncrCountBox, 24:P1}{DiffTriangleCountInBox, 24:N0}{percentIncrTriangleCountInBox, 24:P1}");
        Console.WriteLine($" Eccentric cone       {DiffCountEccentricCone, 24:N0}{percentIncrCountEccentricCone, 24:P1}{DiffTriangleCountInEccentricCone, 24:N0}{percentIncrTriangleCountInEccentricCone, 24:P1}");
        Console.WriteLine("+--------------------------------------------------------------------------------------------------------------------+");
        Console.WriteLine($" SUM                  {DiffSumPrimitiveCount, 24:N0}{percentIncrSumPrimitiveCount, 24:P1}{DiffSumTriangleCount, 24:N0}{percentIncrSumTriangleCount, 24:P1}");
        Console.WriteLine("+====================================================================================================================+");
        return;
        // csharpier-ignore-end

        double ZeroIfNotFinite(double number) => (double.IsFinite(number) ? number : 0);
    }

    /// <summary>
    /// Calculates the increase in percent (0.1 = 10% increase).
    /// </summary>
    /// <returns>The increase/decrease where 100% increase returns 1.0</returns>
    public static double CalcIncreaseInPercent(int diff, int valueBefore)
    {
        return diff / (double)valueBefore;
    }

    public int DiffTriangleCountInInstancedMeshes { get; } =
        statsAfter.TriangleCountInInstancedMeshes - statsBefore.TriangleCountInInstancedMeshes;
    public int DiffTriangleCountInTriangleMeshes { get; } =
        statsAfter.TriangleCountInTriangleMeshes - statsBefore.TriangleCountInTriangleMeshes;
    public int DiffTriangleCountInTrapeziums { get; } =
        statsAfter.TriangleCountInTrapeziums - statsBefore.TriangleCountInTrapeziums;
    public int DiffTriangleCountInTorusSegments { get; } =
        statsAfter.TriangleCountInTorusSegments - statsBefore.TriangleCountInTorusSegments;
    public int DiffTriangleCountInQuads { get; } = statsAfter.TriangleCountInQuads - statsBefore.TriangleCountInQuads;
    public int DiffTriangleCountInNuts { get; } = statsAfter.TriangleCountInNuts - statsBefore.TriangleCountInNuts;
    public int DiffTriangleCountInGeneralRing { get; } =
        statsAfter.TriangleCountInGeneralRings - statsBefore.TriangleCountInGeneralRings;
    public int DiffTriangleCountInEllipsoidSegment { get; } =
        statsAfter.TriangleCountInEllipsoidSegments - statsBefore.TriangleCountInEllipsoidSegments;
    public int DiffTriangleCountInCone { get; } = statsAfter.TriangleCountInCones - statsBefore.TriangleCountInCones;
    public int DiffTriangleCountInCircle { get; } =
        statsAfter.TriangleCountInCircles - statsBefore.TriangleCountInCircles;
    public int DiffTriangleCountInBox { get; } = statsAfter.TriangleCountInBoxes - statsBefore.TriangleCountInBoxes;
    public int DiffTriangleCountInEccentricCone { get; } =
        statsAfter.TriangleCountInEccentricCones - statsBefore.TriangleCountInEccentricCones;

    public int DiffCountTriangleMesh { get; } = statsAfter.CountTriangleMesh - statsBefore.CountTriangleMesh;
    public int DiffCountInstancedMesh { get; } = statsAfter.CountInstancedMesh - statsBefore.CountInstancedMesh;
    public int DiffCountTrapezium { get; } = statsAfter.CountTrapezium - statsBefore.CountTrapezium;
    public int DiffCountTorusSegment { get; } = statsAfter.CountTorusSegment - statsBefore.CountTorusSegment;
    public int DiffCountQuad { get; } = statsAfter.CountQuad - statsBefore.CountQuad;
    public int DiffCountNut { get; } = statsAfter.CountNut - statsBefore.CountNut;
    public int DiffCountGeneralRing { get; } = statsAfter.CountGeneralRing - statsBefore.CountGeneralRing;
    public int DiffCountEllipsoidSegment { get; } =
        statsAfter.CountEllipsoidSegment - statsBefore.CountEllipsoidSegment;
    public int DiffCountCone { get; } = statsAfter.CountCone - statsBefore.CountCone;
    public int DiffCountCircle { get; } = statsAfter.CountCircle - statsBefore.CountCircle;
    public int DiffCountBox { get; } = statsAfter.CountBox - statsBefore.CountBox;
    public int DiffCountEccentricCone { get; } = statsAfter.CountEccentricCone - statsBefore.CountEccentricCone;

    public int DiffSumPrimitiveCount { get; } = statsAfter.SumPrimitiveCount - statsBefore.SumPrimitiveCount;
    public int DiffSumTriangleCount { get; } = statsAfter.SumTriangleCount - statsBefore.SumTriangleCount;
}
