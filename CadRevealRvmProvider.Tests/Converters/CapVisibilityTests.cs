namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealRvmProvider.Converters.CapVisibilityHelpers;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using System.Numerics;
using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;

[TestFixture]
public class CapVisibilityTests
{
    private RvmCylinder _cylinder = null!;
    private RvmSnout _snout = null!;
    private RvmCircularTorus _circularTorus = null!;
    private RvmSphericalDish _sphericalDish = null!;
    private RvmEllipticalDish _ellipticalDish = null!;

    private RvmBoundingBox _defaultRvmBoundingBox = null!;

    [SetUp]
    public void Setup()
    {
        _defaultRvmBoundingBox = new RvmBoundingBox(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f));

        _cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1);
        _snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);
        _circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, MathF.PI / 4);
        _sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1);
        _ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1);
    }

    /////// CYLINDER ///////
    [Test]
    public void CalculateCapVisibility_BoxHidesCapAOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 2, 2, 2);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            box,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_BoxHidesCapBOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 2, 2, 2);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            box,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallBoxDoesNotHideCapsOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 0.5f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            box,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.4f, 0);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SphericalDishHidesCapAOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            sphericalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SphericalDishHidesCapBOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            sphericalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSphericalDishDoesNotHideCapsOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            sphericalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapAOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            ellipticalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapBOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            ellipticalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallEllipticalDishDoesNotHideCapsOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            ellipticalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapAOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapBOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 0.4f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(
            _cylinder,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// SNOUT ///////
    [Test]
    public void CalculateCapVisibility_BoxHidesCapAOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 2, 2, 2);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            box,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_BoxHidesCapBOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 2, 2, 2);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            box,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallBoxDoesNotHideCapsOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 0.5f);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            box,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.4f, 0);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapAOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            ellipticalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapBOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            ellipticalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallEllipticalDishDoesNotHideCapsOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            ellipticalDish,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutDishHidesCapAOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutDishHidesCapBOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.0001)]
    public void CalculateCapVisibility_TestSnoutCapMatch_Cylinder()
    {
        var snout1 = new RvmSnout(
            0,
            Matrix4x4.Identity,
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            2.0f, // top radius
            4.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            -MathF.PI / 4.0f, // bottom shear y
            0.0f, // top shear x
            -MathF.PI / 4.0f // top shear y
        );
        var topCap = snout1.GetTopCapEllipse();

        var transform = Matrix4x4.CreateTranslation(0.0f, 0.0f, 4.0f);
        var snout2 = new RvmSnout(
            0,
            transform,
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            2.0f, // top radius
            4.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            -MathF.PI / 4.0f, // bottom shear y
            0.0f, // top shear x
            -MathF.PI / 4.0f // top shear y
        );
        var bottomCap = snout1.GetBottomCapEllipse();

        Assert.That(topCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0f / MathF.Cos(snout1.TopShearY)));
        Assert.That(topCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0f));

        Assert.That(bottomCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0 / MathF.Cos(snout1.TopShearY)));
        Assert.That(bottomCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0));

        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var snout1CapCenterB = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var (snout1N, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX,
            snout1.TopShearY,
            snout1CapCenter
        );

        snout1.Connections[0] = new RvmConnection(
            snout1,
            snout2,
            1,
            0,
            snout1CapCenter,
            snout1N,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.0001)]
    public void CalculateCapVisibility_TestSnoutCapMatch_RotatedCylinders()
    {
        var transform1 = Matrix4x4.CreateTranslation(0.0f, 0.0f, 0.0f);
        // var rotate = Matrix4x4.CreateRotationX(0.1f);
        var snout1 = new RvmSnout(
            0,
            transform1,
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            2.0f, // top radius
            4.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            -MathF.PI / 4.0f, // bottom shear y
            0.0f, // top shear x
            -MathF.PI / 4.0f // top shear y
        );
        var topCap = snout1.GetTopCapEllipse();

        var transform2 = Matrix4x4.CreateTranslation(0.0f, 0.0f, 4.0f);
        var snout2 = new RvmSnout(
            0,
            transform2,
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            2.0f, // top radius
            4.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            -MathF.PI / 4.0f, // bottom shear y
            0.0f, // top shear x
            -MathF.PI / 4.0f // top shear y
        );
        var bottomCap = snout2.GetBottomCapEllipse();

        Assert.That(topCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0f / MathF.Cos(snout1.TopShearY)));
        Assert.That(topCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0f));

        Assert.That(bottomCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0 / MathF.Cos(snout2.BottomShearY)));
        Assert.That(bottomCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0));

        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var snout1CapCenterB = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var (snout1N, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX,
            snout1.TopShearY,
            snout1CapCenter
        );

        snout1.Connections[0] = new RvmConnection(
            snout1,
            snout2,
            1,
            0,
            snout1CapCenter,
            snout1N,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.01)]
    public void CalculateCapVisibility_TestSnoutCapMatch_TwoSnouts()
    {
        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.001f,
                0.0f,
                0.0f,
                0.0008660254f,
                0.0f,
                0.0005f,
                0.0f,
                0.0005f,
                0.0f,
                -0.0008660254f,
                0.0f,
                75.08398f,
                289.475f,
                35.4f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-500.0f, -500.0f, 160.0475f), new Vector3(500.0f, 500.0f, 695.9459f)),
            500, // bottom radius
            500.0f, // top radius
            535.8984f, // height
            0.0f, // offset x
            0.0f, // offset y
            0, // bottom shear x
            0.2617994f, // bottom shear y
            0.0f, // top shear x
            -0.2617994f // top shear y
        );
        var ellipse1 = snout1.GetBottomCapEllipse();

        var snout2 = new RvmSnout(
            1,
            new Matrix4x4(
                0.001f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                74.95f,
                289.475f,
                35.7660255f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-500.0f, -500.0f, -133.9746f), new Vector3(500.0f, 500.0f, 561.9713f)),
            500.0f, // bottom radius
            500.0f, // top radius
            267.9492f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            -0.2617994f, // top shear x
            0.0f // top shear y
        );
        var ellipse2 = snout2.GetTopCapEllipse();

        var origin = VectorD.Build.Dense([0.0, 0.0, 0.0, 1.0]);
        var p1El1 = VectorD.Build.Dense([ellipse1.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0]);
        var p2El1 = VectorD.Build.Dense([0.0, ellipse1.ellipse2DPolar.semiMinorAxis, 0.0, 1.0]);
        var p1El2 = VectorD.Build.Dense([ellipse2.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0]);
        var p2El2 = VectorD.Build.Dense([0.0, ellipse2.ellipse2DPolar.semiMinorAxis, 0.0, 1.0]);

        var ptOrigEll1 = ellipse1.planeToModelCoord * origin;
        var ptOrigEll2 = ellipse2.planeToModelCoord * origin;
        var ptPt1Ell1 = ellipse1.planeToModelCoord * p1El1;
        var ptPt2Ell1 = ellipse1.planeToModelCoord * p2El1;
        var ptPt1Ell2 = ellipse2.planeToModelCoord * p1El2;
        var ptPt2Ell2 = ellipse2.planeToModelCoord * p2El2;

        // snout1 -> bottom,
        var snout1CapCenter = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        //var snout1TopCapCenter4D = new Vector4(snout1.OffsetX, snout1.OffsetY, snout1.Height, 1.0f);
        var (snout1N, snout1Dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.BottomShearX,
            snout1.BottomShearY,
            snout1CapCenter
        );
        var snout1N4D = new Vector4(snout1N.X, snout1N.Y, snout1N.Z, 0.0f);

        var distance10 =
            snout1N.X * ptOrigEll1[0] + snout1N.Y * ptOrigEll1[1] + snout1N.Z * ptOrigEll1[2] + snout1Dc;
        var distance11 =
            snout1N.X * ptPt1Ell1[0] + snout1N.Y * ptPt1Ell1[1] + snout1N.Z * ptPt1Ell1[2] + snout1Dc;
        var distance12 =
            snout1N.X * ptPt2Ell1[0] + snout1N.Y * ptPt2Ell1[1] + snout1N.Z * ptPt2Ell1[2] + snout1Dc;

        // snout2 -> top,
        var snout2CapCenter = 0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        var snout2CapCenterB = -0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        var (snout2N, snout2Dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.TopShearX,
            snout2.TopShearY,
            snout2CapCenter
        );
        var snout2N4D = new Vector4(snout2N.X, snout2N.Y, snout2N.Z, 0.0f);

        var distance20 =
            snout2N.X * ptOrigEll2[0] + snout2N.Y * ptOrigEll2[1] + snout2N.Z * ptOrigEll2[2] + snout2Dc;
        var distance21 =
            snout2N.X * ptPt1Ell2[0] + snout2N.Y * ptPt1Ell2[1] + snout2N.Z * ptPt1Ell2[2] + snout2Dc;
        var distance22 =
            snout2N.X * ptPt2Ell2[0] + snout2N.Y * ptPt2Ell2[1] + snout2N.Z * ptPt2Ell2[2] + snout2Dc;

        var v4TransfNormal1 = Vector4.Transform(snout1N4D, snout1.Matrix);
        var v4TransfNormal2 = Vector4.Transform(snout2N4D, snout2.Matrix);
        var transfNormal1 = new Vector3(v4TransfNormal1.X, v4TransfNormal1.Y, v4TransfNormal1.Z);
        var transfNormal2 = new Vector3(v4TransfNormal2.X, v4TransfNormal2.Y, v4TransfNormal2.Z);
        transfNormal1 = Vector3.Normalize(transfNormal1);
        transfNormal2 = Vector3.Normalize(transfNormal2);

        Assert.AreEqual(Vector3.Dot(transfNormal1, transfNormal2), 1.0f, 0.001);

        // are the planes going through the same pt?
        // they are not for this snout!!

        Matrix4x4 s1Mat = (snout1.Matrix);
        Matrix4x4 s2Mat = (snout2.Matrix);
        Matrix4x4.Invert(s2Mat, out Matrix4x4 sn2MatInv);
        Matrix4x4.Invert(s1Mat, out Matrix4x4 sn1MatInv);

        var p1W = Vector4.Transform(snout1CapCenter, s1Mat);
        var p1M2 = Vector4.Transform(p1W, sn2MatInv);
        var dC1Pl2 = Vector3.Dot(snout2N, new Vector3(p1M2.X, p1M2.Y, p1M2.Z)) + snout2Dc;

        var p2 = Vector4.Transform(Vector4.Transform(snout2CapCenter, s2Mat), sn1MatInv);
        var dC2Pl1 = Vector3.Dot(snout1N, new Vector3(p2.X, p2.Y, p2.Z)) + snout1Dc;

        Assert.AreEqual(0.0, dC1Pl2);
        Assert.AreEqual(0.0, dC2Pl1);

        Assert.AreEqual(0.0, distance10 * 1000.0);
        Assert.AreEqual(0.0, distance11 * 1000.0);
        Assert.AreEqual(0.0, distance12 * 1000.0);
        Assert.AreEqual(0.0, distance20 * 1000.0);
        Assert.AreEqual(0.0, distance21 * 1000.0);
        Assert.AreEqual(0.0, distance22 * 1000.0);

        snout2.Connections[0] = new RvmConnection(
            snout2,
            snout1,
            1,
            0,
            snout2CapCenter,
            snout1N,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(snout2, snout2CapCenter, snout2CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.0001)]
    public void CalculateCapVisibility_TestSnoutCapMatch_RotatedCylindersWithDifferentShear()
    {
        var transform11 = Matrix4x4.CreateTranslation(0.0f, 0.0f, -2.0f);

        var snout1 = new RvmSnout(
            0,
            transform11,
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            2.0f, // top radius
            4.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );
        var snout1TopCap = snout1.GetTopCapEllipse();

        var transform21 = Matrix4x4.CreateTranslation(0.0f, 0.0f, 2.0f);
        //var rotate = Matrix4x4.CreateRotationZ(MathF.PI / 4.0f);
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -MathF.PI / 4.0f);
        var rotate = Matrix4x4.CreateFromQuaternion(rotationAroundY);

        var snout2 = new RvmSnout(
            0,
            transform21 * rotate,
            new RvmBoundingBox(new Vector3(), new Vector3()),
            2.0f, // bottom radius
            2.0f, // top radius
            4.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            -MathF.PI / 4.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );
        var snout2BottomCap = snout2.GetBottomCapEllipse();

        Assert.That(snout1TopCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0f));
        Assert.That(snout1TopCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0f));

        Assert.That(snout2BottomCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0 / MathF.Cos(snout2.BottomShearX)));
        Assert.That(snout2BottomCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0));

        var origin = VectorD.Build.Dense([0.0, 0.0, 0.0, 1.0]);
        var p1El1 = VectorD.Build.Dense([snout1TopCap.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0]);
        var p2El1 = VectorD.Build.Dense([0.0, snout1TopCap.ellipse2DPolar.semiMinorAxis, 0.0, 1.0]);
        var p1El2 = VectorD.Build.Dense([snout2BottomCap.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0]);
        var p2El2 = VectorD.Build.Dense([0.0, snout2BottomCap.ellipse2DPolar.semiMinorAxis, 0.0, 1.0]);

        var ptOrigEll1 = snout1TopCap.planeToModelCoord * origin;
        var ptOrigEll2 = snout2BottomCap.planeToModelCoord * origin;
        var ptPt1Ell1 = snout1TopCap.planeToModelCoord * p1El1;
        var ptPt2Ell1 = snout1TopCap.planeToModelCoord * p2El1;
        var ptPt1Ell2 = snout2BottomCap.planeToModelCoord * p1El2;
        var ptPt2Ell2 = snout2BottomCap.planeToModelCoord * p2El2;

        // snout1 -> top
        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var snout1CapCenterB = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var (snout1N, snout1Dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX,
            snout1.TopShearY,
            snout1CapCenter
        );
        var snout1N4D = new Vector4(snout1N.X, snout1N.Y, snout1N.Z, 0.0f);

        var distance10 =
            snout1N.X * ptOrigEll1[0] + snout1N.Y * ptOrigEll1[1] + snout1N.Z * ptOrigEll1[2] + snout1Dc;
        var distance11 =
            snout1N.X * ptPt1Ell1[0] + snout1N.Y * ptPt1Ell1[1] + snout1N.Z * ptPt1Ell1[2] + snout1Dc;
        var distance12 =
            snout1N.X * ptPt2Ell1[0] + snout1N.Y * ptPt2Ell1[1] + snout1N.Z * ptPt2Ell1[2] + snout1Dc;

        // snout2 -> bottom
        var snout2CapCenter = -0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        var (snout2N, snout2Dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.BottomShearX,
            snout2.BottomShearY,
            snout2CapCenter
        );
        var snout2N4D = new Vector4(snout2N.X, snout2N.Y, snout2N.Z, 0.0f);

        var distance20 =
            snout2N.X * ptOrigEll2[0] + snout2N.Y * ptOrigEll2[1] + snout2N.Z * ptOrigEll2[2] + snout2Dc;
        var distance21 =
            snout2N.X * ptPt1Ell2[0] + snout2N.Y * ptPt1Ell2[1] + snout2N.Z * ptPt1Ell2[2] + snout2Dc;
        var distance22 =
            snout2N.X * ptPt2Ell2[0] + snout2N.Y * ptPt2Ell2[1] + snout2N.Z * ptPt2Ell2[2] + snout2Dc;

        var v4TransfNormal1 = Vector4.Transform(snout1N4D, snout1.Matrix);
        var v4TransfNormal2 = Vector4.Transform(snout2N4D, snout2.Matrix);
        var transfNormal1 = new Vector3(v4TransfNormal1.X, v4TransfNormal1.Y, v4TransfNormal1.Z);
        var transfNormal2 = new Vector3(v4TransfNormal2.X, v4TransfNormal2.Y, v4TransfNormal2.Z);
        transfNormal1 = Vector3.Normalize(transfNormal1);
        transfNormal2 = Vector3.Normalize(transfNormal2);

        Assert.AreEqual(transfNormal1.X, transfNormal2.X, 0.00001);
        Assert.AreEqual(transfNormal1.Y, transfNormal2.Y, 0.00001);
        Assert.AreEqual(transfNormal1.Z, transfNormal2.Z, 0.00001);

        // are the planes going through the same pt?

        Matrix4x4 s1Mat = (snout1.Matrix);
        Matrix4x4 s2Mat = (snout2.Matrix);
        Matrix4x4.Invert(s2Mat, out Matrix4x4 sn2MatInv);
        Matrix4x4.Invert(s1Mat, out Matrix4x4 sn1MatInv);

        var p1W = Vector4.Transform(snout1CapCenter, s1Mat);
        var p1M2 = Vector4.Transform(p1W, sn2MatInv);
        var dC1Pl2 = Vector3.Dot(snout2N, new Vector3(p1M2.X, p1M2.Y, p1M2.Z)) + snout2Dc;

        var p2 = Vector4.Transform(Vector4.Transform(snout2CapCenter, s2Mat), sn1MatInv);
        var dC2Pl1 = Vector3.Dot(snout1N, new Vector3(p2.X, p2.Y, p2.Z)) + snout1Dc;

        Assert.AreEqual(0.0, dC1Pl2);
        Assert.AreEqual(0.0, dC2Pl1);

        Assert.AreEqual(0.0, distance10 * 1000.0);
        Assert.AreEqual(0.0, distance11 * 1000.0);
        Assert.AreEqual(0.0, distance12 * 1000.0);
        Assert.AreEqual(0.0, distance20 * 1000.0);
        Assert.AreEqual(0.0, distance21 * 1000.0);
        Assert.AreEqual(0.0, distance22 * 1000.0);

        snout1.Connections[0] = new RvmConnection(
            snout1,
            snout2,
            1,
            0,
            snout1CapCenter,
            snout1N,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.1)]
    public void RvmSnout_GeneralCone_CapMatch()
    {
        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                -0.000422618265f,
                -0.0009063078f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                0.000422618265f,
                0.0f,
                112.532379f,
                297.9548f,
                25.335f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -13.639999f), new Vector3(17.355f, 17.355f, 13.639999f)),
            17.355f, // bottom radius
            13.35f, // top radius
            27.2799988f, // height
            10.0f, // offset x
            20.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            -0.1f, // top shear x
            0.0f // top shear y
        );

        Matrix4x4 translate = Matrix4x4.CreateTranslation(snout1.OffsetX, snout1.OffsetY, 0.0f);
        var snout2 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.0f,
                -0.001f,
                0.0f,
                -0.000422618265f,
                -0.0009063078f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                0.000422618265f,
                0.0f,
                112.532379f,
                297.9548f,
                25.335f,
                1.0f
            ) * translate,
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -38.64f), new Vector3(17.355f, 17.355f, 38.64f)),
            13.35f, // bottom radius
            17.355f, // top radius
            77.28f, // height
            15.0f, // offset x
            25.0f, // offset y
            -0.1f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );

        // snout1 -> top
        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var snout1CapCenterB = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var (snout1N, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX,
            snout1.TopShearY,
            snout1CapCenter
        );

        // snout1's top should match snout2's bottom
        snout1.Connections[0] = new RvmConnection(
            snout1,
            snout2,
            1,
            0,
            snout1CapCenter,
            snout1N,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.1)]
    public void RvmSnout_CapSnout_EdgeCases_ZeroRadii()
    {
        var snout2 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                -0.000422618265f,
                0.0009063078f,
                0.0f,
                -0.001f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                -0.000422618265f,
                0.0f,
                353.937f,
                288.3957f,
                59.96253f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, 0.0f, 1.0f)),
            0.0f, // bottom radius
            0.0f, // top radius
            2.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );

        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(
                0.0f,
                0.000422618265f,
                -0.0009063078f,
                0.0f,
                0.0f,
                0.001f,
                0.0f,
                0.0f,
                0.0f,
                -0.0009063078f,
                -0.000422618265f,
                0.0f,
                353.937f,
                288.419281f,
                59.9735146f,
                1.0f
            ),
            new RvmBoundingBox(new Vector3(-62.5f, -62.5f, -25.0f), new Vector3(62.5f, 62.5f, 25.0f)),
            62.5f, // bottom radius
            62.5f, // top radius
            50.0f, // height
            0.0f, // offset x
            0.0f, // offset y
            0.0f, // bottom shear x
            0.0f, // bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
        );

        // snout1 -> top
        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var snout1CapCenterB = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var (snout1N, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX,
            snout1.TopShearY,
            snout1CapCenter
        );

        // snout1's top should match snout2's bottom
        snout1.Connections[0] = new RvmConnection(
            snout1,
            snout2,
            1,
            0,
            snout1CapCenter,
            snout1N,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapASnout1, bool showCapBSnout1) = CapVisibility.IsCapsVisible(
            snout1,
            snout1CapCenter,
            snout1CapCenterB
        );
        // snout 1 has non zero radii and thus its caps should not be occluded by a snout with zero radii
        Assert.That(showCapASnout1, Is.True);
        Assert.That(showCapBSnout1, Is.True);

        // snout2 -> bottom
        var snout2CapCenter = -0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        var snout2CapCenterB = 0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        var (snout2N, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.BottomShearX,
            snout2.BottomShearY,
            snout2CapCenter
        );

        // snout1's top should match snout2's bottom
        snout2.Connections[0] = new RvmConnection(
            snout2,
            snout1,
            0,
            1,
            snout2CapCenter,
            snout2N,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapASnout2, bool showCapBSnout2) = CapVisibility.IsCapsVisible(
            snout2,
            snout2CapCenter,
            snout2CapCenterB
        );
        // snout 2 has zero radii, caps should not show
        Assert.That(showCapASnout2, Is.False);

        // default is true, as this is not being tested
        Assert.That(showCapBSnout2, Is.True);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.3f, 0.3f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(
            _snout,
            snout,
            1,
            1,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// CIRCULAR TORUS ///////
    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            Vector3.Zero,
            new Vector3(0, -1, -0)
        );

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            new Vector3(0, -1, -0),
            Vector3.Zero
        );

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.4f, 0);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            circularTorus,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(_circularTorus, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            Vector3.Zero,
            new Vector3(0, -1, -0)
        );

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            new Vector3(0, -1, -0),
            Vector3.Zero
        );

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsAOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            Vector3.Zero,
            new Vector3(0, -1, -0)
        );

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapAOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            Vector3.Zero,
            new Vector3(0, -1, -0)
        );

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapBOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            new Vector3(0, -1, -0),
            Vector3.Zero
        );

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsAOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 0.4f, 1, 0, 0, 0, 0, 0, 0);

        _circularTorus.Connections[0] = new RvmConnection(
            _circularTorus,
            snout,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        (bool showCapA, bool showCapB) = CapVisibility.IsCapsVisible(
            _circularTorus,
            Vector3.Zero,
            new Vector3(0, -1, -0)
        );

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// SPHERICAL DISH ///////
    [Test]
    public void CalculateCapVisibility_CylinderHidesCapOfSphericalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _sphericalDish.Connections[0] = new RvmConnection(
            _sphericalDish,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        bool showCap = CapVisibility.IsCapVisible(_sphericalDish, Vector3.Zero);

        Assert.IsFalse(showCap);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsAOfSphericalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _sphericalDish.Connections[0] = new RvmConnection(
            _sphericalDish,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        bool showCap = CapVisibility.IsCapVisible(_sphericalDish, Vector3.Zero);

        Assert.IsTrue(showCap);
    }

    /////// ELLIPTICAL DISH ///////
    [Test]
    public void CalculateCapVisibility_CylinderHidesCapOfEllipticalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _ellipticalDish.Connections[0] = new RvmConnection(
            _ellipticalDish,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        bool showCap = CapVisibility.IsCapVisible(_ellipticalDish, Vector3.Zero);

        Assert.IsFalse(showCap);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapOfEllipticalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _ellipticalDish.Connections[0] = new RvmConnection(
            _ellipticalDish,
            cylinder,
            0,
            0,
            Vector3.Zero,
            Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide
        );

        bool showCap = CapVisibility.IsCapVisible(_ellipticalDish, Vector3.Zero);

        Assert.IsTrue(showCap);
    }
}
