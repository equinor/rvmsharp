namespace CadRevealRvmProvider.Tests.Converters;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;

internal class RvmRectangularTorusConverterTests
{
    const int TreeIndex = 1337;
    private RvmRectangularTorus _rvmRectangularTorus = null!;

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

        var logObject = new FailedPrimitivesLogObject();
        var geometries = torus.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Cone>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[3], Is.TypeOf<GeneralRing>());
        Assert.That(geometries.Length, Is.EqualTo(4));
    }

    [Test]
    public void RvmRectangularTorusConverter_WhenInnerRadiusIsZero_ReturnsOnlyOneConeWithCaps()
    {
        var torus = _rvmRectangularTorus with { Angle = 2 * MathF.PI, RadiusInner = 0 };

        var logObject = new FailedPrimitivesLogObject();
        var geometries = torus.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries.Length, Is.EqualTo(3));
    }

    [Test]
    public void RvmRectangularTorusConverter_WhenAngleIsLessThan2Pi_ReturnsTorusWithCaps()
    {
        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmRectangularTorus.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Cone>());
        Assert.That(geometries[1], Is.TypeOf<Cone>());
        Assert.That(geometries[2], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[3], Is.TypeOf<GeneralRing>());
        Assert.That(geometries[4], Is.TypeOf<Quad>());
        Assert.That(geometries[5], Is.TypeOf<Quad>());
        Assert.That(geometries.Length, Is.EqualTo(6));
    }

    [Test]
    public void RvmRectangularTorusConverter_WhenOuterRadiusIsZero_ReturnEmpty()
    {
        var torus = _rvmRectangularTorus with { RadiusOuter = 0 };

        var logObject = new FailedPrimitivesLogObject();
        var geometries = torus.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries, Is.Empty);
    }

    [Test]
    public void RvmRectangularTorusConverter_CorrectQuads()
    {
        var torus = _rvmRectangularTorus with
        {
            Angle = MathF.PI / 2.0f,
            Height = 5.0f,
            RadiusOuter = 3.0f,
            RadiusInner = 1.0f
        };

        var logObject = new FailedPrimitivesLogObject();
        var geometries = torus.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        var quad1 = (Quad)geometries[4];
        var quad2 = (Quad)geometries[5];

        quad1.InstanceMatrix.DecomposeAndNormalize(out var scale1, out var rotation1, out _);
        quad2.InstanceMatrix.DecomposeAndNormalize(out var scale2, out var rotation2, out _);

        Assert.That(scale1.X, Is.EqualTo(5).Within(0.001f));
        Assert.That(scale1.Y, Is.EqualTo(2).Within(0.001f));
        Assert.That(scale1.Z, Is.EqualTo(0).Within(0.001f));

        Assert.That(scale2.X, Is.EqualTo(5).Within(0.001f));
        Assert.That(scale2.Y, Is.EqualTo(2).Within(0.001f));
        Assert.That(scale2.Z, Is.EqualTo(0).Within(0.001f));

        var (quadNormal1, rotationAngle1) = rotation1.DecomposeQuaternion();
        var (quadNormal2, rotationAngle2) = rotation2.DecomposeQuaternion();

        Assert.That(Vector3.Dot(quadNormal1, quadNormal2), Is.EqualTo(0f).Within((0.001f)));
    }
}
