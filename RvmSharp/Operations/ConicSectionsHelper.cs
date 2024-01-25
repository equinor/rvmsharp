namespace RvmSharp.Operations;

using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Diagnostics;
using System.Numerics;
using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

public sealed record Ellipse2DImplicitForm(double A, double B, double C, double D, double E, double F);

public sealed record Ellipse2DPolarForm(
    double SemiMinorAxis,
    double SemiMajorAxis,
    double Theta,
    double X0,
    double Y0,
    Ellipse2DImplicitForm ImplicitEq
);

public sealed record Ellipse3D(Ellipse2DPolarForm Ellipse2DPolar, MatrixD PlaneToModelCoord, MatrixD ModelToPlaneCoord);

public sealed record PlaneImplicitForm(Vector3 Normal, float D);

public sealed record Cone(float BaseR, Vector3 Apex);

// helper class for calculating conic sections (cones and cylinders)
// cylinder can be considered a cone with its apex at infinity
public static class VectorAlgebraHelper
{
    public static MatrixD ConvertMatrix4X4ToMatrixDouble(Matrix4x4 mat)
    {
        return DenseMatrix.OfArray(
            new double[,]
            {
                { mat.M11, mat.M12, mat.M13, mat.M14 },
                { mat.M21, mat.M22, mat.M23, mat.M24 },
                { mat.M31, mat.M32, mat.M33, mat.M34 },
                { mat.M41, mat.M42, mat.M43, mat.M44 }
            }
        );
    }

    public static VectorD Cross(VectorD left, VectorD right)
    {
        VectorD result = new DenseVector(3);
        result[0] = left[1] * right[2] - left[2] * right[1];
        result[1] = -left[0] * right[2] + left[2] * right[0];
        result[2] = left[0] * right[1] - left[1] * right[0];

        return result;
    }

    public static double Dot(VectorD left, VectorD right)
    {
        var result = left[0] * right[0] + left[1] * right[1] + left[2] * right[2];
        return result;
    }

    public static MatrixD CreateUniformScale(double s)
    {
        return DenseMatrix.OfArray(
            new double[,]
            {
                { s, 0, 0, 0 },
                { 0, s, 0, 0 },
                { 0, 0, s, 0 },
                { 0, 0, 0, 1 }
            }
        );
    }

    public static (Vector3 right, Vector3 up, Vector3 view) CalcVectorBasisFromPlane(Vector3 planeNormal)
    {
        var view = -planeNormal;
        var upToProj = new Vector3(0.0f, 1.0f, 0.0f);
        if (planeNormal.Y == 1.0f)
        {
            upToProj = new Vector3(1.0f, 0.0f, 0.0f);
        }
        var up = upToProj - Vector3.Dot(upToProj, view) * view;
        up = Vector3.Normalize(up);

        var right = Vector3.Normalize(Vector3.Cross(up, view));

        return (right, up, view);
    }
}

public static class GeometryHelper
{
    public static PlaneImplicitForm GetPlaneFromShearAndPoint(float shearX, float shearY, Vector3 pt)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, shearY);
        var rotation = rotationAroundX * rotationAroundY;
        var normal = Vector3.Transform(Vector3.UnitZ, rotation);
        normal = Vector3.Normalize(normal);
        var dc = -Vector3.Dot(normal, pt);

        return new PlaneImplicitForm(normal, dc);
    }

    public static PlaneImplicitForm GetPlaneWithNormalPointingAwayFromOrigin(PlaneImplicitForm plane)
    {
        PlaneImplicitForm newPlane =
            (plane.D > 0.0f)
                ? new PlaneImplicitForm(-plane.Normal, -plane.D)
                : new PlaneImplicitForm(plane.Normal, plane.D);

        return newPlane;
    }
}

public static class ConicSectionsHelper
{
    public static readonly Ellipse2DImplicitForm ZeroEllipseImplicit = new Ellipse2DImplicitForm(
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        0.0
    );
    public static readonly Ellipse2DPolarForm ZeroEllipsePolar = new Ellipse2DPolarForm(
        0.0,
        0.0,
        0.0,
        0.0,
        0.0,
        ZeroEllipseImplicit
    );

