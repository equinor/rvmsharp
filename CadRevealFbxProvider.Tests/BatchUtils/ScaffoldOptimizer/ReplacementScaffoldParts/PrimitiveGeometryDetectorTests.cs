namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

public class PrimitiveGeometryDetectorTests
{
    private static Mesh GenCylinder(Vector3 pos, Vector3 centralAxis, float rMinor, float rMajor, float height)
    {
        var u1 = centralAxis;
        var u2 = Vector3.Cross(new Vector3(1, 0, 0), u1);
        var u3 = Vector3.Cross(u2, u1);

        u1 = Vector3.Normalize(u1); // Cylinder center axis unit vector
        u2 = Vector3.Normalize(u2); // Cylinder cap axis1 unit vector
        u3 = Vector3.Normalize(u3); // Cylinder cap axis2 unit vector

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

    [Test]
    public void Invoke_WhenCylindricalPointCloud_ThenDetectAsCylinderWithCorrectRadiusAndHeight()
    {
        // Arrange
        var pos = new Vector3(2.3f, 9.5f, 1.4f);
        var centralAxis = new Vector3(1.2f, 3.4f, 8.2f);
        const float radius = 5.0f;
        const float height = 20.0f;
        Mesh mesh = GenCylinder(pos, centralAxis, radius, radius, height);
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
            Assert.That(geometryDetector.CylinderRadiusMinor, Is.EqualTo(radius).Within(0.1f));
            Assert.That(geometryDetector.CylinderRadiusMajor, Is.EqualTo(radius).Within(0.1f));
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
    public void Invoke_WhenFlattenedCylindricalPointCloud_ThenDetectAsCylinderWithCorrectRadiusAndHeight()
    {
        // Arrange
        var pos = new Vector3(2.3f, 9.5f, 1.4f);
        var centralAxis = new Vector3(1.2f, 3.4f, 8.2f);
        const float radiusMinor = 1.0f;
        const float radiusMajor = 4.3f;
        const float height = 20.0f;
        Mesh mesh = GenCylinder(pos, centralAxis, radiusMinor, radiusMajor, height);
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
            Assert.That(geometryDetector.CylinderRadiusMinor, Is.EqualTo(radiusMinor).Within(0.1f));
            Assert.That(geometryDetector.CylinderRadiusMajor, Is.EqualTo(radiusMajor).Within(0.1f));
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
}
