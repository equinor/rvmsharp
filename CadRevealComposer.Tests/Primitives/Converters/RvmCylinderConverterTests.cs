namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System.Drawing;
using System.Linq;
using System.Numerics;

[TestFixture]
public class RvmCylinderConverterTests
{
    const int _treeIndex = 1337;
    private RvmCylinder _rvmCylinder;

    [SetUp]
    public void Setup()
    {
        _rvmCylinder = new RvmCylinder(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Radius: 1,
            Height: 1
        );
    }

    [Test]
    public void RvmCylinderConverter_ReturnsConeWithCaps()
    {
        var geometries = _rvmCylinder.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }
}
