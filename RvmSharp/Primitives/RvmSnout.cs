namespace RvmSharp.Primitives;

using System;
using System.Numerics;

using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;

using VectorD = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MatrixD = MathNet.Numerics.LinearAlgebra.Matrix<double>;

public record RvmSnout(
    uint Version,
    Matrix4x4 Matrix,
    RvmBoundingBox BoundingBoxLocal,
    float RadiusBottom,
    float RadiusTop,
    float Height,
    float OffsetX,
    float OffsetY,
    float BottomShearX,
    float BottomShearY,
    float TopShearX,
    float TopShearY) : RvmPrimitive(Version,
    RvmPrimitiveKind.Snout,
    Matrix,
    BoundingBoxLocal)
{
    public bool HasShear()
    {
        return BottomShearX != 0 ||
               BottomShearY != 0 ||
               TopShearX != 0 ||
               TopShearY != 0;
    }

    public bool IsEccentric()
    {
        return OffsetX != 0 ||
               OffsetY != 0;
    }

    public static (Vector3 normal, float dc) GetPlaneFromShearAndPoint(float shearX, float shearY, Vector3 pt)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, shearY);
        var rotation = rotationAroundX * rotationAroundY;
        var normal = Vector3.Transform(Vector3.UnitZ, rotation);
        normal = Vector3.Normalize(normal);
        var dc = -Vector3.Dot(normal, pt);

        return (normal, dc);
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


    private (double A, double B, double C, double D, double E, double F) CalcEllipseImplicitForm(MatrixD matPV, double basisRadius, double circleOffsetZ)
    {
        // for convenience of adressing homogeneous coordinates in an array
        const int x = 0;
        const int y = 1;
        const int w = 3;

        // setting up 6 sample points of the circle with some arbitrary theta
        // choice of theta is not so important as long as the points are not too close to each other,
        // this could lead to numerical problems otherwise

        var thetas = new double[6];
        thetas[0] = 0.05* Math.PI;
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

        // this matrix represents the following system of equations using points (xi,yi) with i 1..6
        // NB: (x0,y0) is by tradition used to define the center, so to avoid confusion points are indexed 1..6, not 0..5

        // Ax1^2 + Bx1y1 + Cy1^2 + Dx1 + Ey1 + F = 0
        // Ax2^2 + Bx2y2 + Cy2^2 + Dx2 + Ey2 + F = 0
        // Ax3^2 + Bx3y3 + Cy3^2 + Dx3 + Ey3 + F = 0
        // Ax4^2 + Bx4y4 + Cy4^2 + Dx4 + Ey4 + F = 0
        // Ax5^2 + Bx5y5 + Cy5^2 + Dx5 + Ey5 + F = 0
        // Ax6^2 + Bx6y6 + Cy6^2 + Dx6 + Ey6 + F = 0

        // the system has a trivial solution, the null vector
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
            var A = kernel[0][0];
            var B = kernel[0][1];
            var C = kernel[0][2];
            var D = kernel[0][3];
            var E = kernel[0][4];
            var F = kernel[0][5];
            var x0 = (2 * C * D - A * E) / (B * B - 4.0 * A * C);
            var y0 = (2 * A * E - B * D) / (B * B - 4.0 * A * C);

            var sign = A * x0 * x0 + B * x0 * y0 + C * y0 * y0 + D * x0 + E * y0 + F;
            // make sure A is always positive in order to be able to do the inside/outside test correctly
            if (sign > 0.0)
            {
                kernel[0] = kernel[0].Multiply(-1.0);
            }
            return (kernel[0][0], kernel[0][1], kernel[0][2], kernel[0][3], kernel[0][4], kernel[0][5]);
        }
        // the matrix does not have a null space, so the only solution is a null vector
        return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
    }

    private (double semiMinorAxis,
        double semiMajorAxis,
        double theta,
        double x0,
        double y0) ConvertEllipseImplicitToPolarForm(double A, double B, double C, double D, double E, double F)
    {
        // aka semi major axis a
        var semiRadius1 = -Math.Sqrt(2.0 * (A * E * E + C * D * D - B * D * E + (B * B - 4.0 * A * C) * F) * ((A + C) + Math.Sqrt((A - C) * (A - C) + B * B))) / (B * B - 4.0 * A * C);
        var semiRadius2 = -Math.Sqrt(2.0 * (A * E * E + C * D * D - B * D * E + (B * B - 4.0 * A * C) * F) * ((A + C) - Math.Sqrt((A - C) * (A - C) + B * B))) / (B * B - 4.0 * A * C);

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

        return (semiMinorRadius, semiMajorRadius, theta, x0, y0);
    }

    private (double A, double B, double C, double D, double E, double F, MatrixD xplane_to_model, MatrixD model_to_xplane)
        CalcEllipseIntersectionForCone(Vector3 plane_normal, float plane_dc, Vector3 cone_apex, float cone_base_r, float cone_base_z_offset, Vector3 origo_offset)
    {
        if (plane_dc > 0.0f)
        {
            plane_normal = -plane_normal;
            plane_dc = -plane_dc;
        }

        var view_vec = -plane_normal;
        var up_to_proj = new Vector3(0.0f, 1.0f, 0.0f);
        if (plane_normal.Y == 1.0f)
        {
            up_to_proj = new Vector3(1.0f, 0.0f, 0.0f);
        }
        var up_vec = up_to_proj - Vector3.Dot(up_to_proj, view_vec) * view_vec;
        up_vec = Vector3.Normalize(up_vec);

        var right_vec = Vector3.Normalize(Vector3.Cross(up_vec, view_vec));

        var zn = Vector3.Dot(plane_normal, cone_apex) + plane_dc;

        Vector3 origin = cone_apex - zn * plane_normal - origo_offset;

        var planexy_to_world = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, up_vec.X, view_vec.X, origin.X},
            { right_vec.Y, up_vec.Y, view_vec.Y, origin.Y},
            { right_vec.Z, up_vec.Z, view_vec.Z, origin.Z},
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var world_to_planexy = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(right_vec, origin) },
            { up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(up_vec, origin) },
            { view_vec.X, view_vec.Y, view_vec.Z, -Vector3.Dot(view_vec, origin) },
            { 0.0, 0.0, 0.0, 1.0 }
        });


        if (zn != 0.0)
        {
            var view_mat = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(cone_apex, right_vec) },
            { up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(cone_apex, up_vec) },
            { view_vec.X, view_vec.Y, view_vec.Z, -Vector3.Dot(cone_apex, view_vec) },
            {0.0, 0.0, 0.0, 1.0}
            });
            var proj_mat = DenseMatrix.OfArray(new double[,] {
            { zn, 0.0,  0.0, 0.0 },
            { 0.0, zn,  0.0, 0.0 },
            { 0.0, 0.0, 0.0, 0.0 },
            { 0.0, 0.0, -1.0, 0.0}
            });
            var PV_mat = proj_mat * view_mat;

            // plane base z offset is non zero if bottom radius is 0, because then it is also the apex
            // in this case, we take the top cap
            (var A, var B, var C, var D, var E, var F) = CalcEllipseImplicitForm(PV_mat, cone_base_r, cone_base_z_offset);

            return (A, B, C, D, E, F, planexy_to_world, world_to_planexy);
        }

        return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, planexy_to_world, world_to_planexy);
    }

    private (double A, double B, double C, double D, double E, double F, MatrixD xplane_to_model, MatrixD model_to_xplane)
        CalcEllipseIntersectionForCylinder(Vector3 plane_normal, Vector3 pt_on_plane, float base_r, Vector3 offset)
    {

        var eye = pt_on_plane; // also the origin

        if (-Vector3.Dot(plane_normal, eye) > 0.0f)
        {
            plane_normal = -plane_normal;
        }

        var view_vec = -plane_normal;
        var up_to_proj = new Vector3(0.0f, 1.0f, 0.0f);
        if (plane_normal.Y == 1.0f)
        {
            up_to_proj = new Vector3(1.0f, 0.0f, 0.0f);
        }
        var up_vec = up_to_proj - Vector3.Dot(up_to_proj, view_vec) * view_vec;
        up_vec = Vector3.Normalize(up_vec);

        var right_vec = Vector3.Normalize(Vector3.Cross(up_vec, view_vec));


        var view_mat = DenseMatrix.OfArray(new double[,] {
            { 1.0, 0.0, 0.0, -eye.X },
            { 0.0, 1.0, 0.0, -eye.Y },
            { 0.0, 0.0, 1.0, -eye.Z },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        // TODO: this should take account of oblique cylinders as well
        var oblique_proj_mat = DenseMatrix.OfArray(new double[,] {
            { 1.0, 0.0, 0.0, 0.0 },
            { 0.0, 1.0, 0.0, 0.0 },
            { -plane_normal.X / plane_normal.Z, -plane_normal.Y / plane_normal.Z, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
         });

        var rot_to_plane = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, 0.0 },
            { up_vec.X, up_vec.Y, up_vec.Z, 0.0 },
            { view_vec.X, view_vec.Y, view_vec.Z, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var proj = DenseMatrix.OfArray(new double[,] {
            { 1.0, 0.0, 0.0, 0.0 },
            { 0.0, 1.0, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var PV_mat = proj * rot_to_plane * oblique_proj_mat * view_mat;

        (double A, double B, double C, double D, double E, double F) = CalcEllipseImplicitForm(PV_mat, base_r, eye.Z);

        //Vector3 origin = eye - Vector3.Dot(eye, plane_normal) * plane_normal;

        Vector3 origin = eye - offset;
        var planexy_to_world = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, up_vec.X, view_vec.X, origin.X },
            { right_vec.Y, up_vec.Y, view_vec.Y, origin.Y },
            { right_vec.Z, up_vec.Z, view_vec.Z, origin.Z },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var world_to_planexy = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(right_vec, origin) },
            { up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(up_vec, origin) },
            { view_vec.X, view_vec.Y, view_vec.Z, -Vector3.Dot(view_vec, origin) },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        return (A, B, C, D, E, F, planexy_to_world, world_to_planexy);
    }

    private Vector3 GetConeApex()
    {
     
        Vector3 apex;

        if (RadiusBottom > RadiusTop)
        {
            var hdiff = Height * RadiusTop / (RadiusBottom - RadiusTop);
            var xdiff = OffsetX * RadiusTop / (RadiusBottom - RadiusTop);
            var ydiff = OffsetY * RadiusTop / (RadiusBottom - RadiusTop);
            var TopCenter = new Vector3(OffsetX, OffsetY, Height);
            apex = TopCenter + (new Vector3(xdiff, ydiff, hdiff));
        }
        else
        {
            var hdiff = Height * RadiusBottom / (RadiusTop - RadiusBottom);
            var xdiff = OffsetX * RadiusBottom / (RadiusTop - RadiusBottom);
            var ydiff = OffsetY * RadiusBottom / (RadiusTop - RadiusBottom);
            apex = new Vector3(-xdiff, -ydiff, -hdiff);
        }

        return apex;
    }


    public (double semiMinorAxis, double semiMajorAxis, double theta, double x0, double y0, MatrixD xplane_to_model, MatrixD model_to_xplane)
        GetTopEllipsePolarForm()
    {

        //var OriginOffset = (new Vector3(OffsetX, OffsetY, Height)) / 2.0f;
        var OriginOffset = (new Vector3(OffsetX, OffsetY, Height));

        // cones
        if (Math.Abs(RadiusBottom - RadiusTop) > (double)0.00001m)
        {
            var apex = GetConeApex();

            var TopCenter = new Vector3(OffsetX, OffsetY, Height);

            // plane that is defined by the top cap
            (var normal, var dc) = GetPlaneFromShearAndPoint(TopShearX, TopShearY, TopCenter);

            var R = (RadiusBottom > 0.0) ? RadiusBottom : RadiusTop;
            var base_offset_z = (RadiusBottom > 0.0) ? 0.0f : Height;

            (var A, var B, var C, var D, var E, var F, var xplane_to_model, var model_to_xplane) =
                CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z, OriginOffset);

            if (Math.Abs(RadiusTop) < (double)0.00001m)
            {
                return (0.0, 0.0, 0.0, 0.0, 0.0, xplane_to_model, model_to_xplane);
            }

            (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
            return (b, a, theta, x0, y0, xplane_to_model, model_to_xplane);

        }
        //cylinders
        else
        {
            var slope = GetTopSlope().slope;

            // the most trivial case, cylinder with zero slope
            if(slope == 0 )
            {
                var planexy_to_model = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, -1.0, OriginOffset.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                var model_to_planexy = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, -1.0, -OriginOffset.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });

                return (RadiusTop, RadiusTop, 0.0f, 0.0f, 0.0f, planexy_to_model, model_to_planexy);
            }
            else
            {
                var Offset = new Vector3(OffsetX, OffsetY, Height);
                (var normal, _) = GetPlaneFromShearAndPoint(TopShearX, TopShearY, Offset);
                (var A, var B, var C, var D, var E, var F, var xplane_to_model, var model_to_xplane) =
                    CalcEllipseIntersectionForCylinder(normal, Offset, RadiusTop, OriginOffset);
                (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
                return ((float)b, (float)a, (float)theta, (float)x0, (float)y0, xplane_to_model, model_to_xplane);
                
            }
        }
    }

    public (double semiMinorAxis, double semiMajorAxis, double theta, double x0, double y0, MatrixD xplane_to_model, MatrixD model_to_xplane)
        GetBottomEllipsePolarForm()
    {
        //var OriginOffset = -(new Vector3(OffsetX, OffsetY, Height)) / 2.0f;
        var OriginOffset = new Vector3(0.0f, 0.0f, 0.0f);

        // cones
        if (Math.Abs(RadiusBottom - RadiusTop) > (double)0.00001m)
        {
            var apex = GetConeApex();
            var R = (RadiusBottom > 0.0) ? RadiusBottom : RadiusTop;

            // plane that is defined by the top cap
            var BottomCenter = new Vector3(0.0f, 0.0f, 0.0f);
            (var normal, var dc) = GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, BottomCenter);

            (var A, var B, var C, var D, var E, var F, MatrixD xplane_to_model, MatrixD model_to_xplane) =
                CalcEllipseIntersectionForCone(normal, dc, apex, R, 0.0f, OriginOffset);

            if (Math.Abs(RadiusBottom) < (double)0.00001m)
            {
                return (0.0, 0.0, 0.0, 0.0, 0.0, xplane_to_model, model_to_xplane);
            }

            (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
            return (b, a, theta, x0, y0, xplane_to_model, model_to_xplane);

        }
        //cylinders
        else
        {
            var slope = GetTopSlope().slope;

            // the most trivial case, cylinder with zero slope
            if (slope == 0)
            {
                var planexy_to_model = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, -1.0, OriginOffset.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                var model_to_planexy = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, -1.0, -OriginOffset.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                //var world_to_planexy = planexy_to_world;
                return (RadiusBottom, RadiusBottom, 0.0f, 0.0f, 0.0f, planexy_to_model, model_to_planexy);
            }
            else
            {
                var Offset = new Vector3(0.0f, 0.0f, 0.0f);
                (var normal, _) = GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, Offset);

                (var A, var B, var C, var D, var E, var F, MatrixD xplane_to_model, MatrixD model_to_xplane) =
                    CalcEllipseIntersectionForCylinder(normal, Offset, RadiusBottom, OriginOffset);
                (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
                return ((float)b, (float)a, (float)theta, (float)x0, (float)y0, xplane_to_model, model_to_xplane);

            }
        }
    }


    public (Quaternion rotation, Vector3 normal, float slope) GetTopSlope()
    {
        return TranslateShearToSlope(TopShearX, TopShearY);
    }

    public (Quaternion rotation, Vector3 normal, float slope) GetBottomSlope()
    {
        return TranslateShearToSlope(BottomShearX, BottomShearY);
    }

    private (Quaternion rotation, Vector3 normal, float slope) TranslateShearToSlope(float shearX, float shearY)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, shearY);
        var rotation = rotationAroundX * rotationAroundY;
        var normal = Vector3.Transform(Vector3.UnitZ, rotation);
        var slope = MathF.PI / 2f - MathF.Atan2(normal.Z, MathF.Sqrt(normal.X * normal.X + normal.Y * normal.Y));

        float rotZAmount = 0;

        if (shearX != 0 || shearY != 0)
        {
            rotZAmount = (shearX / (shearX + shearY)) * MathF.PI / 2;
        }

        Quaternion rotationAroundZ = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotZAmount);

        rotation *= rotationAroundZ;

        return (rotation, normal, slope);
    }
};