    public static Cone CreateConeFromSnout(float bottomRadius, float topRadius, Vector3 offset)
    {
        // Assert (bottomRadius - topRadius) != 0.0

        float coneBaseRadius = (topRadius + bottomRadius) * 0.5f;
        var halfOffset = 0.5f * offset;

        if (topRadius == 0.0f)
            return new Cone(coneBaseRadius, halfOffset);

        var ratio = coneBaseRadius / topRadius;

        // apexZ / (apexZ - halfOffset.Z) = ratio
        // apexZ = (apexZ - halfOffset.Z) * ratio
        // apexZ = apexZ * ratio - halfOffset.Z * ratio
        // apex Z - apexZ * ratio = - halfOffset.Z * ratio
        // apexZ * (1.0 - ratio) = - halfOffset.Z * ratio
        // apexZ = - halfOffset.Z * ratio / (1.0 - ratio)
        // apexZ = halfOffset.Z * ratio / (ratio - 1.0)

        var apex = halfOffset * (ratio / (ratio - 1.0f));

        return new Cone(coneBaseRadius, apex);
    }

    // this function returns coefficients A,B,C,D,E,F that describe a general ellipse in an implicit form in a Cartesian plane z=0
    // Ax^2 + Bxy + Cy^2 + Dx + Ey + F = 0
    // ellipse in implicit form https://en.wikipedia.org/wiki/Ellipse#General_ellipse

    // OBS: the description is wrt to a local Cartesian plane z=0,
    // in order to embed the ellipse in 3D, we provide a transformation of this plane to 3D model coordinates
    // this is taken care of elsewhere

    // the ellipse is defined as a projection of a circle onto a plane (assumed z=0)
    // ellipse = matPV * circle
    // matPV: projection*view transformation matrix (assumes transformation of points from left!)
    // basisRadius: radius of the circle being projected
    // circleOffsetZ: z coordinate of the circle samples

    // algorithm:
    // take 6 sample points of the given circle
    // apply matPV transform so that they are located on the ellipse of interest
    // calculate coefficients A,B,C,D,E,F from the set of 6 transformed points
    // OBS: in theory only 5 points are be necessary, but we use 6 points to make the algorithm less complicated

