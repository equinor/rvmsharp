namespace CadRevealComposer.Tests.Utils;

using System.Text.RegularExpressions;
using CadRevealComposer.Utils;
using MathNet.Numerics;
using Primitives;
using System.Drawing;
using System.Numerics;
using Tessellation;

class GeometryDistributionNodeStatsTest : IGeometryDistributionNodeStats
{
    public GeometryDistributionNodeStatsTest(int datasetIndex)
    {
        switch (datasetIndex)
        {
            case 0:
                TriangleCountInInstancedMeshes = 10;
                TriangleCountInTriangleMeshes = 20;
                TriangleCountInTrapeziums = 30;
                TriangleCountInTorusSegments = 40;
                TriangleCountInQuads = 50;
                TriangleCountInNuts = 60;
                TriangleCountInGeneralRings = 70;
                TriangleCountInEllipsoidSegments = 80;
                TriangleCountInCones = 90;
                TriangleCountInCircles = 100;
                TriangleCountInBoxes = 110;
                TriangleCountInEccentricCones = 120;

                CountTriangleMesh = 130;
                CountInstancedMesh = 140;
                CountTrapezium = 150;
                CountTorusSegment =  160;
                CountQuad = 170;
                CountNut = 180;
                CountGeneralRing = 190;
                CountEllipsoidSegment = 200;
                CountCone = 210;
                CountCircle = 220;
                CountBox = 230;
                CountEccentricCone = 240;

                SumPrimitiveCount = 250;
                SumTriangleCount = 260;
                break;
            case 1:
                TriangleCountInInstancedMeshes = 1;
                TriangleCountInTriangleMeshes = 2;
                TriangleCountInTrapeziums = 3;
                TriangleCountInTorusSegments = 4;
                TriangleCountInQuads = 5;
                TriangleCountInNuts = 6;
                TriangleCountInGeneralRings = 7;
                TriangleCountInEllipsoidSegments = 8;
                TriangleCountInCones = 9;
                TriangleCountInCircles = 10;
                TriangleCountInBoxes = 11;
                TriangleCountInEccentricCones = 12;

                CountTriangleMesh = 13;
                CountInstancedMesh = 14;
                CountTrapezium = 15;
                CountTorusSegment =  16;
                CountQuad = 17;
                CountNut = 18;
                CountGeneralRing = 19;
                CountEllipsoidSegment = 20;
                CountCone = 21;
                CountCircle = 22;
                CountBox = 23;
                CountEccentricCone = 24;

                SumPrimitiveCount = 25;
                SumTriangleCount = 26;
                break;
        }
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

[TestFixture]
public class GeometryDistributionNodeStatsDiffTests
{
    private static List<double> ExtractValuesFromTable(string name, string table)
    {
        // Locate first row with "name" and then extract the following four numbers
        string regularExpression1 = "^.*" + name + ".*$";
        const string regularExpression2 = @"[0-9\.]+";

        // Perform regex query
        Match match1 = Regex.Match(table, regularExpression1, RegexOptions.Multiline);
        MatchCollection match2 = match1.Success ? Regex.Matches(match1.ToString(), regularExpression2, RegexOptions.Singleline) : null;

        // Construct output from query result
        return (match2 == null || match2.Count == 0) ? null : match2.Select(str => double.Parse(str.ToString())).ToList();
    }

    [Test]
    public void
        UsingGeometryDistributionNodeStatDiff_GivenTwoGeometryDistributionNodeStatsInputs_VerifyDiffOutput()
    {
        // Prepare
        var stat0 = new GeometryDistributionNodeStatsTest(0);
        var stat1 = new GeometryDistributionNodeStatsTest(1);

        // Act
        var diff = new GeometryDistributionNodeStatsDiff(stat1, stat0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(diff.DiffTriangleCountInInstancedMeshes, Is.EqualTo(10 - 1));
            Assert.That(diff.DiffTriangleCountInTriangleMeshes, Is.EqualTo(20 - 2));
            Assert.That(diff.DiffTriangleCountInTrapeziums, Is.EqualTo(30 - 3));
            Assert.That(diff.DiffTriangleCountInTorusSegments, Is.EqualTo(40 - 4));
            Assert.That(diff.DiffTriangleCountInQuads, Is.EqualTo(50 - 5));
            Assert.That(diff.DiffTriangleCountInNuts, Is.EqualTo(60 - 6));
            Assert.That(diff.DiffTriangleCountInGeneralRing, Is.EqualTo(70 - 7));
            Assert.That(diff.DiffTriangleCountInEllipsoidSegment, Is.EqualTo(80 - 8));
            Assert.That(diff.DiffTriangleCountInCone, Is.EqualTo(90 - 9));
            Assert.That(diff.DiffTriangleCountInCircle, Is.EqualTo(100 - 10));
            Assert.That(diff.DiffTriangleCountInBox, Is.EqualTo(110 - 11));
            Assert.That(diff.DiffTriangleCountInEccentricCone, Is.EqualTo(120 - 12));

            Assert.That(diff.DiffCountTriangleMesh, Is.EqualTo(130 - 13));
            Assert.That(diff.DiffCountInstancedMesh, Is.EqualTo(140 - 14));
            Assert.That(diff.DiffCountTrapezium, Is.EqualTo(150 - 15));
            Assert.That(diff.DiffCountTorusSegment, Is.EqualTo(160 - 16));
            Assert.That(diff.DiffCountQuad, Is.EqualTo(170 - 17));
            Assert.That(diff.DiffCountNut, Is.EqualTo(180 - 18));
            Assert.That(diff.DiffCountGeneralRing, Is.EqualTo(190 - 19));
            Assert.That(diff.DiffCountEllipsoidSegment, Is.EqualTo(200 - 20));
            Assert.That(diff.DiffCountCone, Is.EqualTo(210 - 21));
            Assert.That(diff.DiffCountCircle, Is.EqualTo(220 - 22));
            Assert.That(diff.DiffCountBox, Is.EqualTo(230 - 23));
            Assert.That(diff.DiffCountEccentricCone, Is.EqualTo(240 - 24));

            Assert.That(diff.DiffSumPrimitiveCount, Is.EqualTo(250 - 25));
            Assert.That(diff.DiffSumTriangleCount, Is.EqualTo(260 - 26));
        });
    }

    [Test]
    public void
        UsingGeometryDistributionNodeStatDiff_GivenInputsWhereOutputIsKnown_VerifyCalcOfIncreaseInPercent()
    {
        // Prepare
        const int diff1 = 0;
        const int valueBefore1 = 486;
        const double trueIncrease1 = 0.0; // %

        const int diff2 = 243;
        const int valueBefore2 = 486;
        const double trueIncrease2 = 50.0; // %

        const int diff3 = 486;
        const int valueBefore3 = 486;
        const double trueIncrease3 = 100.0; // %

        const int diff4 = 486*2;
        const int valueBefore4 = 486;
        const double trueIncrease4 = 200.0; // %

        // Act
        double increase1 = GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff1, valueBefore1);
        double increase2 = GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff2, valueBefore2);
        double increase3 = GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff3, valueBefore3);
        double increase4 = GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff4, valueBefore4);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(increase1, Is.EqualTo(trueIncrease1).Within(1.0E-3));
            Assert.That(increase2, Is.EqualTo(trueIncrease2).Within(1.0E-3));
            Assert.That(increase3, Is.EqualTo(trueIncrease3).Within(1.0E-3));
            Assert.That(increase4, Is.EqualTo(trueIncrease4).Within(1.0E-3));
        });
    }

    [Test]
    public void
        UsingGeometryDistributionNodeStatDiff_GivenTwoGeometryDistributionNodeStatsInputs_VerifyPrintStatisticsOutput()
    {
        // Prepare
        var stat0 = new GeometryDistributionNodeStatsTest(0);
        var stat1 = new GeometryDistributionNodeStatsTest(1);
        var diff = new GeometryDistributionNodeStatsDiff(stat1, stat0);

        // Act
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        diff.PrintStatistics();
        var result = stringWriter.ToString().Trim();

        // Assert
        List<double> values = null;
        Assert.Multiple(() =>
        {
            values = ExtractValuesFromTable("Instanced mesh", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountInstancedMesh));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountInstancedMesh, stat1.CountInstancedMesh)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInInstancedMeshes));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInInstancedMeshes, stat1.TriangleCountInInstancedMeshes)).Within(1.0E-3));

            values = ExtractValuesFromTable("Triangle mesh", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountTriangleMesh));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountTriangleMesh, stat1.CountTriangleMesh)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInTriangleMeshes));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInTriangleMeshes, stat1.TriangleCountInTriangleMeshes)).Within(1.0E-3));

            values = ExtractValuesFromTable("Trapezium", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountTrapezium));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountTrapezium, stat1.CountTrapezium)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInTrapeziums));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInTrapeziums, stat1.TriangleCountInTrapeziums)).Within(1.0E-3));

            values = ExtractValuesFromTable("Torus segment", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountTorusSegment));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountTorusSegment, stat1.CountTorusSegment)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInTorusSegments));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInTorusSegments, stat1.TriangleCountInTorusSegments)).Within(1.0E-3));

            values = ExtractValuesFromTable("Quad", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountQuad));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountQuad, stat1.CountQuad)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInQuads));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInQuads, stat1.TriangleCountInQuads)).Within(1.0E-3));

            values = ExtractValuesFromTable("Nut", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountNut));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountNut, stat1.CountNut)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInNuts));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInNuts, stat1.TriangleCountInNuts)).Within(1.0E-3));

            values = ExtractValuesFromTable("General ring", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountGeneralRing));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountGeneralRing, stat1.CountGeneralRing)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInGeneralRing));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInGeneralRing, stat1.TriangleCountInGeneralRings)).Within(1.0E-3));

            values = ExtractValuesFromTable("Ellipsoid segment", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountEllipsoidSegment));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountEllipsoidSegment, stat1.CountEllipsoidSegment)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInEllipsoidSegment));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInEllipsoidSegment, stat1.TriangleCountInEllipsoidSegments)).Within(1.0E-3));

            values = ExtractValuesFromTable("Cone", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountCone));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountCone, stat1.CountCone)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInCone));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInCone, stat1.TriangleCountInCones)).Within(1.0E-3));

            values = ExtractValuesFromTable("Circle", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountCircle));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountCircle, stat1.CountCircle)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInCircle));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInCircle, stat1.TriangleCountInCircles)).Within(1.0E-3));

            values = ExtractValuesFromTable("Box", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountBox));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountBox, stat1.CountBox)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInBox));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInBox, stat1.TriangleCountInBoxes)).Within(1.0E-3));

            values = ExtractValuesFromTable("Eccentric cone", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffCountEccentricCone));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffCountEccentricCone, stat1.CountEccentricCone)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffTriangleCountInEccentricCone));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffTriangleCountInEccentricCone, stat1.TriangleCountInEccentricCones)).Within(1.0E-3));

            values = ExtractValuesFromTable("SUM", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That((int)values[0], Is.EqualTo(diff.DiffSumPrimitiveCount));
            Assert.That(values[1], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffSumPrimitiveCount, stat1.SumPrimitiveCount)).Within(1.0E-3));
            Assert.That((int)values[2], Is.EqualTo(diff.DiffSumTriangleCount));
            Assert.That(values[3], Is.EqualTo(GeometryDistributionNodeStatsDiff.CalcIncreaseInPercent(diff.DiffSumTriangleCount, stat1.SumTriangleCount)).Within(1.0E-3));
        });
    }
}
