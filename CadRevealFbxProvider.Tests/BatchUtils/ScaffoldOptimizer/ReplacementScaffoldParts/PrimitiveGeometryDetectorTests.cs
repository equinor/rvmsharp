namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

public class PrimitiveGeometryDetectorTests
{
    private static (Vector3 u1, Vector3 u2, Vector3 u3) GenAxesSystem(Vector3 centralAxis)
    {
        var u1 = centralAxis;
        var u2 = Vector3.Cross(new Vector3(1, 0, 0), u1);
        var u3 = Vector3.Cross(u2, u1);

        u1 = Vector3.Normalize(u1); // Primary axis unit vector
        u2 = Vector3.Normalize(u2); // Perpendicular axis unit vector 1
        u3 = Vector3.Normalize(u3); // Perpendicular axis unit vector 1

        return (u1, u2, u3);
    }

    private static Mesh GenCylinder(Vector3 pos, Vector3 centralAxis, float rMinor, float rMajor, float height)
    {
        (Vector3 u1, Vector3 u2, Vector3 u3) = GenAxesSystem(centralAxis);

        var cylinderRingPositions = new List<float>() { 0, height };

        var vertices = new List<Vector3>();
        var indices = new List<uint>();
        uint index = 0;
        foreach (float ringPosition in cylinderRingPositions)
        {
            for (float theta = 0; theta < 2.0 * Math.PI; theta += 0.1f)
            {
                Vector3 p =
                    pos
                    + ringPosition * u1
                    + rMajor * (float)Math.Cos(theta) * u2
                    + rMinor * (float)Math.Sin(theta) * u3;
                vertices.Add(p);
                indices.Add(index++);
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), 1.0E-3f);
    }

    private static Mesh GenEllipsoid(Vector3 pos, Vector3 centralAxis, float rMinor, float rSemiMajor, float rMajor)
    {
        (Vector3 u1, Vector3 u2, Vector3 u3) = GenAxesSystem(centralAxis);

        var vertices = new List<Vector3>();
        var indices = new List<uint>();
        uint index = 0;
        for (float phi = 0; phi < 2.0f * Math.PI; phi += 0.1f)
        {
            for (float theta = 0; theta < 2.0 * Math.PI; theta += 0.1f)
            {
                Vector3 p =
                    pos
                    + rSemiMajor * (float)Math.Sin(phi) * u2
                    + rMajor * (float)Math.Cos(theta) * (float)Math.Cos(phi) * u1
                    + rMinor * (float)Math.Sin(theta) * (float)Math.Cos(phi) * u3;
                vertices.Add(p);
                indices.Add(index++);
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), 1.0E-3f);
    }

    private static Mesh GenCuboid(Vector3 pos, Vector3 centralAxis, float length, float depth, float height)
    {
        (Vector3 u1, Vector3 u2, Vector3 u3) = GenAxesSystem(centralAxis);

        var vertices = new List<Vector3>();
        var indices = new List<uint>();
        uint index = 0;

        // Generate vertices for the cuboid
        for (float x = -length / 2; x <= length / 2; x += length)
        {
            for (float y = -depth / 2; y <= depth / 2; y += depth)
            {
                for (float z = -height / 2; z <= height / 2; z += height)
                {
                    Vector3 p = pos + x * u1 + y * u2 + z * u3;
                    vertices.Add(p);
                    indices.Add(index++);
                }
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), 1.0E-3f);
    }