    public static Ellipse2DImplicitForm CalcEllipseImplicitForm(MatrixD matPv, double basisRadius)
    {
        // for convenience of adressing homogeneous coordinates in an array
        const int x = 0;
        const int y = 1;
        const int w = 3;

        // setting up 6 sample points of the circle with some arbitrary theta
        // choice of theta is not so important as long as the points are not too close to each other,
        // this could lead to numerical problems otherwise

        var thetas = new double[6];
        thetas[0] = 0.05 * Math.PI;
        thetas[1] = Math.PI / 3.0f;
        thetas[2] = 2.0 * Math.PI / 3.0;
        thetas[3] = 0.9 * Math.PI;
        thetas[4] = 4.0 * Math.PI / 3.0;
        thetas[5] = 1.7 * Math.PI;

        var circleSamples = new VectorD[6];
        var projSam = new VectorD[6]; // projected samples
        var index = 0;
        foreach (var th in thetas)
        {
            circleSamples[index] = VectorD.Build.Dense(
                new double[] { basisRadius * Math.Cos(th), basisRadius * Math.Sin(th), 0.0, 1.0 }
            );
            projSam[index] = matPv.Multiply(circleSamples[index]);
            projSam[index] = projSam[index].Divide(projSam[index][w]);
            index++;
        }
        // the coeff_6x6 matrix represents the following system of equations using points (xi,yi) with i 1..6
        // NB: (x0,y0) is by tradition used to define the center/origin, so to avoid confusion, points are indexed 1..6, not 0..5
        // A*x1^2 + B*x1*y1 + C*y1^2 + D*x1 + E*y1 + F*1 = 0
        // A*x2^2 + B*x2*y2 + C*y2^2 + D*x2 + E*y2 + F*1 = 0
        // A*x3^2 + B*x3*y3 + C*y3^2 + D*x3 + E*y3 + F*1 = 0
        // A*x4^2 + B*x4*y4 + C*y4^2 + D*x4 + E*y4 + F*1 = 0
        // A*x5^2 + B*x5*y5 + C*y5^2 + D*x5 + E*y5 + F*1 = 0
        // A*x6^2 + B*x6*y6 + C*y6^2 + D*x6 + E*y6 + F*1 = 0
        // the system has a trivial solution, i.e., the null vector
        // or possible other solutions defined as the "null space" or the "kernel" of the coefficient matrix
        // csharpier-ignore
        MatrixD coeff6X6 = DenseMatrix.OfArray(new double[,] {
                { projSam[0][x] * projSam[0][x], projSam[0][x] * projSam[0][y], projSam[0][y] * projSam[0][y], projSam[0][x], projSam[0][y], 1.0 },
                { projSam[1][x] * projSam[1][x], projSam[1][x] * projSam[1][y], projSam[1][y] * projSam[1][y], projSam[1][x], projSam[1][y], 1.0 },
                { projSam[2][x] * projSam[2][x], projSam[2][x] * projSam[2][y], projSam[2][y] * projSam[2][y], projSam[2][x], projSam[2][y], 1.0 },
                { projSam[3][x] * projSam[3][x], projSam[3][x] * projSam[3][y], projSam[3][y] * projSam[3][y], projSam[3][x], projSam[3][y], 1.0 },
                { projSam[4][x] * projSam[4][x], projSam[4][x] * projSam[4][y], projSam[4][y] * projSam[4][y], projSam[4][x], projSam[4][y], 1.0 },
                { projSam[5][x] * projSam[5][x], projSam[5][x] * projSam[5][y], projSam[5][y] * projSam[5][y], projSam[5][x], projSam[5][y], 1.0 }
            });
        VectorD[] kernel;
        if (coeff6X6.Nullity() > 0.0) // nullity should be 0 or 1, otherwise we have a problem
        {
            kernel = coeff6X6.Kernel();

            // A..F initialization, might be later inverted to -A..-F
            var a = kernel[0][0];
            var b = kernel[0][1];
            var c = kernel[0][2];
            var d = kernel[0][3];
            var e = kernel[0][4];
            var f = kernel[0][5];

            // center point of the ellipse is not dependent of the inversion
            var x0 = (2 * c * d - a * e) / (b * b - 4.0 * a * c);
            var y0 = (2 * a * e - b * d) / (b * b - 4.0 * a * c);

            // check if point (x0, y0) is inside or outside of the ellipse
            // for all points inside the ellipse the following should be true:
            // Ax^2 + Bxy + Cy^2 + Dx + Ey + F < 0
            // invert the A..F vector if the center of the ellipse was outside!
            var sign = a * x0 * x0 + b * x0 * y0 + c * y0 * y0 + d * x0 + e * y0 + f;
            if (sign > 0.0)
            {
                kernel[0] = kernel[0].Multiply(-1.0);
            }

            // do not replace kernel by A..F here, maybe it was inverted!
            return new Ellipse2DImplicitForm(
                A: kernel[0][0],
                B: kernel[0][1],
                C: kernel[0][2],
                D: kernel[0][3],
                E: kernel[0][4],
                F: kernel[0][5]
            );
        }
        // the matrix does not have a null space, so the only solution is a null vector
        return ConicSectionsHelper.ZeroEllipseImplicit;
    }

    // converts general ellipse in implicit form to polar form
    // implicit form: Ax^2 + Bxy + Cy^2 + Dx + Ey + F = 0
    // polar form (x,y) = func(a, b, x0, y0, theta)
    // a: semi major axis
    // b: semi minor axis
    // (x0,y0): center of the ellipse in a Cartesian plane z=0 (base plane)
    // theta: angle [rad] of the semi minor axis and x-axis of the base plane

    // used conversion formulas: https://en.wikipedia.org/wiki/Ellipse#General_ellipse
    // have been verified by @vero-so

