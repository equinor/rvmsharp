namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;
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
        Assert.AreEqual(1, distribution.Boxes);
        Assert.AreEqual(1, distribution.Circles);
        Assert.AreEqual(2, distribution.Cones);
        Assert.AreEqual(1, distribution.EccentricCones);
        Assert.AreEqual(2, distribution.EllipsoidSegments);
        Assert.AreEqual(1, distribution.GeneralCylinders);
        Assert.AreEqual(1, distribution.GeneralRings);
        Assert.AreEqual(2, distribution.InstancedMeshes);
        Assert.AreEqual(1, distribution.Nuts);
        Assert.AreEqual(1, distribution.Quads);
        Assert.AreEqual(1, distribution.TorusSegments);
        Assert.AreEqual(2, distribution.Trapeziums);
        Assert.AreEqual(1, distribution.TriangleMeshes);
    }
}
