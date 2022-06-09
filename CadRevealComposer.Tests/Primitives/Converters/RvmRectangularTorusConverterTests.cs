namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System;
using System.Linq;
using System.Numerics;

internal class RvmRectangularTorusConverterTests
{
    const int _treeIndex = 1337;
    private RvmRectangularTorus _rvmRectangularTorus;

    [SetUp]
    public void Setup()
    {
        _rvmRectangularTorus = new RvmRectangularTorus(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            RadiusInner: 1,
            RadiusOuter: 2,
            Height: 1,
            Angle: MathF.PI // 180 degrees
        );
    }

    [Test]
    public void RvmRectangularTorusConverter_WhenAngleIs2Pi_ReturnsTorusWithoutCaps()
    {
        var torus = _rvmRectangularTorus with { Angle = 2 * MathF.PI };

        var geometries = torus.ConvertToRevealPrimitive(_treeIndex, System.Drawing.Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Cone>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[3], Is.TypeOf<GeneralRing>());
        Assert.That(geometries.Length, Is.EqualTo(4));
    }

    public void RvmRectangularTorusConverter_WhenInnerRadiusIsZero_ReturnsOnlyOneConeWithCaps()
    {
        var torus = _rvmRectangularTorus with { Angle = 2 * MathF.PI, RadiusInner = 0 };

        var geometries = torus.ConvertToRevealPrimitive(_treeIndex, System.Drawing.Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }

    public void RvmRectangularTorusConverter_WhenAngleIsLessThan2Pi_ReturnsTorusWithCaps()
    {
        var geometries = _rvmRectangularTorus.ConvertToRevealPrimitive(_treeIndex, System.Drawing.Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Cone>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[3], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[4], Is.TypeOf<Trapezium>());
        Assert.That(geometries[5], Is.TypeOf<Trapezium>());
        Assert.That(geometries.Length, Is.EqualTo(6));
    }
}
