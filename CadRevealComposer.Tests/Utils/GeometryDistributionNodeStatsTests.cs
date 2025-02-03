namespace CadRevealComposer.Tests.Utils;

using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using CadRevealComposer.Utils;
using Primitives;
using Tessellation;

[TestFixture]
public class GeometryDistributionNodeStatsTests
{
    private static List<Vector3> GenVertices(int count)
    {
        var vertices = new List<Vector3>();
        for (int i = 0; i < (count * 3); i++)
        {
            vertices.Add(new Vector3(0.0f, 0.0f, 0.0f));
        }

        return vertices;
    }

    private static List<uint> GenIndices(int count)
    {
        var indices = new List<uint>();
        for (uint i = 0; i < (count * 3); i++)
        {
            indices.Add(i);
        }

        return indices;
    }

    private static List<CadRevealNode> GenNodes(
        (int count, int triangles) instancedMeshStat,
        (int count, int triangles) triangleMeshesStat,
        int trapeziumCount,
        int torusSegmentCount,
        int quadCount,
        int nutCount,
        int generalRingCount,
        int ellipsoidSegmentCount,
        int coneCount,
        int circleCount,
        int boxCount,
        int eccentricConeCount,
        int nodeCount
    )
    {
        var geometries = new List<APrimitive>();

        // Generate instanced meshes
        for (int i = 0; i < instancedMeshStat.count; i++)
        {
            geometries.Add(
                new InstancedMesh(
                    0,
                    new Mesh(
                        GenVertices(instancedMeshStat.triangles).ToArray(),
                        GenIndices(instancedMeshStat.triangles).ToArray(),
                        1.0E-3f
                    ),
                    Matrix4x4.Identity,
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate triangle meshes
        for (int i = 0; i < triangleMeshesStat.count; i++)
        {
            geometries.Add(
                new TriangleMesh(
                    new Mesh(
                        GenVertices(triangleMeshesStat.triangles).ToArray(),
                        GenIndices(triangleMeshesStat.triangles).ToArray(),
                        1.0E-3f
                    ),
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate trapeziums
        for (int i = 0; i < trapeziumCount; i++)
        {
            geometries.Add(
                new Trapezium(
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, 1),
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate torus segments
        for (int i = 0; i < torusSegmentCount; i++)
        {
            geometries.Add(
                new TorusSegment(
                    90.0f,
                    Matrix4x4.Identity,
                    100.0f,
                    10.0f,
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate quads
        for (int i = 0; i < quadCount; i++)
        {
            geometries.Add(
                new Quad(
                    Matrix4x4.Identity,
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate nuts
        for (int i = 0; i < nutCount; i++)
        {
            geometries.Add(
                new Nut(Matrix4x4.Identity, 0, Color.Black, new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1)))
            );
        }

        // Generate rings
        for (int i = 0; i < generalRingCount; i++)
        {
            geometries.Add(
                new GeneralRing(
                    180.0f,
                    90.0f,
                    Matrix4x4.Identity,
                    new Vector3(1, 0, 0),
                    1.0f,
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate ellipsoid segments
        for (int i = 0; i < ellipsoidSegmentCount; i++)
        {
            geometries.Add(
                new EllipsoidSegment(
                    15.0f,
                    10.0f,
                    10.0f,
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate cones
        for (int i = 0; i < coneCount; i++)
        {
            geometries.Add(
                new Cone(
                    180.0f,
                    90.0f,
                    new Vector3(0, 0, 0),
                    new Vector3(1, 1, 1),
                    new Vector3(1, 0, 0),
                    5.0f,
                    5.0f,
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate circles
        for (int i = 0; i < circleCount; i++)
        {
            geometries.Add(
                new Circle(
                    Matrix4x4.Identity,
                    new Vector3(1, 0, 0),
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        // Generate boxes
        for (int i = 0; i < boxCount; i++)
        {
            geometries.Add(
                new Box(Matrix4x4.Identity, 0, Color.Black, new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1)))
            );
        }

        // Generate eccentric cones
        for (int i = 0; i < eccentricConeCount; i++)
        {
            geometries.Add(
                new EccentricCone(
                    new Vector3(0, 0, 0),
                    new Vector3(1, 1, 1),
                    new Vector3(1, 0, 0),
                    10.0f,
                    10.0f,
                    0,
                    Color.Black,
                    new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
                )
            );
        }

        var nodes = new List<CadRevealNode>();
        for (int i = 0; i < nodeCount; i++)
        {
            nodes.Add(
                new CadRevealNode
                {
                    TreeIndex = 0,
                    Name = "Test",
                    Parent = null,
                    Geometries = geometries.ToArray()
                }
            );
        }

        return nodes;
    }

    private static List<int> ExtractValuesFromTable(string name, string table)
    {
        // Locate first row with "name" and then extract the following four numbers
        string regularExpression1 = "^.*" + name + ".*$";
        const string regularExpression2 = "[0-9]+";

        // Perform regex query
        Match match1 = Regex.Match(table, regularExpression1, RegexOptions.Multiline);
        MatchCollection match2 = match1.Success
            ? Regex.Matches(match1.ToString(), regularExpression2, RegexOptions.Singleline)
            : null;

        // Construct output from query result
        return (match2 == null || match2.Count == 0) ? null : match2.Select(str => int.Parse(str.ToString())).ToList();
    }

    [Test]
    [TestCase(1, 4, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
    [TestCase(1, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(4, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 1, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 4, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 1)]
    [TestCase(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)]
    [TestCase(3, 4, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 7)]
    public void UsingGeometryDistributionNodeStats_GivenVaryingInputGeometry_VerifyNodeAndTriangleCount(
        int instancedMeshStatCnt,
        int instancedMeshStatTriCnt,
        int triangleMeshesStatCnt,
        int triangleMeshesStatTriCnt,
        int trapeziumCount,
        int torusSegmentCount,
        int quadCount,
        int nutCount,
        int generalRingCount,
        int ellipsoidSegmentCount,
        int coneCount,
        int circleCount,
        int boxCount,
        int eccentricConeCount,
        int nodeCount
    )
    {
        // Prepare
        List<CadRevealNode> nodes = GenNodes(
            (instancedMeshStatCnt, instancedMeshStatTriCnt),
            (triangleMeshesStatCnt, triangleMeshesStatTriCnt),
            trapeziumCount,
            torusSegmentCount,
            quadCount,
            nutCount,
            generalRingCount,
            ellipsoidSegmentCount,
            coneCount,
            circleCount,
            boxCount,
            eccentricConeCount,
            nodeCount
        );

        // Act
        var stats = new GeometryDistributionNodeStats(nodes);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(stats.CountInstancedMesh, Is.EqualTo(instancedMeshStatCnt * nodeCount));
            Assert.That(stats.CountTriangleMesh, Is.EqualTo(triangleMeshesStatCnt * nodeCount));
            Assert.That(stats.CountTrapezium, Is.EqualTo(trapeziumCount * nodeCount));
            Assert.That(stats.CountTorusSegment, Is.EqualTo(torusSegmentCount * nodeCount));
            Assert.That(stats.CountQuad, Is.EqualTo(quadCount * nodeCount));
            Assert.That(stats.CountNut, Is.EqualTo(nutCount * nodeCount));
            Assert.That(stats.CountGeneralRing, Is.EqualTo(generalRingCount * nodeCount));
            Assert.That(stats.CountEllipsoidSegment, Is.EqualTo(ellipsoidSegmentCount * nodeCount));
            Assert.That(stats.CountCone, Is.EqualTo(coneCount * nodeCount));
            Assert.That(stats.CountCircle, Is.EqualTo(circleCount * nodeCount));
            Assert.That(stats.CountBox, Is.EqualTo(boxCount * nodeCount));
            Assert.That(stats.CountEccentricCone, Is.EqualTo(eccentricConeCount * nodeCount));

            int sumPrimitiveCount =
                (instancedMeshStatCnt * nodeCount)
                + (triangleMeshesStatCnt * nodeCount)
                + (trapeziumCount * nodeCount)
                + (torusSegmentCount * nodeCount)
                + (quadCount * nodeCount)
                + (nutCount * nodeCount)
                + (generalRingCount * nodeCount)
                + (ellipsoidSegmentCount * nodeCount)
                + (coneCount * nodeCount)
                + (circleCount * nodeCount)
                + (boxCount * nodeCount)
                + (eccentricConeCount * nodeCount);
            Assert.That(stats.SumPrimitiveCount, Is.EqualTo(sumPrimitiveCount));

            Assert.That(
                stats.TriangleCountInInstancedMeshes,
                Is.EqualTo(instancedMeshStatTriCnt * instancedMeshStatCnt * nodeCount)
            );
            Assert.That(
                stats.TriangleCountInTriangleMeshes,
                Is.EqualTo(triangleMeshesStatTriCnt * triangleMeshesStatCnt * nodeCount)
            );
            Assert.That(stats.TriangleCountInTrapeziums, Is.EqualTo(2 * trapeziumCount * nodeCount));
            Assert.That(
                stats.TriangleCountInTorusSegments,
                (torusSegmentCount * nodeCount) > 0 ? Is.GreaterThan(0) : Is.EqualTo(0)
            );
            Assert.That(stats.TriangleCountInQuads, Is.EqualTo(2 * quadCount * nodeCount));
            Assert.That(stats.TriangleCountInNuts, Is.EqualTo(24 * nutCount * nodeCount));
            Assert.That(
                stats.TriangleCountInGeneralRings,
                (generalRingCount * nodeCount) > 0 ? Is.GreaterThan(0) : Is.EqualTo(0)
            );
            Assert.That(stats.TriangleCountInEllipsoidSegments, Is.EqualTo(4 * ellipsoidSegmentCount * nodeCount));
            Assert.That(stats.TriangleCountInCones, (coneCount * nodeCount) > 0 ? Is.GreaterThan(0) : Is.EqualTo(0));
            Assert.That(
                stats.TriangleCountInCircles,
                (circleCount * nodeCount) > 0 ? Is.GreaterThan(0) : Is.EqualTo(0)
            );
            Assert.That(stats.TriangleCountInBoxes, Is.EqualTo(12 * boxCount * nodeCount));
            Assert.That(
                stats.TriangleCountInEccentricCones,
                (eccentricConeCount * nodeCount) > 0 ? Is.GreaterThan(0) : Is.EqualTo(0)
            );

            Assert.That(stats.SumPrimitiveCount, sumPrimitiveCount > 0 ? Is.GreaterThan(0) : Is.EqualTo(0));
        });
    }

    [Test]
    public void UsingGeometryDistributionNodeStats_GivenVaryingInputGeometry_VerifyPrintStatisticsOutput()
    {
        // Prepare
        List<CadRevealNode> nodes = GenNodes((3, 4), (4, 5), 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 7);
        var stats = new GeometryDistributionNodeStats(nodes);

        // Act
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        stats.PrintStatistics();
        var result = stringWriter.ToString().Trim();

        // Assert
        List<int> values = null;
        Assert.Multiple(() =>
        {
            values = ExtractValuesFromTable("Instanced mesh", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountInstancedMesh));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInInstancedMeshes));

            values = ExtractValuesFromTable("Triangle mesh", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountTriangleMesh));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInTriangleMeshes));

            values = ExtractValuesFromTable("Trapezium", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountTrapezium));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInTrapeziums));

            values = ExtractValuesFromTable("Torus segment", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountTorusSegment));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInTorusSegments));

            values = ExtractValuesFromTable("Quad", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountQuad));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInQuads));

            values = ExtractValuesFromTable("Nut", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountNut));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInNuts));

            values = ExtractValuesFromTable("General ring", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountGeneralRing));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInGeneralRings));

            values = ExtractValuesFromTable("Ellipsoid segment", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountEllipsoidSegment));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInEllipsoidSegments));

            values = ExtractValuesFromTable("Cone", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountCone));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInCones));

            values = ExtractValuesFromTable("Circle", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountCircle));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInCircles));

            values = ExtractValuesFromTable("Box", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountBox));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInBoxes));

            values = ExtractValuesFromTable("Eccentric cone", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.CountEccentricCone));
            Assert.That(values[1], Is.EqualTo(stats.TriangleCountInEccentricCones));

            values = ExtractValuesFromTable("SUM", result);
            Assert.That(values, Is.Not.EqualTo(null));
            Assert.That(values[0], Is.EqualTo(stats.SumPrimitiveCount));
            Assert.That(values[1], Is.EqualTo(stats.SumTriangleCount));
        });
    }
}
