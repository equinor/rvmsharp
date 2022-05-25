namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System.Drawing;
using System.Linq;
using System.Numerics;

[TestFixture]
public class RvmSnoutConverterTests
{
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
    public void ConvertToRevealPrimitive()
    {
        var geometries = _rvmSnout
            .ConvertToRevealPrimitive(1337, Color.Red)
            .ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
    }

    [Test]
    public void ConvertToRevealPrimitive_WhenConnections_ProducesClosedCone()
    {
        _rvmSnout.Connections[0] = new RvmConnection(_rvmSnout, _rvmSnout, 0, 0, Vector3.One, Vector3.UnitZ,
            RvmConnection.ConnectionType.HasCircularSide);

        var geometries = _rvmSnout
            .ConvertToRevealPrimitive(1337, Color.Red)
            .ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
    }

    [Test]
    [Ignore("Case Not implemented yet")]
    public void ConvertToRevealPrimitive_WhenOffset_ProducesCorrectPrimitive()
    {
        var rvmSnoutWithOffset = _rvmSnout with {OffsetX = 1};
        var cone = rvmSnoutWithOffset.ConvertToRevealPrimitive(1337, Color.Red);
        Assert.That(cone, Is.Not.Null);
    }
}