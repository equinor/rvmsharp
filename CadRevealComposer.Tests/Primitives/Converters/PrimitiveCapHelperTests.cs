namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using NUnit.Framework;
using RvmSharp.Primitives;
using System;
using System.Numerics;

[TestFixture]
public class PrimitiveCapHelperTests
{
    private RvmCylinder _cylinder;
    private RvmSnout _snout;
    private RvmCircularTorus _circularTorus;
    private RvmSphericalDish _sphericalDish;
    private RvmEllipticalDish _ellipticalDish;

    [SetUp]
    public void Setup()
    {
        _cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1);
        _snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);
        _circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.5f, MathF.PI / 4);
        _sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, null, 0.5f, 1);
        _ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, null, 0.5f, 1);
    }

    /////// CYLINDER ///////
    [Test]
    public void CalculateCapVisibility_BoxHidesCapAOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, null, 2, 2, 2);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_BoxHidesCapBOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, null, 2, 2, 2);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallBoxDoesNotHideCapsOfCylinder()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 0.5f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsOfCylinder()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.4f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.5f, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.5f, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfCylinder()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.4f, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SphericalDishHidesCapAOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, sphericalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SphericalDishHidesCapBOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, sphericalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSphericalDishDoesNotHideCapsOfCylinder()
    {
        var sphericalDish = new RvmSphericalDish(1, Matrix4x4.Identity, null, 0.4f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, sphericalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapAOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapBOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, null, 0.5f, 1f);


        _cylinder.Connections[0] = new RvmConnection(_cylinder, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallEllipticalDishDoesNotHideCapsOfCylinder()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, null, 0.4f, 1f);


        _cylinder.Connections[0] = new RvmConnection(_cylinder, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapAOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapBOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _cylinder.Connections[0] = new RvmConnection(_cylinder, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_cylinder, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsOfCylinder()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.4f, 0.4f, 1, 0, 0, 0, 0, 0, 0);

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
        var box = new RvmBox(1, Matrix4x4.Identity, null, 2, 2, 2);

        _snout.Connections[0] = new RvmConnection(_snout, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_BoxHidesCapBOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, null, 2, 2, 2);

        _snout.Connections[0] = new RvmConnection(_snout, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallBoxDoesNotHideCapsOfSnout()
    {
        var box = new RvmBox(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 0.5f);

        _snout.Connections[0] = new RvmConnection(_snout, box, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasRectangularSide | RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.5f, 0);

        _snout.Connections[0] = new RvmConnection(_snout, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.5f, 0);

        _snout.Connections[0] = new RvmConnection(_snout, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfSnout()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.4f, 0);

        _snout.Connections[0] = new RvmConnection(_snout, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsOfSnout()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.4f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapAOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_EllipticalDishHidesCapBOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _snout.Connections[0] = new RvmConnection(_snout, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallEllipticalDishDoesNotHideCapsOfSnout()
    {
        var ellipticalDish = new RvmEllipticalDish(1, Matrix4x4.Identity, null, 0.4f, 1f);


        _snout.Connections[0] = new RvmConnection(_snout, ellipticalDish, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutDishHidesCapAOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(_snout, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
             RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, new Vector3(0, -1, 0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutDishHidesCapBOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(_snout, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, new Vector3(0, -1, 0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsOfSnout()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.4f, 0.4f, 1, 0, 0, 0, 0, 0, 0);

        _snout.Connections[0] = new RvmConnection(_snout, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_snout, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// CIRCULAR TORUS ///////
    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapAOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.5f, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CircularTorusHidesCapBOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.5f, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCircularTorusDoesNotHideCapsOfCircularTorus()
    {
        var circularTorus = new RvmCircularTorus(1, Matrix4x4.Identity, null, 0, 0.4f, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, circularTorus, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsAOfCircularTorus()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.4f, 1f);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapAOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);

        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SnoutHidesCapBOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.5f, 0.5f, 1, 0, 0, 0, 0, 0, 0);


        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallSnoutDoesNotHideCapsAOfCircularTorus()
    {
        var snout = new RvmSnout(1, Matrix4x4.Identity, null, 0.4f, 0.4f, 1, 0, 0, 0, 0, 0, 0);


        _circularTorus.Connections[0] = new RvmConnection(_circularTorus, snout, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_circularTorus, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// SPHERICAL DISH ///////
    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfSphericalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _sphericalDish.Connections[0] = new RvmConnection(_sphericalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_sphericalDish, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfSphericalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _sphericalDish.Connections[0] = new RvmConnection(_sphericalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_sphericalDish, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsAOfSphericalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.4f, 1f);

        _sphericalDish.Connections[0] = new RvmConnection(_sphericalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_sphericalDish, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }

    /////// ELLIPTICAL DISH ///////
    [Test]
    public void CalculateCapVisibility_CylinderHidesCapAOfEllipticalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _ellipticalDish.Connections[0] = new RvmConnection(_ellipticalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_ellipticalDish, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsFalse(showCapA);
        Assert.IsTrue(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_CylinderHidesCapBOfEllipticalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.5f, 1f);

        _ellipticalDish.Connections[0] = new RvmConnection(_ellipticalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_ellipticalDish, new Vector3(0, -1, -0), Vector3.Zero);

        Assert.IsTrue(showCapA);
        Assert.IsFalse(showCapB);
    }

    [Test]
    public void CalculateCapVisibility_SmallCylinderDoesNotHideCapsAOfEllipticalDish()
    {
        var cylinder = new RvmCylinder(1, Matrix4x4.Identity, null, 0.4f, 1f);

        _ellipticalDish.Connections[0] = new RvmConnection(_ellipticalDish, cylinder, 0, 0, Vector3.Zero, Vector3.UnitY,
            RvmConnection.ConnectionType.HasCircularSide);

        (bool showCapA, bool showCapB) = PrimitiveCapHelper.CalculateCapVisibility(_ellipticalDish, Vector3.Zero, new Vector3(0, -1, -0));

        Assert.IsTrue(showCapA);
        Assert.IsTrue(showCapB);
    }
}
