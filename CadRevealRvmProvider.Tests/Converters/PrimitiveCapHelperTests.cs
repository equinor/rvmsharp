namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealRvmProvider.Converters;
using NUnit.Framework;
using RvmSharp.Primitives;
using RvmSharp.Operations;
using System;
using System.Numerics;

using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;

[TestFixture]
public class PrimitiveCapHelperTests
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

        _cylinder.Connections[0] = new RvmConnection(_cylinder, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_BoxHidesCapBOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 2, 2, 2);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallBoxDoesNotHideCapsOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 0.5f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.4f, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SphericalDishHidesCapAOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, sphericalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SphericalDishHidesCapBOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, sphericalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSphericalDishDoesNotHideCapsOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, sphericalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapAOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapBOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);


        _cylinder.Connections[0] = new RvmConnection(_cylinder, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallEllipticalDishDoesNotHideCapsOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);


        _cylinder.Connections[0] = new RvmConnection(_cylinder, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapAOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapBOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 0.4f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }


    /////// SNOUT ///////
    [Test]
    public void CalculateCapVisibility_BoxHidesCapAOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 2, 2, 2);

        _snout.Connections[0] = new RvmConnection(_snout, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_BoxHidesCapBOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 2, 2, 2);

        _snout.Connections[0] = new RvmConnection(_snout, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallBoxDoesNotHideCapsOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 0.5f);

        _snout.Connections[0] = new RvmConnection(_snout, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _snout.Connections[0] = new RvmConnection(_snout, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _snout.Connections[0] = new RvmConnection(_snout, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.4f, 0);

        _snout.Connections[0] = new RvmConnection(_snout, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapAOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapBOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallEllipticalDishDoesNotHideCapsOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);


        _snout.Connections[0] = new RvmConnection(_snout, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutDishHidesCapAOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(_snout, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutDishHidesCapBOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(_snout, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.0001)]
    public void CalculateCapVisibility_TestCapMatch_Cylinder()
    {
        var snout1 = new RvmSnout(0, Matrix4x4.Identity, new RvmBoundingBox(new Vector3(), new Vector3()),
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
        var snout2 = new RvmSnout(0, transform, new RvmBoundingBox(new Vector3(), new Vector3()),
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
        (var snout1_n, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX, snout1.TopShearY,
            snout1CapCenter);

        snout1.Connections[0] = new RvmConnection(snout1, snout2, 1, 0, snout1CapCenter, snout1_n,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.0001)]
    public void CalculateCapVisibility_TestCapMatch_RotatedCylinders()
    {
        var transform1 = Matrix4x4.CreateTranslation(0.0f, 0.0f, 0.0f);
        var rotate = Matrix4x4.CreateRotationX(0.1f);
        var snout1 = new RvmSnout(0, transform1, new RvmBoundingBox(new Vector3(), new Vector3()),
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
        var snout2 = new RvmSnout(0, transform2, new RvmBoundingBox(new Vector3(), new Vector3()),
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
        (var snout1_n, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX, snout1.TopShearY,
            snout1CapCenter);

        snout1.Connections[0] = new RvmConnection(snout1, snout2, 1, 0, snout1CapCenter, snout1_n,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);

    }

    [Test]
    [DefaultFloatingPointTolerance(0.01)]
    public void CalculateCapVisibility_TestCapMatch_TwoSnouts()
    {
        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(0.0f, 0.001f, 0.0f, 0.0f,
                           0.0008660254f, 0.0f, 0.0005f, 0.0f,
                           0.0005f, 0.0f, -0.0008660254f, 0.0f,
                           75.08398f, 289.475f, 35.4f, 1.0f),
            new RvmBoundingBox(new Vector3(-500.0f, -500.0f, 160.0475f),
                                new Vector3(500.0f, 500.0f, 695.9459f)),
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
            new Matrix4x4(0.001f, 0.0f, 0.0f, 0.0f,
                           0.0f, -0.001f, 0.0f, 0.0f,
                           0.0f, 0.0f, -0.001f, 0.0f,
                           74.95f, 289.475f, 35.7660255f, 1.0f),
            new RvmBoundingBox(new Vector3(-500.0f, -500.0f, -133.9746f),
                                new Vector3(500.0f, 500.0f, 561.9713f)),
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

        var origin = VectorD.Build.Dense(new double[] { 0.0, 0.0, 0.0, 1.0 });
        var p1_el1 = VectorD.Build.Dense(new double[] { ellipse1.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0 });
        var p2_el1 = VectorD.Build.Dense(new double[] { 0.0, ellipse1.ellipse2DPolar.semiMinorAxis, 0.0, 1.0 });
        var p1_el2 = VectorD.Build.Dense(new double[] { ellipse2.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0 });
        var p2_el2 = VectorD.Build.Dense(new double[] { 0.0, ellipse2.ellipse2DPolar.semiMinorAxis, 0.0, 1.0 });

        var pt_orig_ell1 = ellipse1.planeToModelCoord * origin;
        var pt_orig_ell2 = ellipse2.planeToModelCoord * origin;
        var pt_pt1_ell1 = ellipse1.planeToModelCoord * p1_el1;
        var pt_pt2_ell1 = ellipse1.planeToModelCoord * p2_el1;
        var pt_pt1_ell2 = ellipse2.planeToModelCoord * p1_el2;
        var pt_pt2_ell2 = ellipse2.planeToModelCoord * p2_el2;

        // snout1 -> bottom, 
        var snout1CapCenter = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        //var snout1TopCapCenter4D = new Vector4(snout1.OffsetX, snout1.OffsetY, snout1.Height, 1.0f);
        (var snout1_n, var snout1_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.BottomShearX, snout1.BottomShearY,
            snout1CapCenter);
        var snout1_n_4d = new Vector4(snout1_n.X, snout1_n.Y, snout1_n.Z, 0.0f);

        var distance1_0 = snout1_n.X * pt_orig_ell1[0] + snout1_n.Y * pt_orig_ell1[1] + snout1_n.Z * pt_orig_ell1[2] + snout1_dc;
        var distance1_1 = snout1_n.X * pt_pt1_ell1[0] + snout1_n.Y * pt_pt1_ell1[1] + snout1_n.Z * pt_pt1_ell1[2] + snout1_dc;
        var distance1_2 = snout1_n.X * pt_pt2_ell1[0] + snout1_n.Y * pt_pt2_ell1[1] + snout1_n.Z * pt_pt2_ell1[2] + snout1_dc;

        // snout2 -> top, 
        var snout2CapCenter = 0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        var snout2CapCenterB = -0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        (var snout2_n, var snout2_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.TopShearX, snout2.TopShearY,
            snout2CapCenter);
        var snout2_n_4d = new Vector4(snout2_n.X, snout2_n.Y, snout2_n.Z, 0.0f);

        var distance2_0 = snout2_n.X * pt_orig_ell2[0] + snout2_n.Y * pt_orig_ell2[1] + snout2_n.Z * pt_orig_ell2[2] + snout2_dc;
        var distance2_1 = snout2_n.X * pt_pt1_ell2[0] + snout2_n.Y * pt_pt1_ell2[1] + snout2_n.Z * pt_pt1_ell2[2] + snout2_dc;
        var distance2_2 = snout2_n.X * pt_pt2_ell2[0] + snout2_n.Y * pt_pt2_ell2[1] + snout2_n.Z * pt_pt2_ell2[2] + snout2_dc;

        var v4transfNormal1 = Vector4.Transform(snout1_n_4d, snout1.Matrix);
        var v4transfNormal2 = Vector4.Transform(snout2_n_4d, snout2.Matrix);
        var transfNormal1 = new Vector3(v4transfNormal1.X, v4transfNormal1.Y, v4transfNormal1.Z);
        var transfNormal2 = new Vector3(v4transfNormal2.X, v4transfNormal2.Y, v4transfNormal2.Z);
        transfNormal1 = Vector3.Normalize(transfNormal1);
        transfNormal2 = Vector3.Normalize(transfNormal2);

        Assert.AreEqual(Vector3.Dot(transfNormal1, transfNormal2), 1.0f, 0.001);

        // are the planes going through the same pt?
        // they are not for this snout!!

        Matrix4x4 s1Mat = (snout1.Matrix);
        Matrix4x4 s2Mat = (snout2.Matrix);
        Matrix4x4 sn2MatInv;
        Matrix4x4.Invert(s2Mat, out sn2MatInv);
        Matrix4x4 sn1MatInv;
        Matrix4x4.Invert(s1Mat, out sn1MatInv);

        var p1_w = Vector4.Transform(snout1CapCenter, s1Mat);
        var p1_m2 = Vector4.Transform(p1_w, sn2MatInv);
        var d_c1_pl2 = Vector3.Dot(snout2_n, new Vector3(p1_m2.X, p1_m2.Y, p1_m2.Z)) + snout2_dc;

        var p2 = Vector4.Transform(Vector4.Transform(snout2CapCenter, s2Mat), sn1MatInv);
        var d_c2_pl1 = Vector3.Dot(snout1_n, new Vector3(p2.X, p2.Y, p2.Z)) + snout1_dc;

        Assert.AreEqual(0.0, d_c1_pl2);
        Assert.AreEqual(0.0, d_c2_pl1);

        Assert.AreEqual(0.0, distance1_0 * 1000.0);
        Assert.AreEqual(0.0, distance1_1 * 1000.0);
        Assert.AreEqual(0.0, distance1_2 * 1000.0);
        Assert.AreEqual(0.0, distance2_0 * 1000.0);
        Assert.AreEqual(0.0, distance2_1 * 1000.0);
        Assert.AreEqual(0.0, distance2_2 * 1000.0);

        snout2.Connections[0] = new RvmConnection(snout2, snout1, 1, 0, snout2CapCenter, snout1_n,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(snout2, snout2CapCenter, snout2CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    [DefaultFloatingPointTolerance(0.0001)]
    public void CalculateCapVisibility_TestCapMatch_RotatedCylindersWithDifferentShear()
    {
        var transform11 = Matrix4x4.CreateTranslation(0.0f, 0.0f, -2.0f);

        var snout1 = new RvmSnout(0, transform11, new RvmBoundingBox(new Vector3(), new Vector3()),
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

        var snout2 = new RvmSnout(0, transform21*rotate, new RvmBoundingBox(new Vector3(), new Vector3()),
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
        var snout2bottomCap = snout2.GetBottomCapEllipse();

        Assert.That(snout1TopCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0f));
        Assert.That(snout1TopCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0f));

        Assert.That(snout2bottomCap.ellipse2DPolar.semiMajorAxis, Is.EqualTo(2.0 / MathF.Cos(snout2.BottomShearX)));
        Assert.That(snout2bottomCap.ellipse2DPolar.semiMinorAxis, Is.EqualTo(2.0));

        var origin = VectorD.Build.Dense(new double[] { 0.0, 0.0, 0.0, 1.0 });
        var p1_el1 = VectorD.Build.Dense(new double[] { snout1TopCap.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0 });
        var p2_el1 = VectorD.Build.Dense(new double[] { 0.0, snout1TopCap.ellipse2DPolar.semiMinorAxis, 0.0, 1.0 });
        var p1_el2 = VectorD.Build.Dense(new double[] { snout2bottomCap.ellipse2DPolar.semiMajorAxis, 0.0, 0.0, 1.0 });
        var p2_el2 = VectorD.Build.Dense(new double[] { 0.0, snout2bottomCap.ellipse2DPolar.semiMinorAxis, 0.0, 1.0 });

        var pt_orig_ell1 = snout1TopCap.planeToModelCoord * origin;
        var pt_orig_ell2 = snout2bottomCap.planeToModelCoord * origin;
        var pt_pt1_ell1 = snout1TopCap.planeToModelCoord * p1_el1;
        var pt_pt2_ell1 = snout1TopCap.planeToModelCoord * p2_el1;
        var pt_pt1_ell2 = snout2bottomCap.planeToModelCoord * p1_el2;
        var pt_pt2_ell2 = snout2bottomCap.planeToModelCoord * p2_el2;


        // snout1 -> top
        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var snout1CapCenterB = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        (var snout1_n, var snout1_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX, snout1.TopShearY,
            snout1CapCenter);
        var snout1_n_4d = new Vector4(snout1_n.X, snout1_n.Y, snout1_n.Z, 0.0f);

        var distance1_0 = snout1_n.X * pt_orig_ell1[0] + snout1_n.Y * pt_orig_ell1[1] + snout1_n.Z * pt_orig_ell1[2] + snout1_dc;
        var distance1_1 = snout1_n.X * pt_pt1_ell1[0] + snout1_n.Y * pt_pt1_ell1[1] + snout1_n.Z * pt_pt1_ell1[2] + snout1_dc;
        var distance1_2 = snout1_n.X * pt_pt2_ell1[0] + snout1_n.Y * pt_pt2_ell1[1] + snout1_n.Z * pt_pt2_ell1[2] + snout1_dc;

        // snout2 -> bottom
        var snout2CapCenter = -0.5f * (new Vector3(snout2.OffsetX, snout2.OffsetY, snout2.Height));
        (var snout2_n, var snout2_dc) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout2.BottomShearX, snout2.BottomShearY,
            snout2CapCenter);
        var snout2_n_4d = new Vector4(snout2_n.X, snout2_n.Y, snout2_n.Z, 0.0f);

        var distance2_0 = snout2_n.X * pt_orig_ell2[0] + snout2_n.Y * pt_orig_ell2[1] + snout2_n.Z * pt_orig_ell2[2] + snout2_dc;
        var distance2_1 = snout2_n.X * pt_pt1_ell2[0] + snout2_n.Y * pt_pt1_ell2[1] + snout2_n.Z * pt_pt1_ell2[2] + snout2_dc;
        var distance2_2 = snout2_n.X * pt_pt2_ell2[0] + snout2_n.Y * pt_pt2_ell2[1] + snout2_n.Z * pt_pt2_ell2[2] + snout2_dc;
        
        var v4transfNormal1 = Vector4.Transform(snout1_n_4d, snout1.Matrix);
        var v4transfNormal2 = Vector4.Transform(snout2_n_4d, snout2.Matrix);
        var transfNormal1 = new Vector3(v4transfNormal1.X, v4transfNormal1.Y, v4transfNormal1.Z);
        var transfNormal2 = new Vector3(v4transfNormal2.X, v4transfNormal2.Y, v4transfNormal2.Z);
        transfNormal1 = Vector3.Normalize(transfNormal1);
        transfNormal2 = Vector3.Normalize(transfNormal2);

        Assert.AreEqual((double)transfNormal1.X, (double)transfNormal2.X, 0.00001);
        Assert.AreEqual((double)transfNormal1.Y, (double)transfNormal2.Y, 0.00001);
        Assert.AreEqual((double)transfNormal1.Z, (double)transfNormal2.Z, 0.00001);

        // are the planes going through the same pt?

        Matrix4x4 s1Mat = (snout1.Matrix);
        Matrix4x4 s2Mat = (snout2.Matrix);
        Matrix4x4 sn2MatInv;
        Matrix4x4.Invert(s2Mat, out sn2MatInv);
        Matrix4x4 sn1MatInv;
        Matrix4x4.Invert(s1Mat, out sn1MatInv);

        var p1_w = Vector4.Transform(snout1CapCenter, s1Mat);
        var p1_m2 = Vector4.Transform(p1_w, sn2MatInv);
        var d_c1_pl2 = Vector3.Dot(snout2_n, new Vector3(p1_m2.X, p1_m2.Y, p1_m2.Z)) + snout2_dc;

        var p2 = Vector4.Transform(Vector4.Transform(snout2CapCenter, s2Mat), sn1MatInv);
        var d_c2_pl1 = Vector3.Dot(snout1_n, new Vector3(p2.X, p2.Y, p2.Z)) + snout1_dc;

        Assert.AreEqual(0.0, d_c1_pl2);
        Assert.AreEqual(0.0, d_c2_pl1);

        Assert.AreEqual(0.0, distance1_0 * 1000.0);
        Assert.AreEqual(0.0, distance1_1 * 1000.0);
        Assert.AreEqual(0.0, distance1_2 * 1000.0);
        Assert.AreEqual(0.0, distance2_0 * 1000.0);
        Assert.AreEqual(0.0, distance2_1 * 1000.0);
        Assert.AreEqual(0.0, distance2_2 * 1000.0);

        snout1.Connections[0] = new RvmConnection(snout1, snout2, 1, 0, snout1CapCenter, snout1_n,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }
    [Test]
    [DefaultFloatingPointTolerance(0.1)]
    public void RvmSnout_GeneralCone_CapMatch()
    {
        var snout1 = new RvmSnout(
            1,
            new Matrix4x4(0.0f, 0.0f, -0.001f, 0.0f,
                           -0.000422618265f, -0.0009063078f, 0.0f, 0.0f,
                           0.0f, -0.0009063078f, 0.000422618265f, 0.0f,
                           112.532379f, 297.9548f, 25.335f, 1.0f),
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -13.639999f),
                                new Vector3(17.355f, 17.355f, 13.639999f)),
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
            new Matrix4x4(0.0f, 0.0f, -0.001f, 0.0f,
                           -0.000422618265f, -0.0009063078f, 0.0f, 0.0f,
                           0.0f, -0.0009063078f, 0.000422618265f, 0.0f,
                           112.532379f, 297.9548f, 25.335f, 1.0f) * translate,
            new RvmBoundingBox(new Vector3(-17.355f, -17.355f, -38.64f),
                                new Vector3(17.355f, 17.355f, 38.64f)),
            13.35f, // bottom radius
            17.355f, // top radius
            77.28f, // height
            15.0f, // offset x
            25.0f, // offset y
            -0.1f, // bottom shear x
            0.0f,// bottom shear y
            0.0f, // top shear x
            0.0f // top shear y
            );

        // snout1 -> top
        var snout1CapCenter = 0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        var snout1CapCenterB = -0.5f * (new Vector3(snout1.OffsetX, snout1.OffsetY, snout1.Height));
        (var snout1_n, _) = GeometryHelper.GetPlaneFromShearAndPoint(
            snout1.TopShearX, snout1.TopShearY,
            snout1CapCenter);

        // snout1's top should match snout2's bottom
        snout1.Connections[0] = new RvmConnection(snout1, snout2, 1, 0, snout1CapCenter, snout1_n,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(snout1, snout1CapCenter, snout1CapCenterB);
        Assert.That(showCapA, Is.False);
        Assert.That(showCapB, Is.True);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.3f, 0.3f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(_snout, snout, 1, 1, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// CIRCULAR TORUS ///////
    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.5f, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0, 0.4f, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsAOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapAOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapBOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);


        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsAOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 0.4f, 1, 0, 0, 0, 0, 0, 0);


        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// SPHERICAL DISH ///////
    [Test]
    public void CalculateCapVisibility_CylinderHidesCapOfSphericalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _sphericalDish.Connections[0] = new RvmConnection(_sphericalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        bool showCap = PrimitiveCapHelper.CalculateCapVisibility(_sphericalDish, Vector3.Zero);

        Assert.IsFalse(showCap);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsAOfSphericalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _sphericalDish.Connections[0] = new RvmConnection(_sphericalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        bool showCap = PrimitiveCapHelper.CalculateCapVisibility(_sphericalDish, Vector3.Zero);

        Assert.IsTrue(showCap);
    }

    /////// ELLIPTICAL DISH ///////
    [Test]
    public void CalculateCapVisibility_CylinderHidesCapOfEllipticalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.5f, 1f);

        _ellipticalDish.Connections[0] = new RvmConnection(_ellipticalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        bool showCap = PrimitiveCapHelper.CalculateCapVisibility(_ellipticalDish, Vector3.Zero);

        Assert.IsFalse(showCap);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapOfEllipticalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, _defaultRvmBoundingBox, 0.4f, 1f);

        _ellipticalDish.Connections[0] = new RvmConnection(_ellipticalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        bool showCap = PrimitiveCapHelper.CalculateCapVisibility(_ellipticalDish, Vector3.Zero);

        Assert.IsTrue(showCap);
    }
}
