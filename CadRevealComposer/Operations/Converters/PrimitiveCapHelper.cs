namespace CadRevealComposer.Operations.Converters;

using RvmSharp.Primitives;
using System;
using System.Numerics;
using Utils;

public static class PrimitiveCapHelper
{
    static PrimitiveCapHelper()
    {
    }

    public static bool CalculateCapVisibility(RvmPrimitive primitive, Vector3 capCenter)
    {
        return CalculateCapVisibility(primitive, capCenter, Vector3.Zero).showCapA;
    }

    public static (bool showCapA, bool showCapB) CalculateCapVisibility(RvmPrimitive primitive, Vector3 centerCapA,
        Vector3 centerCapB)
    {
        const float factor = 0.000_05f;

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

            var offset1 = isSorted
                ? connection.ConnectionIndex1
                : connection.ConnectionIndex2;

            var offset2 = isSorted
                ? connection.ConnectionIndex2
                : connection.ConnectionIndex1;

            var isCenterCapA = connection.Position.EqualsWithinTolerance(centerCapA, factor);
            var isCenterCapB = connection.Position.EqualsWithinTolerance(centerCapB, factor);

            var showCap = (prim1, prim2) switch
            {
                (RvmBox a, RvmCylinder b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b),
                (RvmBox a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b, offset2),
                (RvmCylinder a, RvmCylinder b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b),
                (RvmCircularTorus a, RvmCircularTorus b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b),
                (RvmCircularTorus a, RvmCylinder b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b),
                (RvmCircularTorus a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b, offset2),
                (RvmCylinder a, RvmSphericalDish b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b),
                (RvmCylinder a, RvmEllipticalDish b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b),
                (RvmCylinder a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b, offset2),
                (RvmCylinder a, RvmPyramid b) => true,
                (RvmEllipticalDish a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b, offset2),
                (RvmSnout a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b, offset1, offset2),
                (RvmSnout a, RvmSphericalDish b) => !OtherPrimitiveHasLargerOrEqualCap(primitive, a, b, offset1),
                _ => true
            };

            if (showCap is false && isCenterCapA)
            {
                showCapA = false;
            }

            if (showCap is false && isCenterCapB)
            {
                showCapB = false;
            }
        }

