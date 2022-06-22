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

    public static (bool showCapA, bool showCapB) CalculateCapVisibility(RvmPrimitive primitive, Vector3 capCenterA,
        Vector3 capCenterB)
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

            var isCapCenterA = connection.Position.EqualsWithinTolerance(capCenterA, factor);
            var isCapCenterB = connection.Position.EqualsWithinTolerance(capCenterB, factor);

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
        rvmCylinder1.Matrix.DecomposeAndNormalize(out var cylinderScale1, out _, out _);
        rvmCylinder2.Matrix.DecomposeAndNormalize(out var cylinderScale2, out _, out _);

        var cylinderRadius1 = rvmCylinder1.Radius * cylinderScale1.X;
        var cylinderRadius2 = rvmCylinder2.Radius * cylinderScale2.X;

        if (ReferenceEquals(currentPrimitive, rvmCylinder1) &&
            cylinderRadius2 >= cylinderRadius1)
        {
            return true;
        }

        if (ReferenceEquals(currentPrimitive, rvmCylinder2) &&
            cylinderRadius1 >= cylinderRadius2)
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

        var torusRadius1 = rvmCircularTorus1.Radius * torusScale1.X;
        var torusRadius2 = rvmCircularTorus2.Radius * torusScale2.X;

        if (ReferenceEquals(currentPrimitive, rvmCircularTorus1))
        {
            if (torusRadius2 >= torusRadius1)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmCircularTorus2))
        {
            if (torusRadius1 >= torusRadius2)
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
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var circularTorusRadius = rvmCircularTorus.Radius * circularTorusScale.X;
        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        if (ReferenceEquals(currentPrimitive, rvmCircularTorus))
        {
            if (cylinderRadius >= circularTorusRadius)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmCylinder))
        {
            if (circularTorusRadius >= cylinderRadius)
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
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        var isSnoutCapTop = rvmSnoutOffset == 0;
        var snoutRadius = isSnoutCapTop
            ? rvmSnout.RadiusTop * snoutScale.X
            : rvmSnout.RadiusBottom * snoutScale.X;

        if (ReferenceEquals(currentPrimitive, rvmCylinder))
        {
            if (snoutRadius >= cylinderRadius)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmSnout))
        {
            if (cylinderRadius >= snoutRadius)
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
        rvmSnout1.Matrix.DecomposeAndNormalize(out var snoutScale1, out _, out _);
        rvmSnout2.Matrix.DecomposeAndNormalize(out var snoutScale2, out _, out _);

        var isSnoutCapTop1 = rvmSnoutOffset1 == 0;
        var snoutRadius1 = isSnoutCapTop1
            ? rvmSnout1.RadiusTop * snoutScale1.X
            : rvmSnout1.RadiusBottom * snoutScale1.X;

        var isSnoutCapTop2 = rvmSnoutOffset2 == 0;
        var snoutRadius2 = isSnoutCapTop2
            ? rvmSnout2.RadiusTop * snoutScale2.X
            : rvmSnout2.RadiusBottom * snoutScale2.X;

        if (ReferenceEquals(currentPrimitive, rvmSnout1))
        {
            if (snoutRadius2 >= snoutRadius1)
            {
                return true;
            }
        }

        if (ReferenceEquals(currentPrimitive, rvmSnout2))
        {
            if (snoutRadius1 >= snoutRadius2)
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