namespace RvmSharp.Operations;
using System;

using MathNet.Numerics.LinearAlgebra.Double;

using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

public sealed record EllipseImplicitForm(double A, double B, double C, double D, double E, double F);
public sealed record EllipsePolarForm(double semiMinorAxis, double semiMajorAxis, double theta, double x0, double y0);

public static class ConicSectionsHelper
{
    public static readonly EllipseImplicitForm zeroEllipseImplicit = new EllipseImplicitForm(0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
    public static readonly EllipsePolarForm zeroEllipsePolar = new EllipsePolarForm(0.0, 0.0, 0.0, 0.0, 0.0);

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

    public static EllipseImplicitForm CalcEllipseImplicitForm(MatrixD matPV, double basisRadius, double circleOffsetZ)
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
            circleSamples[index] = VectorD.Build.Dense(new double[] {
                basisRadius * Math.Cos(th),
                basisRadius * Math.Sin(th),
                circleOffsetZ,
                1.0});
            projSam[index] = matPV.Multiply(circleSamples[index]);
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

        MatrixD coeff_6x6 = DenseMatrix.OfArray(new double[,] {
                { projSam[0][x] * projSam[0][x], projSam[0][x] * projSam[0][y], projSam[0][y] * projSam[0][y], projSam[0][x], projSam[0][y], 1.0 },
                { projSam[1][x] * projSam[1][x], projSam[1][x] * projSam[1][y], projSam[1][y] * projSam[1][y], projSam[1][x], projSam[1][y], 1.0 },
                { projSam[2][x] * projSam[2][x], projSam[2][x] * projSam[2][y], projSam[2][y] * projSam[2][y], projSam[2][x], projSam[2][y], 1.0 },
                { projSam[3][x] * projSam[3][x], projSam[3][x] * projSam[3][y], projSam[3][y] * projSam[3][y], projSam[3][x], projSam[3][y], 1.0 },
                { projSam[4][x] * projSam[4][x], projSam[4][x] * projSam[4][y], projSam[4][y] * projSam[4][y], projSam[4][x], projSam[4][y], 1.0 },
                { projSam[5][x] * projSam[5][x], projSam[5][x] * projSam[5][y], projSam[5][y] * projSam[5][y], projSam[5][x], projSam[5][y], 1.0 }
            });
        VectorD[] kernel;
        if (coeff_6x6.Nullity() > 0.0) // nullity should be 0 or 1, otherwise we have a problem
        {
            kernel = coeff_6x6.Kernel();

            // A..F initialization, might be later inverted to -A..-F
            var A = kernel[0][0];
            var B = kernel[0][1];
            var C = kernel[0][2];
            var D = kernel[0][3];
            var E = kernel[0][4];
            var F = kernel[0][5];

            // center point of the ellipse is not dependent of the inversion
            var x0 = (2 * C * D - A * E) / (B * B - 4.0 * A * C);
            var y0 = (2 * A * E - B * D) / (B * B - 4.0 * A * C);

            // check if point (x0, y0) is inside or outside of the ellipse
            // for all points inside the ellipse the following should be true:
            // Ax^2 + Bxy + Cy^2 + Dx + Ey + F < 0
            // invert the A..F vector if the center of the ellipse was outside!
            var sign = A * x0 * x0 + B * x0 * y0 + C * y0 * y0 + D * x0 + E * y0 + F;
            if (sign > 0.0)
            {
                kernel[0] = kernel[0].Multiply(-1.0);
            }

            // do not replace kernel by A..F here, maybe it was inverted!
            return new EllipseImplicitForm(kernel[0][0], kernel[0][1], kernel[0][2], kernel[0][3], kernel[0][4], kernel[0][5]);
        }
        // the matrix does not have a null space, so the only solution is a null vector
        return ConicSectionsHelper.zeroEllipseImplicit;
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

    public static EllipsePolarForm ConvertEllipseImplicitToPolarForm(EllipseImplicitForm el)
    {
        double A = el.A;
        double B = el.B;
        double C = el.C;
        double D = el.D;
        double E = el.E;
        double F = el.F;

        // aka semi major axis a
        var semiRadius1 = -Math.Sqrt(2.0 * (A * E * E + C * D * D - B * D * E + (B * B - 4.0 * A * C) * F) * ((A + C) + Math.Sqrt((A - C) * (A - C) + B * B))) / (B * B - 4.0 * A * C);
        var semiRadius2 = -Math.Sqrt(2.0 * (A * E * E + C * D * D - B * D * E + (B * B - 4.0 * A * C) * F) * ((A + C) - Math.Sqrt((A - C) * (A - C) + B * B))) / (B * B - 4.0 * A * C);

        //TODO: check if this possible switch affects theta
        var semiMajorRadius = Math.Max(semiRadius1, semiRadius2);
        var semiMinorRadius = Math.Min(semiRadius1, semiRadius2);

        B = (Math.Abs(B) < (double)0.00001m) ? 0.0 : B;
        var diffAC = (Math.Abs(A - C) < (double)0.00001m) ? 0.0 : (A - C);
        var theta = (Math.Abs(B) > (double)0.00001m) ?
        //var theta = (Math.Abs(B) > 1.0e-12) ?
            Math.Atan((C - A - Math.Sqrt((A - C) * (A - C) + B * B) / B)) :
            (diffAC <= 0.0) ? 0.0 : Math.PI / 2.0;
        B = (Math.Abs(B) < (double)0.00001m) ? 0.0 : theta;

        var x0 = (2 * C * D - A * E) / (B * B - 4.0 * A * C);
        var y0 = (2 * A * E - B * D) / (B * B - 4.0 * A * C);

        x0 = (Math.Abs(x0) < (double)0.00001m) ? 0.0 : x0;
        y0 = (Math.Abs(y0) < (double)0.00001m) ? 0.0 : y0;

        return new EllipsePolarForm(semiMinorRadius, semiMajorRadius, theta, x0, y0);
    }
}