        return (showCapA, showCapB);
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmPrimitive currentPrimitive,
        RvmBox rvmBox,
        RvmCylinder rvmCylinder)
    {
        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        // Only check for the cylinder, because a box does not have any caps
        if (ReferenceEquals(currentPrimitive, rvmCylinder))
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
        RvmPrimitive currentPrimitive,
        RvmBox rvmBox,
        RvmSnout rvmSnout,
        uint rvmSnoutOffset)
    {
        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var isSnoutCapTop = rvmSnoutOffset == 0;

        var snoutRadius = isSnoutCapTop
            ? rvmSnout.RadiusTop * snoutScale.X
            : rvmSnout.RadiusBottom * snoutScale.X;

        // Only check for the snout, because a box does not have any caps
        if (ReferenceEquals(currentPrimitive, rvmSnout))
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
        RvmPrimitive currentPrimitive,
        RvmCylinder rvmCylinder1,
        RvmCylinder rvmCylinder2)
    {
        rvmCylinder1.Matrix.DecomposeAndNormalize(out var scale1, out _, out _);
        rvmCylinder2.Matrix.DecomposeAndNormalize(out var scale2, out _, out _);

        var radius1 = rvmCylinder1.Radius * scale1.X;
        var radius2 = rvmCylinder2.Radius * scale2.X;

        if (ReferenceEquals(currentPrimitive, rvmCylinder1) &&
            radius2 >= radius1)
        {
            return true;
        }

        if (ReferenceEquals(currentPrimitive, rvmCylinder2) &&
            radius1 >= radius2)
        {
            return true;
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmPrimitive currentPrimitive,
        RvmCircularTorus rvmCircularTorus1,
        RvmCircularTorus rvmCircularTorus2)
    {
        rvmCircularTorus1.Matrix.DecomposeAndNormalize(out var torusScale1, out _, out _);
        rvmCircularTorus2.Matrix.DecomposeAndNormalize(out var torusScale2, out _, out _);

        var radius1 = rvmCircularTorus1.Radius * torusScale1.X;
        var radius2 = rvmCircularTorus2.Radius * torusScale2.X;

        if (ReferenceEquals(currentPrimitive, rvmCircularTorus1))
        {
            if (radius2 >= radius1)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmCircularTorus2))
        {
            if (radius1 >= radius2)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmPrimitive currentPrimitive,
        RvmCircularTorus rvmCircularTorus,
        RvmCylinder rvmCylinder)
    {
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var scaleCircularTorus, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var scaleCylinder, out _, out _);

        var radiusCircularTorus = rvmCircularTorus.Radius * scaleCircularTorus.X;
        var radiusCylinder = rvmCylinder.Radius * scaleCylinder.X;

        if (ReferenceEquals(currentPrimitive, rvmCircularTorus))
        {
            if (radiusCylinder >= radiusCircularTorus)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmCylinder))
        {
            if (radiusCircularTorus >= radiusCylinder)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmPrimitive currentPrimitive,
        RvmCircularTorus rvmCircularTorus,
        RvmSnout rvmSnout,
        uint rvmSnoutOffset)
    {
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var torusRadius = rvmCircularTorus.Radius * circularTorusScale.X;

        var isSnoutCapTop = rvmSnoutOffset == 0;

        var snoutRadius = isSnoutCapTop
            ? rvmSnout.RadiusTop * snoutScale.X
            : rvmSnout.RadiusBottom * snoutScale.X;

        if (ReferenceEquals(currentPrimitive, rvmCircularTorus))
        {
            if (snoutRadius >= torusRadius)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmSnout))
        {
            if (torusRadius >= snoutRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmPrimitive currentPrimitive,
        RvmCylinder rvmCylinder,
        RvmSphericalDish rvmSphericalDish)
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var rvmSphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (ReferenceEquals(currentPrimitive, rvmCylinder))
        {
            if (rvmSphericalDishRadius >= cylinderRadius)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmSphericalDish))
        {
            if (cylinderRadius >= rvmSphericalDishRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(RvmPrimitive currentPrimitive, RvmCylinder rvmCylinder,
        RvmEllipticalDish rvmEllipticalDish)
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        if (ReferenceEquals(currentPrimitive, rvmCylinder))
        {
            if (ellipticalDishRadius >= cylinderRadius)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmEllipticalDish))
        {
            if (cylinderRadius >= ellipticalDishRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmPrimitive currentPrimitive,
        RvmCylinder rvmCylinder,
        RvmSnout rvmSnout,
        uint rvmSnoutOffset)
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var scaleCylinder, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var scaleSnout, out _, out _);

        var radiusCylinder = rvmCylinder.Radius * scaleCylinder.X;
        var radiusSnoutTop = rvmSnout.RadiusTop * scaleSnout.X;
        var radiusSnoutBottom = rvmSnout.RadiusBottom * scaleSnout.X;

        var isSnoutCapTop = rvmSnoutOffset == 0;
        var isSnoutCapBottom = rvmSnoutOffset == 1;

        var snoutRadius = isSnoutCapTop
            ? rvmSnout.RadiusTop * scaleSnout.X
            : rvmSnout.RadiusBottom * scaleSnout.X;

        if (ReferenceEquals(currentPrimitive, rvmCylinder))
        {
            if (snoutRadius >= radiusCylinder)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmSnout))
        {
            if (radiusCylinder >= snoutRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(RvmPrimitive currentPrimitive,
        RvmEllipticalDish rvmEllipticalDish, RvmSnout rvmSnout, uint rvmSnoutOffset)
    {
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        var isSnoutCapTop = rvmSnoutOffset == 0;
        var snoutRadius = isSnoutCapTop
            ? rvmSnout.RadiusTop * snoutScale.X
            : rvmSnout.RadiusBottom * snoutScale.X;

        if (ReferenceEquals(currentPrimitive, rvmSnout))
        {
            if (ellipticalDishRadius >= snoutRadius)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmEllipticalDish))
        {
            if (snoutRadius >= ellipticalDishRadius)
            {
                return true;
            }
        }

        return false;
    }


    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmPrimitive currentPrimitive,
        RvmSnout rvmSnout1,
        RvmSnout rvmSnout2,
        uint rvmSnoutOffset1,
        uint rvmSnoutOffset2)
    {
        rvmSnout1.Matrix.DecomposeAndNormalize(out var scaleSnout1, out _, out _);
        rvmSnout2.Matrix.DecomposeAndNormalize(out var scaleSnout2, out _, out _);

        var radiusSnoutTop1 = rvmSnout1.RadiusTop * scaleSnout1.X;
        var radiusSnoutBottom1 = rvmSnout1.RadiusBottom * scaleSnout1.X;
        var radiusSnoutTop2 = rvmSnout2.RadiusTop * scaleSnout2.X;
        var radiusSnoutBottom2 = rvmSnout2.RadiusBottom * scaleSnout2.X;

        var isSnoutCapTop1 = rvmSnoutOffset1 == 0;
        var isSnoutCapBottom1 = rvmSnoutOffset1 == 1;
        var isSnoutCapTop2 = rvmSnoutOffset2 == 0;
        var isSnoutCapBottom2 = rvmSnoutOffset2 == 1;

        if (ReferenceEquals(currentPrimitive, rvmSnout1))
        {
            if (isSnoutCapTop1 && isSnoutCapTop2 && radiusSnoutTop2 >= radiusSnoutTop1 ||
                isSnoutCapTop1 && isSnoutCapBottom2 && radiusSnoutTop2 >= radiusSnoutBottom1 ||
                isSnoutCapBottom1 && isSnoutCapTop2 && radiusSnoutBottom2 >= radiusSnoutTop1 ||
                isSnoutCapBottom1 && isSnoutCapBottom2 && radiusSnoutBottom2 >= radiusSnoutBottom1)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmSnout2))
        {
            if (isSnoutCapTop2 && isSnoutCapTop1 && radiusSnoutTop1 >= radiusSnoutTop2 ||
                isSnoutCapTop2 && isSnoutCapBottom1 && radiusSnoutTop1 >= radiusSnoutBottom2 ||
                isSnoutCapBottom2 && isSnoutCapTop1 && radiusSnoutBottom1 >= radiusSnoutTop2 ||
                isSnoutCapBottom2 && isSnoutCapBottom1 && radiusSnoutBottom1 >= radiusSnoutBottom2)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(RvmPrimitive currentPrimitive, RvmSnout rvmSnout,
        RvmSphericalDish rvmSphericalDish, uint rvmSnoutOffset)
    {
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var isSnoutCapTop = rvmSnoutOffset == 0;
        var snoutRadius = isSnoutCapTop
            ? rvmSnout.RadiusTop * snoutScale.X
            : rvmSnout.RadiusBottom * snoutScale.X;

        var sphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (ReferenceEquals(currentPrimitive, rvmSnout))
        {
            if (sphericalDishRadius >= snoutRadius)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmSphericalDish))
        {
            if (snoutRadius >= sphericalDishRadius)
            {
                return true;
            }
        }

        return false;
    }
}