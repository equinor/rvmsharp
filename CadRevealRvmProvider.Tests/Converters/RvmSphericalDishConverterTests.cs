namespace CadRevealRvmProvider.Tests.Converters;

using System.Numerics;
using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;

public class RvmSphericalDishConverterTests
{
    const int TreeIndex = 1337;
    private RvmSphericalDish _rvmSphericalDish = null!;

    [SetUp]
    public void Setup()
    {
        _rvmSphericalDish = new RvmSphericalDish(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            BaseRadius: 1,
            Height: 1
        );
    }

    [Test]
    public void RvmSphericalDishConverter_ReturnsEllipsoidSegmentWithCap()
    {
        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmSphericalDish
            .ConvertToRevealPrimitive(TreeIndex, System.Drawing.Color.Red, logObject)
            .ToArray();

        Assert.That(geometries.Length, Is.EqualTo(2));
        Assert.That(geometries[0], Is.TypeOf<EllipsoidSegment>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
    }
}
