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
        double CastToZeroIfInvalid(double number) => (double.IsNaN(number) || double.IsInfinity(number)) ? 0.0 : number;

        double percentIncrCountInstancedMesh = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountInstancedMesh, statsBefore.CountInstancedMesh)
        );
        double percentIncrCountTriangleMesh = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountTriangleMesh, statsBefore.CountTriangleMesh)
        );
        double percentIncrCountTrapezium = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountTrapezium, statsBefore.CountTrapezium)
        );
        double percentIncrCountTorusSegment = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountTorusSegment, statsBefore.CountTorusSegment)
        );
        double percentIncrCountQuad = CastToZeroIfInvalid(CalcIncreaseInPercent(DiffCountQuad, statsBefore.CountQuad));
        double percentIncrCountNut = CastToZeroIfInvalid(CalcIncreaseInPercent(DiffCountNut, statsBefore.CountNut));
        double percentIncrCountGeneralRing = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountGeneralRing, statsBefore.CountGeneralRing)
        );
        double percentIncrCountEllipsoidSegment = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountEllipsoidSegment, statsBefore.CountEllipsoidSegment)
        );
        double percentIncrCountCone = CastToZeroIfInvalid(CalcIncreaseInPercent(DiffCountCone, statsBefore.CountCone));
        double percentIncrCountCircle = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountCircle, statsBefore.CountCircle)
        );
        double percentIncrCountBox = CastToZeroIfInvalid(CalcIncreaseInPercent(DiffCountBox, statsBefore.CountBox));
        double percentIncrCountEccentricCone = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffCountEccentricCone, statsBefore.CountEccentricCone)
        );
        double percentIncrSumPrimitiveCount = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffSumPrimitiveCount, statsBefore.SumPrimitiveCount)
        );

        double percentIncrTriangleCountInInstancedMeshes = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInInstancedMeshes, statsBefore.TriangleCountInInstancedMeshes)
        );
        double percentIncrTriangleCountInTriangleMeshes = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInTriangleMeshes, statsBefore.TriangleCountInTriangleMeshes)
        );
        double percentIncrTriangleCountInTrapeziums = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInTrapeziums, statsBefore.TriangleCountInTrapeziums)
        );
        double percentIncrTriangleCountInTorusSegments = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInTorusSegments, statsBefore.TriangleCountInTorusSegments)
        );
        double percentIncrTriangleCountInQuads = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInQuads, statsBefore.TriangleCountInQuads)
        );
        double percentIncrTriangleCountInNuts = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInNuts, statsBefore.TriangleCountInNuts)
        );
        double percentIncrTriangleCountInGeneralRing = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInGeneralRing, statsBefore.TriangleCountInGeneralRings)
        );
        double percentIncrTriangleCountInEllipsoidSegment = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInEllipsoidSegment, statsBefore.TriangleCountInEllipsoidSegments)
        );
        double percentIncrTriangleCountInCone = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInCone, statsBefore.TriangleCountInCones)
        );
        double percentIncrTriangleCountInCircle = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInCircle, statsBefore.TriangleCountInCircles)
        );
        double percentIncrTriangleCountInBox = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInBox, statsBefore.TriangleCountInBoxes)
        );
        double percentIncrTriangleCountInEccentricCone = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffTriangleCountInEccentricCone, statsBefore.TriangleCountInEccentricCones)
        );
        double percentIncrSumTriangleCount = CastToZeroIfInvalid(
            CalcIncreaseInPercent(DiffSumTriangleCount, statsBefore.SumTriangleCount)
        );

        Console.WriteLine($"Geometry statistics: {haeding}");
        Console.WriteLine(
            "+====================+=======================+=======================+=======================+=======================+"
        );
        Console.WriteLine(
            "| Primitive          | Primitive count diff. | Increase. in percent  | Triangle count diff.  | Increase. in percent  +"
        );
        Console.WriteLine(
            "+--------------------+-----------------------+-----------------------+-----------------------+-----------------------+"
        );
        Console.WriteLine(
            $" Instanced mesh       {DiffCountInstancedMesh, -24}{percentIncrCountInstancedMesh, -24:0.000}{DiffTriangleCountInInstancedMeshes, -24}{percentIncrTriangleCountInInstancedMeshes, -24:0.000}"
        );
        Console.WriteLine(
            $" Triangle mesh        {DiffCountTriangleMesh, -24}{percentIncrCountTriangleMesh, -24:0.000}{DiffTriangleCountInTriangleMeshes, -24}{percentIncrTriangleCountInTriangleMeshes, -24:0.000}"
        );
        Console.WriteLine(
            $" Trapezium            {DiffCountTrapezium, -24}{percentIncrCountTrapezium, -24:0.000}{DiffTriangleCountInTrapeziums, -24}{percentIncrTriangleCountInTrapeziums, -24:0.000}"
        );
        Console.WriteLine(
            $" Torus segment        {DiffCountTorusSegment, -24}{percentIncrCountTorusSegment, -24:0.000}{DiffTriangleCountInTorusSegments, -24}{percentIncrTriangleCountInTorusSegments, -24:0.000}"
        );
        Console.WriteLine(
            $" Quad                 {DiffCountQuad, -24}{percentIncrCountQuad, -24:0.000}{DiffTriangleCountInQuads, -24}{percentIncrTriangleCountInQuads, -24:0.000}"
        );
        Console.WriteLine(
            $" Nut                  {DiffCountNut, -24}{percentIncrCountNut, -24:0.000}{DiffTriangleCountInNuts, -24}{percentIncrTriangleCountInNuts, -24:0.000}"
        );
        Console.WriteLine(
            $" General ring         {DiffCountGeneralRing, -24}{percentIncrCountGeneralRing, -24:0.000}{DiffTriangleCountInGeneralRing, -24}{percentIncrTriangleCountInGeneralRing, -24:0.000}"
        );
        Console.WriteLine(
            $" Ellipsoid segment    {DiffCountEllipsoidSegment, -24}{percentIncrCountEllipsoidSegment, -24:0.000}{DiffTriangleCountInEllipsoidSegment, -24}{percentIncrTriangleCountInEllipsoidSegment, -24:0.000}"
        );
        Console.WriteLine(
            $" Cone                 {DiffCountCone, -24}{percentIncrCountCone, -24:0.000}{DiffTriangleCountInCone, -24}{percentIncrTriangleCountInCone, -24:0.000}"
        );
        Console.WriteLine(
            $" Circle               {DiffCountCircle, -24}{percentIncrCountCircle, -24:0.000}{DiffTriangleCountInCircle, -24}{percentIncrTriangleCountInCircle, -24:0.000}"
        );
        Console.WriteLine(
            $" Box                  {DiffCountBox, -24}{percentIncrCountBox, -24:0.000}{DiffTriangleCountInBox, -24}{percentIncrTriangleCountInBox, -24:0.000}"
        );
        Console.WriteLine(
            $" Eccentric cone       {DiffCountEccentricCone, -24}{percentIncrCountEccentricCone, -24:0.000}{DiffTriangleCountInEccentricCone, -24}{percentIncrTriangleCountInEccentricCone, -24:0.000}"
        );
        Console.WriteLine(
            "+--------------------------------------------------------------------------------------------------------------------+"
        );
        Console.WriteLine(
            $" SUM                  {DiffSumPrimitiveCount, -24}{percentIncrSumPrimitiveCount, -24:0.000}{DiffSumTriangleCount, -24}{percentIncrSumTriangleCount, -24:0.000}"
        );
        Console.WriteLine(
            "+====================================================================================================================+"
        );
    }

    public static double CalcIncreaseInPercent(int diff, int valueBefore)
    {
        return diff * 100.0 / valueBefore;
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
