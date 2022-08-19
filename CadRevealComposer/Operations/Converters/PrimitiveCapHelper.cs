namespace CadRevealComposer.Operations.Converters;

using RvmSharp.Primitives;
using System;
using System.Numerics;
using Utils;

public static class PrimitiveCapHelper
{
    public static bool CalculateCapVisibility(RvmPrimitive primitive, Vector3 capCenter)
    {
        return CalculateCapVisibility(primitive, capCenter, Vector3.Zero).showCapA;
    }

    public static (bool showCapA, bool showCapB) CalculateCapVisibility(RvmPrimitive primitive, Vector3 capCenterA,
        Vector3 capCenterB)
    {
        const float connectionDistanceTolerance = 0.000_05f;

        bool showCapA = true, showCapB = true;

        foreach (var connection in primitive.Connections.WhereNotNull())
        {
            // sort Primitive1/Primitive2 to avoid creating double amount of switch statements
            var isSorted = StringComparer.Ordinal.Compare(
                connection.Primitive1.GetType().Name,
                connection.Primitive2.GetType().Name) < 0;

            var prim1 = isSorted
                ? connection.Primitive1
                : connection.Primitive2;

            var prim2 = isSorted
                ? connection.Primitive2
                : connection.Primitive1;

            var connectionIndex1 = isSorted
                ? connection.ConnectionIndex1
                : connection.ConnectionIndex2;

            var connectionIndex2 = isSorted
                ? connection.ConnectionIndex2
                : connection.ConnectionIndex1;

            var isCapCenterA = connection.Position.EqualsWithinTolerance(capCenterA, connectionDistanceTolerance);
            var isCapCenterB = connection.Position.EqualsWithinTolerance(capCenterB, connectionDistanceTolerance);

            var isPrim1CurrentPrimitive = ReferenceEquals(primitive, prim1);

            var showCap = (prim1, prim2) switch
            {
                (RvmBox a, RvmCylinder b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, isPrim1CurrentPrimitive),
                (RvmBox a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex2,
                    isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmCylinder b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, isPrim1CurrentPrimitive),
                (RvmCircularTorus a, RvmCircularTorus b) => !OtherPrimitiveHasLargerOrEqualCap(a, b,
                    isPrim1CurrentPrimitive),
                (RvmCircularTorus a, RvmCylinder b) =>
                    !OtherPrimitiveHasLargerOrEqualCap(a, b, isPrim1CurrentPrimitive),
                (RvmCircularTorus a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex2,
                    isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmSphericalDish b) =>
                    !OtherPrimitiveHasLargerOrEqualCap(a, b, isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmEllipticalDish b) => !OtherPrimitiveHasLargerOrEqualCap(a, b,
                    isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex2,
                    isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmPyramid b) => true, // TODO
                (RvmEllipticalDish a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex2,
                    isPrim1CurrentPrimitive),
                (RvmSnout a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex1, connectionIndex2,
                    isPrim1CurrentPrimitive),
                (RvmSnout a, RvmSphericalDish b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex1,
                    isPrim1CurrentPrimitive),
                _ => true
            };

            if (showCap is false && isCapCenterA)
            {
                showCapA = false;
            }

            if (showCap is false && isCapCenterB)
            {
                showCapB = false;
            }
        }

        return (showCapA, showCapB);
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmBox rvmBox,
        RvmCylinder rvmCylinder,
        bool isPrim1CurrentPrimitive)
    {
        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        // Only check for the cylinder, because a box does not have any caps
        if (!isPrim1CurrentPrimitive)
        {
            // TODO: Is it possible to find out which sides to compare with?
            if (cylinderRadius < halfLengthX &&
                cylinderRadius < halfLengthY &&
                cylinderRadius < halfLengthZ)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmBox rvmBox,
        RvmSnout rvmSnout,
        uint rvmSnoutOffset,
        bool isPrim1CurrentPrimitive)
    {
        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var isSnoutCapTop = rvmSnoutOffset == 0;

        var snoutRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMinorAxis * snoutScale.X;

        // Only check for the snout, because a box does not have any caps
        if (!isPrim1CurrentPrimitive)
        {
            // TODO: Is it possible to find out which sides to compare with?
            if (snoutRadius < halfLengthX &&
                snoutRadius < halfLengthY &&
                snoutRadius < halfLengthZ)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmCylinder rvmCylinder1,
        RvmCylinder rvmCylinder2,
        bool isPrim1CurrentPrimitive)
    {
        rvmCylinder1.Matrix.DecomposeAndNormalize(out var cylinderScale1, out _, out _);
        rvmCylinder2.Matrix.DecomposeAndNormalize(out var cylinderScale2, out _, out _);

        var cylinderRadius1 = rvmCylinder1.Radius * cylinderScale1.X;
        var cylinderRadius2 = rvmCylinder2.Radius * cylinderScale2.X;

        if (isPrim1CurrentPrimitive)
        {
            if (cylinderRadius2 >= cylinderRadius1)
            {
                return true;
            }
        }
        else
        {
            if (cylinderRadius1 >= cylinderRadius2)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmCircularTorus rvmCircularTorus1,
        RvmCircularTorus rvmCircularTorus2,
        bool isPrim1CurrentPrimitive)
    {
        rvmCircularTorus1.Matrix.DecomposeAndNormalize(out var torusScale1, out _, out _);
        rvmCircularTorus2.Matrix.DecomposeAndNormalize(out var torusScale2, out _, out _);

        var torusRadius1 = rvmCircularTorus1.Radius * torusScale1.X;
        var torusRadius2 = rvmCircularTorus2.Radius * torusScale2.X;

        if (isPrim1CurrentPrimitive)
        {
            if (torusRadius2 >= torusRadius1)
            {
                return true;
            }
        }
        else
        {
            if (torusRadius1 >= torusRadius2)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmCircularTorus rvmCircularTorus,
        RvmCylinder rvmCylinder,
        bool isPrim1CurrentPrimitive)
    {
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var circularTorusRadius = rvmCircularTorus.Radius * circularTorusScale.X;
        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (cylinderRadius >= circularTorusRadius)
            {
                return true;
            }
        }
        else
        {
            if (circularTorusRadius >= cylinderRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmCircularTorus rvmCircularTorus,
        RvmSnout rvmSnout,
        uint rvmSnoutOffset,
        bool isPrim1CurrentPrimitive)
    {
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var torusRadius = rvmCircularTorus.Radius * circularTorusScale.X;

        var isSnoutCapTop = rvmSnoutOffset == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMajorAxis * snoutScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (semiMinorRadius >= torusRadius)
            {
                return true;
            }
        }
        else
        {
            if (torusRadius >= semiMajorRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmCylinder rvmCylinder,
        RvmSphericalDish rvmSphericalDish,
        bool isPrim1CurrentPrimitive)
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var rvmSphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (rvmSphericalDishRadius >= cylinderRadius)
            {
                return true;
            }
        }
        else
        {
            if (cylinderRadius >= rvmSphericalDishRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmCylinder rvmCylinder,
        RvmEllipticalDish rvmEllipticalDish,
        bool isPrim1CurrentPrimitive)
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (ellipticalDishRadius >= cylinderRadius)
            {
                return true;
            }
        }
        else
        {
            if (cylinderRadius >= ellipticalDishRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmCylinder rvmCylinder,
        RvmSnout rvmSnout,
        uint rvmSnoutOffset,
        bool isPrim1CurrentPrimitive)
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        var isSnoutCapTop = rvmSnoutOffset == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMajorAxis * snoutScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (semiMinorRadius >= cylinderRadius)
            {
                return true;
            }
        }
        else
        {
            if (cylinderRadius >= semiMajorRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmEllipticalDish rvmEllipticalDish,
        RvmSnout rvmSnout,
        uint rvmSnoutOffset,
        bool isPrim1CurrentPrimitive)
    {
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        var isSnoutCapTop = rvmSnoutOffset == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMajorAxis * snoutScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (semiMinorRadius >= ellipticalDishRadius)
            {
                return true;
            }
        }
        else
        {
            if (ellipticalDishRadius >= semiMajorRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmSnout rvmSnout1,
        RvmSnout rvmSnout2,
        uint rvmSnoutOffset1,
        uint rvmSnoutOffset2,
        bool isPrim1CurrentPrimitive)
    {
        rvmSnout1.Matrix.DecomposeAndNormalize(out var snoutScale1, out _, out _);
        rvmSnout2.Matrix.DecomposeAndNormalize(out var snoutScale2, out _, out _);

        var isSnoutCapTop1 = rvmSnoutOffset1 == 0;

        var semiMinorAxis1 = isSnoutCapTop1
            ? rvmSnout1.GetTopRadii().semiMinorAxis * snoutScale1.X
            : rvmSnout1.GetBottomRadii().semiMinorAxis * snoutScale1.X;

        var semiMajorAxis1 = isSnoutCapTop1
            ? rvmSnout1.GetTopRadii().semiMajorAxis * snoutScale1.X
            : rvmSnout1.GetBottomRadii().semiMajorAxis * snoutScale1.X;

        var isSnoutCapTop2 = rvmSnoutOffset2 == 0;

        var semiMinorAxis2 = isSnoutCapTop2
            ? rvmSnout2.GetTopRadii().semiMinorAxis * snoutScale2.X
            : rvmSnout2.GetBottomRadii().semiMinorAxis * snoutScale2.X;

        var semiMajorAxis2 = isSnoutCapTop1
            ? rvmSnout2.GetTopRadii().semiMajorAxis * snoutScale2.X
            : rvmSnout2.GetBottomRadii().semiMajorAxis * snoutScale2.X;

        // TODO This can give false positives. Need to check that the major axis aligns the other major axis
        if (isPrim1CurrentPrimitive)
        {
            if (semiMajorAxis2 >= semiMajorAxis1 && semiMinorAxis2 >= semiMinorAxis1)
            {
                return true;
            }
        }
        else
        {
            if (semiMajorAxis1 >= semiMajorAxis2 && semiMinorAxis1 >= semiMajorAxis1)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmSnout rvmSnout,
        RvmSphericalDish rvmSphericalDish,
        uint rvmSnoutOffset,
        bool isPrim1CurrentPrimitive)
    {
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var isSnoutCapTop = rvmSnoutOffset == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopRadii().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomRadii().semiMajorAxis * snoutScale.X;

        var sphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (sphericalDishRadius >= semiMajorRadius)
            {
                return true;
            }
        }
        else
        {
            if (semiMinorRadius >= sphericalDishRadius)
            {
                return true;
            }
        }

        return false;
    }
}