    public static Ellipse2DPolarForm ConvertEllipseImplicitToPolarForm(Ellipse2DImplicitForm el)
    {
        double a = el.A;
        double b = el.B;
        double c = el.C;
        double d = el.D;
        double e = el.E;
        double f = el.F;

        // aka semi major axis a
        var semiRadius1 =
            -Math.Sqrt(
                2.0
                    * (a * e * e + c * d * d - b * d * e + (b * b - 4.0 * a * c) * f)
                    * ((a + c) + Math.Sqrt((a - c) * (a - c) + b * b))
            ) / (b * b - 4.0 * a * c);
        var semiRadius2 =
            -Math.Sqrt(
                2.0
                    * (a * e * e + c * d * d - b * d * e + (b * b - 4.0 * a * c) * f)
                    * ((a + c) - Math.Sqrt((a - c) * (a - c) + b * b))
            ) / (b * b - 4.0 * a * c);

        //TODO: check if this possible switch affects theta
        var semiMajorRadius = Math.Max(semiRadius1, semiRadius2);
        var semiMinorRadius = Math.Min(semiRadius1, semiRadius2);

        b = (Math.Abs(b) < 0.00001) ? 0.0 : b;
        var diffAc = (Math.Abs(a - c) < 0.00001) ? 0.0 : (a - c);
        var theta =
            (Math.Abs(b) > 0.00001)
                ?
                //var theta = (Math.Abs(B) > 1.0e-12) ?
                Math.Atan((c - a - Math.Sqrt((a - c) * (a - c) + b * b) / b))
                : (diffAc <= 0.0)
                    ? 0.0
                    : Math.PI / 2.0;
        b = (Math.Abs(b) < 0.00001) ? 0.0 : theta;

        var x0 = (2 * c * d - a * e) / (b * b - 4.0 * a * c);
        var y0 = (2 * a * e - b * d) / (b * b - 4.0 * a * c);

        x0 = (Math.Abs(x0) < 0.00001) ? 0.0 : x0;
        y0 = (Math.Abs(y0) < 0.00001) ? 0.0 : y0;

        return new Ellipse2DPolarForm(semiMinorRadius, semiMajorRadius, theta, x0, y0, el);
    }

    public static double CalcDistancePointEllise(Ellipse2DPolarForm el, double px, double py)
    {
        var dx = px - el.X0;
        var dy = py - el.Y0;

        // quadratic equation for point on ellipse defined as X = C + k(P-C)
        // with P being pt_e1_snout2_xplane_local_coord, C is the center (x0,y0) and k is a parameter to be defined
        // distance to the ellipse is then D = |P-C| - |X-C|
        // A * (x0 +k(Px-xo))^2 + B * (x0 + k(Px-x0)*(y0 + k(Py-y0)) + ... + E * (y0 + k(Py-y0)) + F = 0
        // some transformations of the expression
        // k^2 * (sqFactor) + k * linFactor + constFactor = 0
        var sqFactor = el.ImplicitEq.A * dx * dx + el.ImplicitEq.B * dx * dy + el.ImplicitEq.C * dy * dy;
        var linFactor =
            el.ImplicitEq.A * 2.0 * el.X0 * dx
            + el.ImplicitEq.B * (el.X0 * dx + el.Y0 * dy)
            + el.ImplicitEq.C * 2.0 * el.Y0 * dy
            + el.ImplicitEq.D * dx
            + el.ImplicitEq.E * dy;
        // this is the equation for the constant factor:
        // var constFactor =
        //    el.implicitEq.A * el.x0 * el.x0 +
        //    el.implicitEq.B * el.x0 * el.y0 +
        //    el.implicitEq.C * el.y0 * el.y0 +
        //    el.implicitEq.D * el.x0 +
        //    el.implicitEq.E * el.y0 +
        //    el.implicitEq.F;
        // it evaluates to -1 as we put the center (x0,y0) of the ellipse into the equation of the same ellipse!
        var constFactor = -1.0;

        var discriminant = linFactor * linFactor - 4.0 * sqFactor * constFactor;

        if (discriminant <= 0.0 || Math.Abs(sqFactor) < 1e-18)
        {
            // distance is so small that it is almost impossible to represent numerically
            // k is probably like 1.0000000000000000000000000001
            // or it is too close to the center of the ellipse => bisetrix cannot be determined
            var d =
                el.ImplicitEq.A * px * px
                + el.ImplicitEq.B * px * py
                + el.ImplicitEq.C * py * py
                + el.ImplicitEq.D * px
                + el.ImplicitEq.E * py
                + el.ImplicitEq.F;
            Trace.Assert(d < 0.0 || Math.Abs(d) < 1e-9);
            return d;
        }
        else
        {
            var discriminantSqrt = Math.Sqrt(discriminant);

            var root1 = (-linFactor + discriminantSqrt) / (2.0 * sqFactor);
            var root2 = (-linFactor - discriminantSqrt) / (2.0 * sqFactor);
            var k = (root1 > 0) ? root1 : root2;
            Trace.Assert(
                k > 0.0,
                "Error in point-ellipse distance calculation. "
                    + $"One root is expected to be positive, but it was not. Root1: {root1} and root2: {root2}"
            );

            var xPointX = el.X0 + dx * k;
            var xPointY = el.Y0 + dy * k;

            var distX = px - xPointX;
            var distY = py - xPointY;

            var d = Math.Sqrt(distX * distX + distY * distY);
            if (k > 1.0)
            {
                d = -d;
            }

            return d;
        }
    }

