namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using NUnit.Framework.Legacy;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class ExceptionHandlerConverterTests
{
    private RvmBox _rvmBox = null!;
    private RvmCircularTorus _rvmCircularTorus = null!;
    private RvmCylinder _rvmCylinder = null!;
    private RvmSnout _rvmSnout = null!;
    private RvmEllipticalDish _rvmEllipticalDish = null!;
    private RvmPyramid _rvmPyramid = null!;
    private RvmRectangularTorus _rvmRectangularTorus = null!;
    private RvmSphere _rvmSphere = null!;
    private RvmSphericalDish _rvmSphericalDish = null!;

    private readonly FailedPrimitivesLogObject _logObject = new FailedPrimitivesLogObject();
    private readonly Vector3 _scale = new Vector3(1, 1, 1);
    private readonly Quaternion _rotation = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f);

    [SetUp]
    public void Setup()
    {
        _rvmBox = new RvmBox(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            LengthX: -1,
            LengthY: 1,
            LengthZ: 1
        );
        _rvmCircularTorus = new RvmCircularTorus(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Offset: 0f,
            Radius: -1,
            Angle: MathF.PI // 180 degrees
        );
        _rvmCylinder = new RvmCylinder(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Radius: 1,
            Height: 0f
        );

        _rvmEllipticalDish = new RvmEllipticalDish(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            BaseRadius: 0f,
            Height: 1
        );

        _rvmPyramid = new RvmPyramid(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            BottomX: 1,
            BottomY: 2,
            TopX: 3,
            TopY: 4,
            OffsetX: 5,
            OffsetY: 6,
            Height: 0f
        );

        _rvmRectangularTorus = new RvmRectangularTorus(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            RadiusInner: 1,
            RadiusOuter: 2,
            Height: 1,
            Angle: float.PositiveInfinity
        );

        _rvmSnout = new RvmSnout(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(Vector3.Zero, Vector3.Zero),
            RadiusBottom: 1,
            RadiusTop: 0.1f,
            Height: 2,
            OffsetX: 0,
            OffsetY: 0,
            BottomShearX: float.NegativeInfinity,
            BottomShearY: 0,
            TopShearX: 0,
            TopShearY: 0
        );

        _rvmSphere = new RvmSphere(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Radius: -1
        );

        _rvmSphericalDish = new RvmSphericalDish(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            BaseRadius: 0f,
            Height: 1
        );
    }

    [Test]
    public void InvalidRvmBox()
    {
        bool result = _rvmBox.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidRvmCircularTorus()
    {
        bool result = _rvmCircularTorus.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidRvmCylinder()
    {
        bool result = _rvmCylinder.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.True); // Returns a cap (a circle), so this must be allowed
    }

    [Test]
    public void InvalidRvmEllipticalDish()
    {
        bool result = _rvmEllipticalDish.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidRvmPyramid()
    {
        bool result = _rvmPyramid.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidRvmRectangularTorus()
    {
        bool result = _rvmRectangularTorus.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidRvmSnout()
    {
        bool result = _rvmSnout.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidRvmSphere()
    {
        bool result = _rvmSphere.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidRvmSphericalDish()
    {
        bool result = _rvmSphericalDish.CanBeConverted(_scale, _rotation, _logObject);
        Assert.That(result, Is.False);
    }

    [Test]
    public void InvalidIsScaleValid()
    {
        Vector3 notValidScale = new Vector3(1, -1, 1);
        bool result = ConverterExceptionHandling.IsScaleValid(notValidScale);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidIsScaleValid()
    {
        bool result = ConverterExceptionHandling.IsScaleValid(_scale);
        Assert.That(result, Is.True);
    }

    [Test]
    public void InvalidIsRotationValid()
    {
        Quaternion notValidRotation = new Quaternion(0.5f, 0.5f, float.PositiveInfinity, 0.5f);
        bool result = ConverterExceptionHandling.IsRotationValid(notValidRotation);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidIsRotationValid()
    {
        bool result = ConverterExceptionHandling.IsRotationValid(_rotation);
        Assert.That(result, Is.True);
    }
}