    [Test]
    [TestCase(1.0f, 4.3f, 20.0f)]
    [TestCase(5.0f, 5.0f, 20.0f)]
    public void Invoke_WhenFlattenedCylindricalPointCloud_ThenDetectAsCylinderWithCorrectRadiusAndHeight(float rMinor, float rMajor, float height)
    {
        // Arrange
        var pos = new Vector3(2.3f, 9.5f, 1.4f);
        var centralAxis = new Vector3(1.2f, 3.4f, 8.2f);
        Mesh mesh = GenCylinder(pos, centralAxis, rMinor, rMajor, height);
        Vector3 centerPosition = mesh.Vertices.Aggregate(Vector3.Zero, (acc, x) => x + acc) / mesh.Vertices.Length;

        // Act
        var geometryDetector = new PrimitiveGeometryDetector(mesh);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                geometryDetector.DetectedGeometry,
                Is.EqualTo(PrimitiveGeometryDetector.PrimitiveGeometry.Cylinder)
            );
            Assert.That(geometryDetector.CylinderRadiusMinor, Is.EqualTo(rMinor).Within(0.1f));
            Assert.That(geometryDetector.CylinderRadiusMajor, Is.EqualTo(rMajor).Within(0.1f));
            Assert.That(geometryDetector.CylinderHeight, Is.EqualTo(height).Within(0.1));
            Assert.That(geometryDetector.CenterPosition.X, Is.EqualTo(centerPosition.X).Within(0.1f));
            Assert.That(geometryDetector.CenterPosition.Y, Is.EqualTo(centerPosition.Y).Within(0.1f));
            Assert.That(geometryDetector.CenterPosition.Z, Is.EqualTo(centerPosition.Z).Within(0.1f));
            Assert.That(
                Vector3.Cross(geometryDetector.MajorAxis, centralAxis).Length(),
                Is.EqualTo(0.0f).Within(1.0E-3f)
            );
            Assert.That(
                Vector3.Dot(geometryDetector.SemiMajorAxis, geometryDetector.MajorAxis),
                Is.EqualTo(0.0f).Within(1.0E-3f)
            );
        });
    }

    [Test]
    [TestCase(4.3f, 4.3f, 4.3f, false)]
    [TestCase(4.3f, 4.3f, 7.8f, true)]
    [TestCase(2.3f, 4.3f, 7.8f, true)]
    public void Invoke_WhenEllipsoidPointCloud_ThenDetectAsEllipsoidWithCorrectRadii(float rMinor, float rSemiMajor, float rMajor, bool requireMajorAxesAlignment)
    {
        // Arrange
        var pos = new Vector3(2.3f, 9.5f, 1.4f);
        var centralAxis = new Vector3(1.2f, 3.4f, 8.2f);
        Mesh mesh = GenEllipsoid(pos, centralAxis, rMinor, rSemiMajor, rMajor);
        Vector3 centerPosition = mesh.Vertices.Aggregate(Vector3.Zero, (acc, x) => x + acc) / mesh.Vertices.Length;

        // Act
        var geometryDetector = new PrimitiveGeometryDetector(mesh);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                geometryDetector.DetectedGeometry,
                Is.EqualTo(PrimitiveGeometryDetector.PrimitiveGeometry.Ellipsoid)
            );
            Assert.That(geometryDetector.EllipsoidRadiusMinor, Is.EqualTo(rMinor).Within(0.1f));
            Assert.That(geometryDetector.EllipsoidRadiusSemiMajor, Is.EqualTo(rSemiMajor).Within(0.1f));
            Assert.That(geometryDetector.EllipsoidRadiusMajor, Is.EqualTo(rMajor).Within(0.1));
            Assert.That(geometryDetector.CenterPosition.X, Is.EqualTo(centerPosition.X).Within(0.1f));
            Assert.That(geometryDetector.CenterPosition.Y, Is.EqualTo(centerPosition.Y).Within(0.1f));
            Assert.That(geometryDetector.CenterPosition.Z, Is.EqualTo(centerPosition.Z).Within(0.1f));

            if (requireMajorAxesAlignment)
            {
                Assert.That(
                    Vector3.Cross(geometryDetector.MajorAxis, centralAxis).Length(),
                    Is.EqualTo(0.0f).Within(5.0E-3f)
                );
                Assert.That(
                    Vector3.Dot(geometryDetector.SemiMajorAxis, geometryDetector.MajorAxis),
                    Is.EqualTo(0.0f).Within(5.0E-3f)
                );
            }
        });
    }

    [Test]
//    [TestCase(8.7f, 5.7f, 4.7f)]
//    [TestCase(8.7f, 4.7f, 4.7f)]
    [TestCase(4.7f, 4.7f, 4.7f)]
    public void Invoke_WhenCuboidPointCloud_ThenDetectAsCuboidWithCorrectSize(float length, float depth, float height)
    {
        // Arrange
        var pos = new Vector3(2.3f, 9.5f, 1.4f);
        var centralAxis = new Vector3(1.2f, 3.4f, 8.2f);
        Mesh mesh = GenCuboid(pos, centralAxis, length, depth, height);
        Vector3 centerPosition = mesh.Vertices.Aggregate(Vector3.Zero, (acc, x) => x + acc) / mesh.Vertices.Length;

        // Act
        var geometryDetector = new PrimitiveGeometryDetector(mesh);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                geometryDetector.DetectedGeometry,
                Is.EqualTo(PrimitiveGeometryDetector.PrimitiveGeometry.Cuboid)
            );
            Assert.That(geometryDetector.CuboidLongestEdgeLength, Is.EqualTo(length).Within(0.1f));
            Assert.That(geometryDetector.CuboidIntermediateEdgeLength, Is.EqualTo(depth).Within(0.1f).Or.EqualTo(height).Within(0.1f));
            Assert.That(geometryDetector.CuboidShortestEdgeLength, Is.EqualTo(depth).Within(0.1f).Or.EqualTo(height).Within(0.1f));
            Assert.That(geometryDetector.CenterPosition.X, Is.EqualTo(centerPosition.X).Within(0.1f));
            Assert.That(geometryDetector.CenterPosition.Y, Is.EqualTo(centerPosition.Y).Within(0.1f));
            Assert.That(geometryDetector.CenterPosition.Z, Is.EqualTo(centerPosition.Z).Within(0.1f));
        });
    }
}
