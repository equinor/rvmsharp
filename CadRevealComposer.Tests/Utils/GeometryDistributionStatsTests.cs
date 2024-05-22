namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;
using NUnit.Framework.Legacy;
using Primitives;

public class GeometryDistributionStatsTests
{
    [Test]
    public void Constructor_CountsPrimitivesCorrectly()
    {
        // Arrange
        var primitives = new APrimitive[]
        {
            // ReSharper disable AssignNullToNotNullAttribute -- We only need instances of the types, the data does not matter in the test
            new Box(default, default, default, default),
            new Cone(default, default, default, default, default, default, default, default, default, default),
            new Cone(default, default, default, default, default, default, default, default, default, default),
            new EllipsoidSegment(default, default, default, default, default, default, default, default),
            new EllipsoidSegment(default, default, default, default, default, default, default, default),
            new TriangleMesh(default, default, default, default),
            new InstancedMesh(default, default, default, default, default, default),
            new InstancedMesh(default, default, default, default, default, default),
            new Circle(default, default, default, default, default),
            new Trapezium(default, default, default, default, default, default, default),
            new Trapezium(default, default, default, default, default, default, default),
            new Nut(default, default, default, default),
            new GeneralRing(default, default, default, default, default, default, default, default),
            new Quad(default, default, default, default),
            new GeneralCylinder(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                default
            ),
            new TorusSegment(default, default, default, default, default, default, default),
            new EccentricCone(default, default, default, default, default, default, default, default)
            // ReSharper enable AssignNullToNotNullAttribute
        };

        // Act
        var distribution = new GeometryDistributionStats(primitives);

        // Assert
        Assert.That(distribution.Boxes, Is.EqualTo(1));
        Assert.That(distribution.Circles, Is.EqualTo(1));
        Assert.That(distribution.Cones, Is.EqualTo(2));
        Assert.That(distribution.EccentricCones, Is.EqualTo(1));
        Assert.That(distribution.EllipsoidSegments, Is.EqualTo(2));
        Assert.That(distribution.GeneralCylinders, Is.EqualTo(1));
        Assert.That(distribution.GeneralRings, Is.EqualTo(1));
        Assert.That(distribution.InstancedMeshes, Is.EqualTo(2));
        Assert.That(distribution.Nuts, Is.EqualTo(1));
        Assert.That(distribution.Quads, Is.EqualTo(1));
        Assert.That(distribution.TorusSegments, Is.EqualTo(1));
        Assert.That(distribution.Trapeziums, Is.EqualTo(2));
        Assert.That(distribution.TriangleMeshes, Is.EqualTo(1));
    }
}
