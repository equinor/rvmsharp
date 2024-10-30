namespace CadRevealComposer.Tests.Operations;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Operations;
using Primitives;

[TestFixture]
public class CameraPositioningTests
{
    [Test]
    public void InitialCameraPositionLongestAxisX()
    {
        APrimitive[] geometries = { new TestPrimitiveWithBoundingBox(new Vector3(0, 0, 0), new Vector3(100, 50, 100)) };

        var (position, target, direction) = CameraPositioning.CalculateInitialCamera(geometries);
        Assert.That(position, Is.EqualTo(new SerializableVector3(50, -103.56537f, 124.22725f)));
        Assert.That(target, Is.EqualTo(new SerializableVector3(50, 25, 50)));
        Assert.That(direction, Is.EqualTo(new SerializableVector3(0, 0.8660254f, -0.5f)));
    }

    [Test]
    public void InitialCameraPositionLongestAxisY()
    {
        APrimitive[] geometries = { new TestPrimitiveWithBoundingBox(new Vector3(0, 0, 0), new Vector3(50, 100, 100)) };

        var (position, target, direction) = CameraPositioning.CalculateInitialCamera(geometries);
        Assert.That(position, Is.EqualTo(new SerializableVector3(-103.56537f, 50, 124.22725f)));
        Assert.That(target, Is.EqualTo(new SerializableVector3(25, 50, 50)));
        Assert.That(direction, Is.EqualTo(new SerializableVector3(0.8660254f, 0, -0.5f)));
    }

    private record TestPrimitiveWithBoundingBox(Vector3 Min, Vector3 Max)
        : APrimitive(int.MaxValue, Color.Red, new BoundingBox(Min, Max), 0, null);
}
