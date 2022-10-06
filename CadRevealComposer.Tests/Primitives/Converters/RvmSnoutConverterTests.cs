namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System;
using System.Drawing;
using System.Linq;
using System.Numerics;

[TestFixture]
public class RvmSnoutConverterTests
{
    private const int _treeIndex = 1337;
    private static RvmSnout _rvmSnout;

    [SetUp]
    public void Setup()
    {
        _rvmSnout = new RvmSnout(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(Vector3.Zero, Vector3.Zero),
            RadiusBottom: 1,
            RadiusTop: 0.1f,
            Height: 2,
            OffsetX: 0,
            OffsetY: 0,
            BottomShearX: 0,
            BottomShearY: 0,
            TopShearX: 0,
            TopShearY: 0);
    }

    [Test]
    public void RvmSnoutConverter_ReturnsConeWithCaps()
    {
        var geometries = _rvmSnout.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }

    [Test]
    public void RvmSnoutConverter_WhenHasShearAndIsEccentric_ThrowException()
    {
        var snout = _rvmSnout with { BottomShearX = 0.5f, OffsetX = 0.5f };

        Assert.Throws<NotImplementedException>(
            delegate { snout.ConvertToRevealPrimitive(_treeIndex, Color.Red); });
    }

    [Test]
    public void RvmSnoutConverter_WhenHasShearAndCylinderShaped_ReturnsCylinderWithCaps()
    {

        _rvmSnout = _rvmSnout with { RadiusBottom = 1, RadiusTop = 1, BottomShearX = 0.5f };

        var geometries = _rvmSnout.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<GeneralCylinder>());
        Assert.That(geometries[1], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }

    [Test]
    public void RvmSnoutConverter_WhenNoShearAndEccentric_ReturnsEccentricConeWithCaps()
    {
        var snout = _rvmSnout with { OffsetX = 0.5f };

        var geometries = snout.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<EccentricCone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }


    [Test]
    public void RvmSnoutConverter_WhenNoShearAndNotEccentric_ReturnsConeWithCaps()
    {
        var geometries = _rvmSnout.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }
}