namespace CadRevealComposer.Operations.Converters;

using RvmSharp.Primitives;
using System;
using System.Numerics;
using Utils;

using MathNet.Numerics.LinearAlgebra.Double;
using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

public static class PrimitiveCapHelper
{
    public static int global_count_removed_caps = 0;

    public static bool CalculateCapVisibility(RvmPrimitive primitive, Vector3 capCenter)
    {
        return CalculateCapVisibility(primitive, capCenter, Vector3.Zero).showCapA;
    }
    public static MatrixD ConvertMatrix4x4ToMatrixDouble(Matrix4x4 mat)
    {
        return DenseMatrix.OfArray(new double[,] {
           { mat.M11, mat.M12, mat.M13, mat.M14 },
           { mat.M21, mat.M22, mat.M23, mat.M24 },
           { mat.M31, mat.M32, mat.M33, mat.M34 },
           { mat.M41, mat.M42, mat.M43, mat.M44 }
        });
    }

    public static MatrixD CreateUniformScale(double s)
    {
        return DenseMatrix.OfArray(new double[,] {
           { s, 0, 0, 0 },
           { 0, s, 0, 0 },
           { 0, 0, s, 0 },
           { 0, 0, 0, 1 }
        });
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

            int counter = 0;

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

        var isSnoutCapTop = rvmSnoutCapIndex == 0;

        var snoutMajorAxis = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMajorAxis * snoutScale.X;

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

        var isSnoutCapTop = rvmSnoutCapIndex == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMajorAxis * snoutScale.X;

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

        var isSnoutCapTop = rvmSnoutCapIndex == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMajorAxis * snoutScale.X;

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

        var isSnoutCapTop = rvmSnoutCapIndex == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMajorAxis * snoutScale.X;

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
        rvmSnout1.Matrix.DecomposeAndNormalize(out var snoutScale1, out _, out _);
        rvmSnout2.Matrix.DecomposeAndNormalize(out var snoutScale2, out _, out _);

        var isSnoutCapTop1 = rvmSnoutCapIndex1 == 0;
        var isSnoutCapTop2 = rvmSnoutCapIndex2 == 0;

        // this is to improve numerics
        MatrixD scaleMat = CreateUniformScale(1.0 / (double)snoutScale1.X);

        // TODO User story: #77874
        // This test can be optimized by comparing the major axii and minor axii
        // This will however require that we are able to check that the major axii of
        // one primitive aligns the the major axii of the other

        var result_old = false;
        
        var semiMinorAxis1 = isSnoutCapTop1
            ? rvmSnout1.GetTopEllipsePolarForm().semiMinorAxis * snoutScale1.X
            : rvmSnout1.GetBottomEllipsePolarForm().semiMinorAxis * snoutScale1.X;

        var semiMajorAxis1 = isSnoutCapTop1
            ? rvmSnout1.GetTopEllipsePolarForm().semiMajorAxis * snoutScale1.X
            : rvmSnout1.GetBottomEllipsePolarForm().semiMajorAxis * snoutScale1.X;

        var semiMinorAxis2 = isSnoutCapTop2
            ? rvmSnout2.GetTopEllipsePolarForm().semiMinorAxis * snoutScale2.X
            : rvmSnout2.GetBottomEllipsePolarForm().semiMinorAxis * snoutScale2.X;

        var semiMajorAxis2 = isSnoutCapTop2
            ? rvmSnout2.GetTopEllipsePolarForm().semiMajorAxis * snoutScale2.X
            : rvmSnout2.GetBottomEllipsePolarForm().semiMajorAxis * snoutScale2.X;

        // TODO User story: #77874
        // This test can be optimized by comparing the major axii and minor axii
        // This will however require that we are able to check that the major axii of
        // one primitive aligns the the major axii of the other
        if (isPrim1CurrentPrimitive)
        {
            if (semiMinorAxis2 >= semiMajorAxis1)
            {
                result_old = true;
            }
        }
        else
        {
            if (semiMinorAxis1 >= semiMajorAxis2)
            {
                result_old = true;
            }
        }
        

        var result_new = true;

        // Fix:
        (double semiMinorAxis, double semiMajorAxis, double theta, double x0, double y0, MatrixD plane2world, MatrixD world2plane) ellipseCurrent;
        (double semiMinorAxis, double semiMajorAxis, double theta, double x0, double y0, MatrixD plane2world, MatrixD world2plane) ellipse2test;
        (double A, double B, double C, double D, double E, double F, MatrixD plane2world, MatrixD world2plane) ellipseOther;

        MatrixD obj1_to_world;
        MatrixD obj2_to_world;
        MatrixD world_to_obj2;
        if (isPrim1CurrentPrimitive)
        {
            // is ellipse1 totally inside ellipse2 ?
            ellipseCurrent = isSnoutCapTop1 ? rvmSnout1.GetTopEllipsePolarForm() : rvmSnout1.GetBottomEllipsePolarForm();
            ellipseOther = isSnoutCapTop2 ? rvmSnout2.GetTopEllipseImplicitForm() : rvmSnout2.GetBottomEllipseImplicitForm();
            ellipse2test = isSnoutCapTop2 ? rvmSnout2.GetTopEllipsePolarForm() : rvmSnout2.GetBottomEllipsePolarForm();
            obj1_to_world = ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout1.Matrix)).Multiply(scaleMat);// these matrices are stored as trans
            obj2_to_world = ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout2.Matrix)).Multiply(scaleMat);
            world_to_obj2 = ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout2.Matrix)).Multiply(scaleMat).Inverse();

        }
        else
        {
            ellipseCurrent = isSnoutCapTop2 ? rvmSnout2.GetTopEllipsePolarForm() : rvmSnout2.GetBottomEllipsePolarForm();
            ellipseOther = isSnoutCapTop1 ? rvmSnout1.GetTopEllipseImplicitForm() : rvmSnout1.GetBottomEllipseImplicitForm();
            ellipse2test = isSnoutCapTop1 ? rvmSnout1.GetTopEllipsePolarForm() : rvmSnout1.GetBottomEllipsePolarForm();
            obj1_to_world = ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout2.Matrix)).Multiply(scaleMat);
            obj2_to_world = ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout1.Matrix)).Multiply(scaleMat);
            world_to_obj2 = ConvertMatrix4x4ToMatrixDouble(Matrix4x4.Transpose(rvmSnout1.Matrix)).Multiply(scaleMat).Inverse();
        }

        var a_e1 = ellipseCurrent.semiMajorAxis;
        var b_e1 = ellipseCurrent.semiMinorAxis;
        var x0_e1 = ellipseCurrent.x0;
        var y0_e1 = ellipseCurrent.y0;

        var a_e2 = ellipse2test.semiMajorAxis;
        var b_e2 = ellipse2test.semiMinorAxis;
        var x0_e2 = ellipse2test.x0;
        var y0_e2 = ellipse2test.y0;

        var pt_local_e1 = new VectorD[4];
        pt_local_e1[0] = VectorD.Build.Dense(new double[] { a_e1 - x0_e1, -y0_e1, 0.0f, 1.0 });
        pt_local_e1[1] = VectorD.Build.Dense(new double[] { -x0_e1, b_e1 - y0_e1, 0.0f, 1.0 });
        pt_local_e1[2] = VectorD.Build.Dense(new double[] { -a_e1 - x0_e1, -y0_e1, 0.0f, 1.0 });
        pt_local_e1[3] = VectorD.Build.Dense(new double[] { -x0_e1, -b_e1 - y0_e1, 0.0f, 1.0 });

        var mat_stack = ellipseOther.world2plane * world_to_obj2 * obj1_to_world * ellipseCurrent.plane2world;

        var mat_stack1 = world_to_obj2 * obj1_to_world;
        var mat_stack2 = ellipseOther.world2plane * ellipseCurrent.plane2world;


        var pt_local_e2 = new VectorD[4];
        for (int i = 0; i < 4; i++)
        {
            pt_local_e2[i] = mat_stack.Multiply(pt_local_e1[i]);
        }

        const int x = 0;
        const int y = 1;

        // hide cap that is defined by the four points if they fully covered by the other cap
        // return if all if all points of current ellipse are inside the other ellipse

        var pt_local_e2_rot = new VectorD[4];
        var max_r = 0.0;
        for (int i = 0; i < 4; i++)
        {
            var diff_theta = ellipse2test.theta - ellipseCurrent.theta;
            pt_local_e2_rot[i] = pt_local_e2[i];
            pt_local_e2_rot[i][x] =
                pt_local_e2[i][x] * Math.Cos(diff_theta) +
                pt_local_e2[i][y] * Math.Sin(diff_theta);
            pt_local_e2_rot[i][y] =
                -pt_local_e2[i][x] * Math.Sin(diff_theta) +
                pt_local_e2[i][y] * Math.Cos(diff_theta);

            var part1 = (pt_local_e2_rot[i][x] - ellipse2test.x0) / ellipse2test.semiMajorAxis;
            var part2 = (pt_local_e2_rot[i][y] - ellipse2test.y0) / ellipse2test.semiMinorAxis;
            var d = part1 * part1 + part2 * part2;

            if (d > 1.0+(double)0.00001m) result_new = false;
            max_r = Math.Max(d, max_r);
        }


        var wasTruePositive = (result_old && result_new);
        var wasFalsePositive = (result_old && !result_new);
        var wasFalseNegative = (!result_old && result_new);
        var wasTrueNegatives = (!result_old && !result_new);

        if(wasFalsePositive)
            global_count_removed_caps++;

        return result_old;
    }

    private static bool OtherPrimitiveHasLargerOrEqualCap(
        RvmSnout rvmSnout,
        RvmSphericalDish rvmSphericalDish,
        uint rvmSnoutCapIndex,
        bool isPrim1CurrentPrimitive)
    {
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var isSnoutCapTop = rvmSnoutCapIndex == 0;

        var semiMinorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMinorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMinorAxis * snoutScale.X;

        var semiMajorRadius = isSnoutCapTop
            ? rvmSnout.GetTopEllipsePolarForm().semiMajorAxis * snoutScale.X
            : rvmSnout.GetBottomEllipsePolarForm().semiMajorAxis * snoutScale.X;

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