    public static Ellipse3D CalcEllipseIntersectionForCone(PlaneImplicitForm capPlane, Cone cone)
    {
        PlaneImplicitForm xPlane = GeometryHelper.GetPlaneWithNormalPointingAwayFromOrigin(capPlane);

        (var rightVec, var upVec, var viewVec) = VectorAlgebraHelper.CalcVectorBasisFromPlane(xPlane.Normal);

        // distance of the apex to the intersection plane (cap)
        var zn = Vector3.Dot(xPlane.Normal, cone.Apex) + xPlane.D;

        // project apex to the cap plane
        Vector3 originOfPlane = cone.Apex - zn * xPlane.Normal;

        var transformPlaneToModelCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { rightVec.X, upVec.X, viewVec.X, originOfPlane.X },
                { rightVec.Y, upVec.Y, viewVec.Y, originOfPlane.Y },
                { rightVec.Z, upVec.Z, viewVec.Z, originOfPlane.Z },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var transformModelToPlaneCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { rightVec.X, rightVec.Y, rightVec.Z, -Vector3.Dot(rightVec, originOfPlane) },
                { upVec.X, upVec.Y, upVec.Z, -Vector3.Dot(upVec, originOfPlane) },
                { viewVec.X, viewVec.Y, viewVec.Z, -Vector3.Dot(viewVec, originOfPlane) },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        if (zn != 0.0)
        {
            var viewMat = DenseMatrix.OfArray(
                new double[,]
                {
                    { rightVec.X, rightVec.Y, rightVec.Z, -Vector3.Dot(cone.Apex, rightVec) },
                    { upVec.X, upVec.Y, upVec.Z, -Vector3.Dot(cone.Apex, upVec) },
                    { viewVec.X, viewVec.Y, viewVec.Z, -Vector3.Dot(cone.Apex, viewVec) },
                    { 0.0, 0.0, 0.0, 1.0 }
                }
            );
            var projMat = DenseMatrix.OfArray(
                new double[,]
                {
                    { zn, 0.0, 0.0, 0.0 },
                    { 0.0, zn, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 0.0 },
                    { 0.0, 0.0, -1.0, 0.0 }
                }
            );
            var pvMat = projMat * viewMat;

            Ellipse2DImplicitForm ellImpl = CalcEllipseImplicitForm(pvMat, cone.BaseR);

            var ellipsePolar = ConvertEllipseImplicitToPolarForm(ellImpl);
            return new Ellipse3D(ellipsePolar, transformPlaneToModelCoord, transformModelToPlaneCoord);
        }

        return new Ellipse3D(ZeroEllipsePolar, transformPlaneToModelCoord, transformModelToPlaneCoord);
    }

