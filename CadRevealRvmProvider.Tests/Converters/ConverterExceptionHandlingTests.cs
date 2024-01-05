namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
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

    private readonly FailedPrimitivesLogObject logObject = new FailedPrimitivesLogObject();
    private Vector3 _scale = new Vector3(1, 1, 1);
    private Quaternion _rotation = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f);

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
    }

    [Test]
    public void CanBeConvertedRvmBoxNegativeLength_Bool()
    {
        bool result = _rvmBox.CanBeConverted(_scale, _rotation, logObject);
        Assert.IsFalse(result);
    }

    [Test]
    public void CanBeConvertedRvmCircularTorusNegativeRadius_Bool()
    {
        bool result = _rvmCircularTorus.CanBeConverted(_scale, _rotation, logObject);
        Assert.IsFalse(result);
    }

    [Test]
    public void CanBeConvertedRvmCylinderZeroHeight_Bool()
    {
        bool result = _rvmCylinder.CanBeConverted(_scale, _rotation, logObject);
        Assert.IsTrue(result); // Returns a cap (a circle), so this must be allowed
    }

    [Test]
    public void CanBeConvertedRvmSnoutInfiniteShear_Bool()
    {
        bool result = _rvmSnout.CanBeConverted(_scale, _rotation, logObject);
        Assert.IsFalse(result);
    }

    [Test]
    public void IsScaleValid_Bool()
    {
        Vector3 notValidScale = new Vector3(1, -1, 1);
        bool result = ConverterExceptionHandling.IsScaleValid(notValidScale);
        Assert.IsFalse(result);
    }

    [Test]
    public void IsRotationValid_Bool()
    {
        Quaternion notValidRotation = new Quaternion(0.5f, 0.5f, float.PositiveInfinity, 0.5f);
        bool result = ConverterExceptionHandling.IsRotationValid(notValidRotation);
        Assert.IsFalse(result);
    }
}
