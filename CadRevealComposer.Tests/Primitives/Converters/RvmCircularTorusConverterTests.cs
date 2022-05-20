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
public class RvmCircularTorusConverterTests
{
    private RvmCircularTorus _rvmCircularTorus;

    [SetUp]
    public void Setup()
    {
        _rvmCircularTorus = new RvmCircularTorus(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One,Vector3.One ),
            Offset: 0f,
            Radius: 1,
            Angle: MathF.PI // 180 degrees
        );
    }

    [Test]
    public void RvmCircularConverter_WhenAngleIs2Pi_ReturnsTorus()
    {
        var torus = _rvmCircularTorus with {Angle = 2 * MathF.PI};
        var primitive = torus.ConvertToRevealPrimitive(1337, Color.Red).SingleOrDefault();
        Assert.That(primitive, Is.TypeOf<TorusSegment>());
    }

    [Test]
    public void RvmCircularConverter_WhenNotCompleteAndNoConnections_ReturnsClosedTorusSegment()
    {
        var angle = MathF.PI;
        var torus = _rvmCircularTorus with {Angle = angle};
        var primitive = torus.ConvertToRevealPrimitive(1337, Color.Red).SingleOrDefault();
        Assert.That(primitive, Is.TypeOf<TorusSegment>());

        var closedTorusSegment = (TorusSegment) primitive;
        Assert.That(closedTorusSegment.ArcAngle, Is.EqualTo(angle).Within(0.001));
    }

    [Test]
    public void RvmCircularConverter_WhenNotCompleteAndHasConnection_ReturnsClosedTorusSegment()
    {
        var angle = MathF.PI;
        var torus = _rvmCircularTorus with {Angle = angle};
        torus.Connections[0] = new RvmConnection(torus, torus, 0, 0, Vector3.Zero, Vector3.UnitZ,
            RvmConnection.ConnectionType.HasCircularSide);
        var primitive = torus.ConvertToRevealPrimitive(1337, Color.Red).SingleOrDefault();

        Assert.That(primitive, Is.TypeOf<TorusSegment>());

        var closedTorusSegment = (TorusSegment) primitive;
        Assert.That(closedTorusSegment.ArcAngle, Is.EqualTo(angle).Within(0.001));
    }
}