    public static Ellipse3D CalcEllipseIntersectionForCylinder(PlaneImplicitForm capPlane, float baseR, Vector3 origin)
    {
        PlaneImplicitForm xPlane = GeometryHelper.GetPlaneWithNormalPointingAwayFromOrigin(capPlane);

        var eye = origin; // eye is placed in the origin

        (var rightVec, var upVec, var viewVec) = VectorAlgebraHelper.CalcVectorBasisFromPlane(xPlane.Normal);

        var viewMat = DenseMatrix.OfArray(
            new double[,]
            {
                { 1.0, 0.0, 0.0, -eye.X },
                { 0.0, 1.0, 0.0, -eye.Y },
                { 0.0, 0.0, 1.0, -eye.Z },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var rotToPlane = DenseMatrix.OfArray(
            new double[,]
            {
                { rightVec.X, rightVec.Y, rightVec.Z, 0.0 },
                { upVec.X, upVec.Y, upVec.Z, 0.0 },
                { viewVec.X, viewVec.Y, viewVec.Z, 0.0 },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        // TODO: this should take account of oblique cylinders as well
        var obliqueProjMat = DenseMatrix.OfArray(
            new double[,]
            {
                { 1.0, 0.0, 0.0, 0.0 },
                { 0.0, 1.0, 0.0, 0.0 },
                { -xPlane.Normal.X / xPlane.Normal.Z, -xPlane.Normal.Y / xPlane.Normal.Z, 0.0, 0.0 },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var proj = DenseMatrix.OfArray(
            new double[,]
            {
                { 1.0, 0.0, 0.0, 0.0 },
                { 0.0, 1.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0, 0.0 },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var pvMat = proj * rotToPlane * obliqueProjMat * viewMat;

        var ellipseImplicitForm = CalcEllipseImplicitForm(pvMat, baseR);

        var transformPlaneToModelCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { rightVec.X, upVec.X, viewVec.X, eye.X },
                { rightVec.Y, upVec.Y, viewVec.Y, eye.Y },
                { rightVec.Z, upVec.Z, viewVec.Z, eye.Z },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var transformModelToPlaneCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { rightVec.X, rightVec.Y, rightVec.Z, -Vector3.Dot(rightVec, eye) },
                { upVec.X, upVec.Y, upVec.Z, -Vector3.Dot(upVec, eye) },
                { viewVec.X, viewVec.Y, viewVec.Z, -Vector3.Dot(viewVec, eye) },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var ellipsePolar = ConvertEllipseImplicitToPolarForm(ellipseImplicitForm);
        return new Ellipse3D(ellipsePolar, transformPlaneToModelCoord, transformModelToPlaneCoord);
    }

    public static Ellipse3D CalcEllipseIntersectionForCylinderWithZeroCapSlope(float radius, Vector3 origin)
    {
        var transformPlaneToModelCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { -1.0, 0.0, 0.0, origin.X },
                { 0.0, 1.0, 0.0, origin.Y },
                { 0.0, 0.0, -1.0, origin.Z },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );
        var transformModelToPlaneCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { -1.0, 0.0, 0.0, origin.X },
                { 0.0, 1.0, 0.0, -origin.Y },
                { 0.0, 0.0, -1.0, origin.Z },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var rsq = radius * radius;
        var elImplicit = new Ellipse2DImplicitForm(1.0 / rsq, 0.0, 1.0 / rsq, 0.0, 0.0, -1.0);
        var elPolar = new Ellipse2DPolarForm(radius, radius, 0.0, 0.0, 0.0, elImplicit);
        return new Ellipse3D(elPolar, transformPlaneToModelCoord, transformModelToPlaneCoord);
    }

    public static Ellipse3D CreateDegenerateEllipse(PlaneImplicitForm capPlane, Cone cone)
    {
        PlaneImplicitForm xPlane = GeometryHelper.GetPlaneWithNormalPointingAwayFromOrigin(capPlane);

        (var rightVec, var upVec, var viewVec) = VectorAlgebraHelper.CalcVectorBasisFromPlane(xPlane.Normal);

        // distance of the apex to the intersection plane (cap)
        var zn = Vector3.Dot(xPlane.Normal, cone.Apex) + xPlane.D;

        // project apex to the cap plane
        Vector3 originModelCoord = cone.Apex - zn * xPlane.Normal;

        var transformPlaneToModelCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { rightVec.X, upVec.X, viewVec.X, originModelCoord.X },
                { rightVec.Y, upVec.Y, viewVec.Y, originModelCoord.Y },
                { rightVec.Z, upVec.Z, viewVec.Z, originModelCoord.Z },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        var transformModelToPlaneCoord = DenseMatrix.OfArray(
            new double[,]
            {
                { rightVec.X, rightVec.Y, rightVec.Z, -Vector3.Dot(rightVec, originModelCoord) },
                { upVec.X, upVec.Y, upVec.Z, -Vector3.Dot(upVec, originModelCoord) },
                { viewVec.X, viewVec.Y, viewVec.Z, -Vector3.Dot(viewVec, originModelCoord) },
                { 0.0, 0.0, 0.0, 1.0 }
            }
        );

        return new Ellipse3D(ZeroEllipsePolar, transformPlaneToModelCoord, transformModelToPlaneCoord);
    }
}
