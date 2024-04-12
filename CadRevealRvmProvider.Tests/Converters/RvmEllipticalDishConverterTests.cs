namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

public class RvmEllipticalDishConverterTests
{
    const int TreeIndex = 1337;
    private RvmEllipticalDish _rvmEllipticalDish = null!;

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
        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmEllipticalDish.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<EllipsoidSegment>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(2));
    }
}
