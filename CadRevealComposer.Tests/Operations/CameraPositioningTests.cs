namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class CameraPositioningTests
{
    [Test]
    public void InitialCameraPositionLongestAxisX()
    {
        APrimitive[] geometries = {
            new TestPrimitiveWithBoundingBox(
                new Vector3(0, 0, 0),
                new Vector3(100, 50, 100)
            )
        };

        var (position, target, direction) = CameraPositioning.CalculateInitialCamera(geometries);
        Assert.AreEqual(new Vector3(50, -103.56537f, 124.22725f), position);
        Assert.AreEqual(new Vector3(50, 25, 50), target);
        Assert.AreEqual(new Vector3(0, 0.8660254f, -0.5f), direction);
    }

    [Test]
    public void InitialCameraPositionLongestAxisY()
    {
        APrimitive[] geometries = {
            new TestPrimitiveWithBoundingBox(
                new Vector3(0, 0, 0),
                new Vector3(50, 100, 100)
            )
        };

        var (position, target, direction) = CameraPositioning.CalculateInitialCamera(geometries);
        Assert.AreEqual(new Vector3(-103.56537f, 50, 124.22725f), position);
        Assert.AreEqual(new Vector3(25, 50, 50), target);
        Assert.AreEqual(new Vector3(0.8660254f, 0, -0.5f), direction);
    }

    private record TestPrimitiveWithBoundingBox(Vector3 Min, Vector3 Max)
        : APrimitive(int.MaxValue, Color.Red, new RvmBoundingBox(Min, Max));
}