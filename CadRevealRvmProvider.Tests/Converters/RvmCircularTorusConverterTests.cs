namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class RvmCircularTorusConverterTests
{
    private RvmCircularTorus _rvmCircularTorus = null!;

    [SetUp]
    public void Setup()
    {
        _rvmCircularTorus = new RvmCircularTorus(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Offset: 0f,
            Radius: 1,
            Angle: MathF.PI // 180 degrees
        );
    }

    [Test]
    public void RvmCircularConverter_WhenAngleIs2Pi_ReturnsTorusWithoutCaps()
    {
        var logObject = new FailedPrimitivesLogObject();
        var torus = _rvmCircularTorus with { Angle = 2 * MathF.PI };
        var geometries = torus.ConvertToRevealPrimitive(1337, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<TorusSegment>());
        Assert.That(geometries.Count, Is.EqualTo(1));
    }

    [Test]
    public void RvmCircularConverter_WhenAngleIsLessThan2Pi_ReturnsTorusWithCaps()
    {
        var torus = _rvmCircularTorus with { Angle = MathF.PI };
        var geometries = torus.ConvertToRevealPrimitive(1337, Color.Red, null).ToArray();

        Assert.That(geometries[0], Is.TypeOf<TorusSegment>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }
}
