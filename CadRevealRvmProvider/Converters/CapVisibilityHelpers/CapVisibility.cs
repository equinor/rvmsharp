namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using MathNet.Numerics.LinearAlgebra.Double;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Numerics;
using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

public static class CapVisibility
{
    public static int TotalNumberOfCapsTested;
    public static int CapsHidden;
    public static int CapsShown;
    public static int CapsWithoutConnections;

    private static readonly float Buffer = 0.1f;

    public static bool IsCapVisible(RvmPrimitive primitive, Vector3 capCenter)
    {
        TotalNumberOfCapsTested--; // Subtracting one, since two will be add later
        CapsShown--; // Subtracting one, since "CapB" will return as shown later

        return IsCapsVisible(primitive, capCenter, Vector3.Zero).showCapA;
    }

    public static (bool showCapA, bool showCapB) IsCapsVisible(
        RvmPrimitive primitive,
        Vector3 capCenterA,
        Vector3 capCenterB
    )
    {
        Interlocked.Increment(ref TotalNumberOfCapsTested);
        Interlocked.Increment(ref TotalNumberOfCapsTested);

        bool showCapA = true,
            showCapB = true;

        if (primitive.Connections.Length == 0 || primitive.Connections.All(x => x == null))
        {
            CapsWithoutConnections++;
            if (capCenterB != Vector3.Zero)
            {
                CapsWithoutConnections++;
            }
        }
        else
        {
            var count = 0;
            var positionSum = Vector3.Zero;

            var testLocation = Vector3.Zero;
            bool testLocationSet = false;

            foreach (var connection in primitive.Connections)
            {
                if (connection != null)
                {
                    positionSum += connection.Position;
                    if (!testLocationSet)
                    {
                        testLocation = connection.Position;
                        testLocationSet = true;
                    }
                    count++;
                }
            }
            var averagePosition = positionSum / count;

            if (averagePosition.EqualsWithinTolerance(testLocation, 0.0001f))
            {
                CapsWithoutConnections++;
            }
        }

        foreach (var connection in primitive.Connections.WhereNotNull())
        {
            // sort Primitive1/Primitive2 to avoid creating double amount of switch statements
            var isSorted =
                StringComparer.Ordinal.Compare(
                    connection.Primitive1.GetType().Name,
                    connection.Primitive2.GetType().Name
                ) < 0;

            var prim1 = isSorted ? connection.Primitive1 : connection.Primitive2;

            var prim2 = isSorted ? connection.Primitive2 : connection.Primitive1;

            var connectionIndex1 = isSorted ? connection.ConnectionIndex1 : connection.ConnectionIndex2;

            var connectionIndex2 = isSorted ? connection.ConnectionIndex2 : connection.ConnectionIndex1;

            var diffA = MathF.Abs((connection.Position - capCenterA).Length());
            var diffB = MathF.Abs((connection.Position - capCenterB).Length());

            var isCapCenterA = diffA <= diffB;
            var isCapCenterB = diffB < diffA;

            var isPrim1CurrentPrimitive = ReferenceEquals(primitive, prim1);

            var showCap = (prim1, prim2) switch
            {
                (RvmBox a, RvmCylinder b) => ShowCurrentCap(a, b, isPrim1CurrentPrimitive),
                (RvmBox a, RvmSnout b) => ShowCurrentCap(a, b, connectionIndex2, isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmCylinder b) => ShowCurrentCap(a, b, isPrim1CurrentPrimitive),
                (RvmCircularTorus a, RvmCircularTorus b) => ShowCurrentCap(a, b, isPrim1CurrentPrimitive),
                (RvmCircularTorus a, RvmCylinder b) => ShowCurrentCap(a, b, isPrim1CurrentPrimitive),
                (RvmCircularTorus a, RvmSnout b) => ShowCurrentCap(a, b, connectionIndex2, isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmSphericalDish b) => ShowCurrentCap(a, b, isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmEllipticalDish b) => ShowCurrentCap(a, b, isPrim1CurrentPrimitive),
                (RvmCylinder a, RvmSnout b) => ShowCurrentCap(a, b, connectionIndex2, isPrim1CurrentPrimitive),
                (RvmEllipticalDish a, RvmSnout b) => ShowCurrentCap(a, b, connectionIndex2, isPrim1CurrentPrimitive),
                (RvmSnout a, RvmSnout b)
                    => ShowCurrentCap(a, b, connectionIndex1, connectionIndex2, isPrim1CurrentPrimitive),
                (RvmSnout a, RvmSphericalDish b) => ShowCurrentCap(a, b, connectionIndex1, isPrim1CurrentPrimitive),
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

        if (!showCapA)
        {
            CapsHidden++;
        }
        else
        {
            CapsShown++;
        }

        if (!showCapB)
        {
            CapsHidden++;
        }
        else
        {
            CapsShown++;
        }

        return (showCapA, showCapB);
    }

    private static bool ShowCurrentCap(RvmBox rvmBox, RvmCylinder rvmCylinder, bool isPrim1CurrentPrimitive)
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
            if (cylinderRadius < halfLengthX && cylinderRadius < halfLengthY && cylinderRadius < halfLengthZ)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmBox rvmBox,
        RvmSnout rvmSnout,
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        var snoutMajorAxis = snoutEllipse.semiMajorAxis * snoutScale.X;

        // Only check for the snout, because a box does not have any caps
        if (!isPrim1CurrentPrimitive)
        {
            // TODO: Is it possible to find out which sides to compare with?
            if (snoutMajorAxis < halfLengthX && snoutMajorAxis < halfLengthY && snoutMajorAxis < halfLengthZ)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(RvmCylinder rvmCylinder1, RvmCylinder rvmCylinder2, bool isPrim1CurrentPrimitive)
    {
        rvmCylinder1.Matrix.DecomposeAndNormalize(out var cylinderScale1, out _, out _);
        rvmCylinder2.Matrix.DecomposeAndNormalize(out var cylinderScale2, out _, out _);

        var cylinderRadius1 = rvmCylinder1.Radius * cylinderScale1.X;
        var cylinderRadius2 = rvmCylinder2.Radius * cylinderScale2.X;

        if (isPrim1CurrentPrimitive)
        {
            if (cylinderRadius2 + Buffer >= cylinderRadius1)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius1 + Buffer >= cylinderRadius2)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmCircularTorus rvmCircularTorus1,
        RvmCircularTorus rvmCircularTorus2,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmCircularTorus1.Matrix.DecomposeAndNormalize(out var torusScale1, out _, out _);
        rvmCircularTorus2.Matrix.DecomposeAndNormalize(out var torusScale2, out _, out _);

        var torusRadius1 = rvmCircularTorus1.Radius * torusScale1.X;
        var torusRadius2 = rvmCircularTorus2.Radius * torusScale2.X;

        if (isPrim1CurrentPrimitive)
        {
            if (torusRadius2 + Buffer >= torusRadius1)
            {
                return false;
            }
        }
        else
        {
            if (torusRadius1 + Buffer >= torusRadius2)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmCircularTorus rvmCircularTorus,
        RvmCylinder rvmCylinder,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var circularTorusRadius = rvmCircularTorus.Radius * circularTorusScale.X;
        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (cylinderRadius + Buffer >= circularTorusRadius)
            {
                return false;
            }
        }
        else
        {
            if (circularTorusRadius + Buffer >= cylinderRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmCircularTorus rvmCircularTorus,
        RvmSnout rvmSnout,
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var torusRadius = rvmCircularTorus.Radius * circularTorusScale.X;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (semiMinorRadius + Buffer >= torusRadius)
            {
                return false;
            }
        }
        else
        {
            if (torusRadius + Buffer >= semiMajorRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmCylinder rvmCylinder,
        RvmSphericalDish rvmSphericalDish,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var rvmSphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (rvmSphericalDishRadius + Buffer >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + Buffer >= rvmSphericalDishRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmCylinder rvmCylinder,
        RvmEllipticalDish rvmEllipticalDish,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (ellipticalDishRadius + Buffer >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + Buffer >= ellipticalDishRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmCylinder rvmCylinder,
        RvmSnout rvmSnout,
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (semiMinorRadius + Buffer >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + Buffer >= semiMajorRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmEllipticalDish rvmEllipticalDish,
        RvmSnout rvmSnout,
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (semiMinorRadius + Buffer >= ellipticalDishRadius)
            {
                return false;
            }
        }
        else
        {
            if (ellipticalDishRadius + Buffer >= semiMajorRadius)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ShowCurrentCap(
        RvmSnout rvmSnout1,
        RvmSnout rvmSnout2,
        uint rvmSnoutCapIndex1,
        uint rvmSnoutCapIndex2,
        bool isPrim1CurrentPrimitive
    )
    {
        var isSnoutCapTop1 = rvmSnoutCapIndex1 == 1;
        var isSnoutCapTop2 = rvmSnoutCapIndex2 == 1;

        Ellipse3D ellipseCurrent;
        Ellipse3D ellipseOther;

        MatrixD snout1ToWorld;
        MatrixD worldToSnout2;
        if (isPrim1CurrentPrimitive)
        {
            // any snout has larger cap than a snout w zero radius top&bottom
            if (rvmSnout1.RadiusBottom < 0.00001 && rvmSnout1.RadiusTop < 0.00001)
                return false;
            if (rvmSnout2.RadiusBottom < 0.00001 && rvmSnout2.RadiusTop < 0.00001)
                return true;

            // is ellipse1 totally inside ellipse2 ?
            ellipseCurrent = isSnoutCapTop1 ? rvmSnout1.GetTopCapEllipse() : rvmSnout1.GetBottomCapEllipse();
            ellipseOther = isSnoutCapTop2 ? rvmSnout2.GetTopCapEllipse() : rvmSnout2.GetBottomCapEllipse();
            snout1ToWorld = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(rvmSnout1.Matrix).Transpose();
            // these matrices are stored as trans ^^ vv
            worldToSnout2 = VectorAlgebraHelper
                .ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout2.Matrix))
                .Inverse();
        }
        else
        {
            // any snout has larger cap than a snout w zero radius top&bottom
            if (rvmSnout2.RadiusBottom < 0.00001 && rvmSnout2.RadiusTop < 0.00001)
                return false;
            if (rvmSnout1.RadiusBottom < 0.00001 && rvmSnout1.RadiusTop < 0.00001)
                return true;

            ellipseCurrent = isSnoutCapTop2 ? rvmSnout2.GetTopCapEllipse() : rvmSnout2.GetBottomCapEllipse();
            ellipseOther = isSnoutCapTop1 ? rvmSnout1.GetTopCapEllipse() : rvmSnout1.GetBottomCapEllipse();
            snout1ToWorld = VectorAlgebraHelper.ConvertMatrix4x4ToMatrixDouble(rvmSnout2.Matrix).Transpose();
            worldToSnout2 = VectorAlgebraHelper
                .ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout1.Matrix))
                .Inverse();
        }

        double aE1 = ellipseCurrent.ellipse2DPolar.semiMajorAxis;
        double bE1 = ellipseCurrent.ellipse2DPolar.semiMinorAxis;
        double x0E1 = ellipseCurrent.ellipse2DPolar.x0;
        double y0E1 = ellipseCurrent.ellipse2DPolar.y0;
        double theta = ellipseCurrent.ellipse2DPolar.theta;

        var ptE1_xplaneCoord = new VectorD[4];
        ptE1_xplaneCoord[0] = VectorD.Build.Dense(new double[] { aE1 - x0E1, -y0E1, 0.0f, 1.0 });
        ptE1_xplaneCoord[1] = VectorD.Build.Dense(new double[] { -x0E1, bE1 - y0E1, 0.0f, 1.0 });
        ptE1_xplaneCoord[2] = VectorD.Build.Dense(new double[] { -aE1 - x0E1, -y0E1, 0.0f, 1.0 });
        ptE1_xplaneCoord[3] = VectorD.Build.Dense(new double[] { -x0E1, -bE1 - y0E1, 0.0f, 1.0 });

        var cosTheta = Math.Cos(theta);
        var sinTheta = Math.Sin(theta);
        var matRotationEl1 = DenseMatrix.OfArray(
            new double[,]
            {
                { cosTheta, sinTheta, 0.0, 0.0 },
                { sinTheta, cosTheta, 0.0, 0.0 },
                { 0.0, 0.0, 1.0, 0.0 },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var mat_stack =
            ellipseOther.modelToPlaneCoord
            * worldToSnout2
            * snout1ToWorld
            * ellipseCurrent.planeToModelCoord
            * matRotationEl1;

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

            var d = ConicSectionsHelper.CalcDistancePointEllise(ellipseOther.ellipse2DPolar, px, py);
            if (d > 0.1) // 0.1mm
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShowCurrentCap(
        RvmSnout rvmSnout,
        RvmSphericalDish rvmSphericalDish,
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive
    )
    {
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var isSnoutCapTop = rvmSnoutCapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        var sphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (isPrim1CurrentPrimitive)
        {
            if (sphericalDishRadius + Buffer >= semiMajorRadius)
            {
                return false;
            }
        }
        else
        {
            if (semiMinorRadius + Buffer >= sphericalDishRadius)
            {
                return false;
            }
        }

        return true;
    }
}
