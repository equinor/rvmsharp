namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System.Drawing;
using System.Linq;
using System.Numerics;

public class RvmEllipticalDishConverterTests
{
    const int _treeIndex = 1337;
    private RvmEllipticalDish _rvmEllipticalDish;

    [SetUp]
    public void Setup()
    {
        _rvmEllipticalDish = new RvmEllipticalDish(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            BaseRadius: 1,
            Height: 1
        );
    }

    [Test]
    public void RvmEllipticalDishConverter_ReturnsEllipsoidSegmentWithCap()
    {
        var geometries = _rvmEllipticalDish.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<EllipsoidSegment>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(2));
    }
}
