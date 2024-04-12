namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class RvmSnoutConverterTests
{
    private const int TreeIndex = 1337;
    private static RvmSnout _rvmSnout = null!;

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
            TopShearY: 0
        );
    }

    [Test]
    public void RvmSnoutConverter_ReturnsConeWithCaps()
    {
        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmSnout.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

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
            delegate
            {
                var logObject = new FailedPrimitivesLogObject();
                snout.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject);
            }
        );
    }

    [Test]
    public void RvmSnoutConverter_WhenHasShearAndCylinderShaped_ReturnsCylinderWithCaps()
    {
        _rvmSnout = _rvmSnout with { RadiusBottom = 1, RadiusTop = 1, BottomShearX = 0.5f };

        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmSnout.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<GeneralCylinder>());
        Assert.That(geometries[1], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }

    [Test]
    public void RvmSnoutConverter_WhenNoShearAndEccentric_ReturnsEccentricConeWithCaps()
    {
        var snout = _rvmSnout with { OffsetX = 0.5f };

        var logObject = new FailedPrimitivesLogObject();
        var geometries = snout.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<EccentricCone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }

    [Test]
    public void RvmSnoutConverter_WhenNoShearAndNotEccentric_ReturnsConeWithCaps()
    {
        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmSnout.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Circle>());
        Assert.That(geometries[2], Is.TypeOf<Circle>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }
}
