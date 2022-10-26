namespace CadRevealComposer.Operations.Converters;

using RvmSharp.Primitives;
using RvmSharp.Operations;
using System;
using System.Numerics;
using Utils;

using MathNet.Numerics.LinearAlgebra.Double;
using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

public static class PrimitiveCapHelper
{
    public static int global_count_hidden_caps = 0;
    public static int global_count_shown_caps = 0;

    public static bool CalculateCapVisibility(RvmPrimitive primitive, Vector3 capCenter)
    {
        return CalculateCapVisibility(primitive, capCenter, Vector3.Zero).showCapA;
    }
    

    public static (bool showCapA, bool showCapB) CalculateCapVisibility(RvmPrimitive primitive, Vector3 capCenterA,
    Vector3 capCenterB)
    {
        const float connectionDistanceTolerance = 0.000_05f; // Arbitrary value

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
                (RvmEllipticalDish a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex2,
                    isPrim1CurrentPrimitive),
                (RvmSnout a, RvmSnout b) => !OtherPrimitiveHasLargerOrEqualCap(a, b, connectionIndex1, connectionIndex2,
                    isPrim1CurrentPrimitive), // TODO User story: #77874
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
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive)
    {
        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var snoutMajorAxis = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMajorAxis * snoutScale.X;

        // Only check for the snout, because a box does not have any caps
        if (!isPrim1CurrentPrimitive)
        {
            // TODO: Is it possible to find out which sides to compare with?
            if (snoutMajorAxis < halfLengthX &&
                snoutMajorAxis < halfLengthY &&
                snoutMajorAxis < halfLengthZ)
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
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive)
    {
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var torusRadius = rvmCircularTorus.Radius * circularTorusScale.X;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMajorAxis * snoutScale.X;

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
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive)
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMajorAxis * snoutScale.X;

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
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive)
    {
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMajorAxis * snoutScale.X;

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
        uint rvmSnoutCapIndex1,
        uint rvmSnoutCapIndex2,
        bool isPrim1CurrentPrimitive)
    {
        var isSnoutCapTop1 = rvmSnoutCapIndex1 == 1;
        var isSnoutCapTop2 = rvmSnoutCapIndex2 == 1;

        (EllipsePolarForm polarEq, MatrixD xplane2ModelCoord, MatrixD modelCoord2xplane) ellipseCurrent;
        (EllipsePolarForm polarEq, MatrixD xplane2ModelCoord, MatrixD modelCoord2xplane) ellipseOther;

        MatrixD snout1ToWorld;
        MatrixD worldToSnout2;
        if (isPrim1CurrentPrimitive)
        {
            // is ellipse1 totally inside ellipse2 ?
            ellipseCurrent = isSnoutCapTop1 ? rvmSnout1.GetTopCapEllipse() : rvmSnout1.GetBottomCapEllipse();
            ellipseOther = isSnoutCapTop2 ? rvmSnout2.GetTopCapEllipse() : rvmSnout2.GetBottomCapEllipse();
            snout1ToWorld = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(rvmSnout1.Matrix).Transpose();
            // these matrices are stored as trans ^^ vv
            worldToSnout2 = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout2.Matrix)).Inverse();
        }
        else
        {
            ellipseCurrent = isSnoutCapTop2 ? rvmSnout2.GetTopCapEllipse() : rvmSnout2.GetBottomCapEllipse();
            ellipseOther = isSnoutCapTop1 ? rvmSnout1.GetTopCapEllipse() : rvmSnout1.GetBottomCapEllipse();
            snout1ToWorld = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(rvmSnout2.Matrix).Transpose();
            worldToSnout2 = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout1.Matrix)).Inverse();
        }

        double aE1 =  ellipseCurrent.polarEq.semiMajorAxis;
        double bE1 =  ellipseCurrent.polarEq.semiMinorAxis;
        double x0E1 = ellipseCurrent.polarEq.x0;
        double y0E1 = ellipseCurrent.polarEq.y0;
        double theta = ellipseCurrent.polarEq.theta;

        var ptE1_xplaneCoord = new VectorD[4];
        ptE1_xplaneCoord[0] = VectorD.Build.Dense(new double[] { aE1 - x0E1, -y0E1, 0.0f, 1.0 });
        ptE1_xplaneCoord[1] = VectorD.Build.Dense(new double[] { -x0E1, bE1 - y0E1, 0.0f, 1.0 });
        ptE1_xplaneCoord[2] = VectorD.Build.Dense(new double[] { -aE1 - x0E1, -y0E1, 0.0f, 1.0 });
        ptE1_xplaneCoord[3] = VectorD.Build.Dense(new double[] { -x0E1, -bE1 - y0E1, 0.0f, 1.0 });

        var cosTheta = Math.Cos(theta);
        var sinTheta = Math.Sin(theta);
        var matRotationEl1 = DenseMatrix.OfArray(new double[,] {
           { cosTheta, sinTheta, 0.0, 0.0 },
           { sinTheta, cosTheta, 0.0, 0.0 },
           { 0.0, 0.0, 1.0, 0.0 },
           { 0.0, 0.0, 0.0, 1.0 }
        });

        var mat_stack =
            ellipseOther.modelCoord2xplane *
            worldToSnout2 * snout1ToWorld *
            ellipseCurrent.xplane2ModelCoord *
            matRotationEl1;

        var ptE1_transformedTo_xplaneCoordOfE2 = new VectorD[4];
        for (int i = 0; i < 4; i++)
        {
            ptE1_transformedTo_xplaneCoordOfE2[i] = mat_stack.Multiply(ptE1_xplaneCoord[i]);
        }

        // hide cap if all four points (extremities) of the ellipse (cap) are inside the other cap
        // returns true if all if all points of the current ellipse are inside the other ellipse
        // returns false if there exists at least one point of the current ellipse that is outside the other ellipse
        const int x = 0;
        const int y = 1;
        for (int i = 0; i < 4; i++)
        {
            var px = ptE1_transformedTo_xplaneCoordOfE2[i][x];
            var py = ptE1_transformedTo_xplaneCoordOfE2[i][y];

            var d = ConicSectionsHelper.CalcDistancePointEllise(ellipseOther.polarEq, px, py);
            if (d > 0.1) // 0.1mm
            {
                global_count_shown_caps++;
                return false;
            }
        }

        global_count_hidden_caps++;
        return true;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmSnout rvmSnout,
        RvmSphericalDish rvmSphericalDish,
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive)
    {
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().polarEq.semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomCapEllipse().polarEq.semiMajorAxis * snoutScale.X;

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