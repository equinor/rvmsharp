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

    private (Vector3 normal, float dc) GetPlaneFromShearAndPoint(float shearX, float shearY, Vector3 pt)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, shearY);
        var rotation = rotationAroundX * rotationAroundY;
        var normal = Vector3.Transform(Vector3.UnitZ, rotation);
        normal = Vector3.Normalize(normal);
        var dc = -Vector3.Dot(normal, pt);

        return (normal, dc);
    }

    private (double A, double B, double C, double D, double E, double F) CalcEllipseImplicitForm(MatrixD pv_mat, double basis_r, double base_offsetz)
    {
        var thetas = new double[6];
        thetas[0] = 0.05* Math.PI;
        thetas[1] = Math.PI / 3.0f;
        thetas[2] = 2.0 * Math.PI / 3.0;
        thetas[3] = 0.9 * Math.PI;
        thetas[4] = 4.0 * Math.PI / 3.0;
        thetas[5] = 1.7 * Math.PI;

        const int x = 0;
        const int y = 1;
        const int z = 2;
        const int w = 3;

        var circle_samples = new VectorD[6];
        var sam_prj = new VectorD[6];
        var index = 0;
        foreach (var th in thetas)
        {
            circle_samples[index] = VectorD.Build.Dense(new double[] { basis_r* Math.Cos(th), basis_r* Math.Sin(th), (double)base_offsetz, (double)1.0});
            // for some strange reason the transform function multiplies the vector and the matrix w the vector on the left
            sam_prj[index] = pv_mat.Multiply(circle_samples[index]);
            sam_prj[index] = sam_prj[index].Divide(sam_prj[index][w]);
            index++;
        }

        
        MatrixD coeff_6x6 = DenseMatrix.OfArray(new double[,] {
                { sam_prj[0][x] * sam_prj[0][x], sam_prj[0][x] * sam_prj[0][y], sam_prj[0][y] * sam_prj[0][y], sam_prj[0][x], sam_prj[0][y], 1.0 },
                { sam_prj[1][x] * sam_prj[1][x], sam_prj[1][x] * sam_prj[1][y], sam_prj[1][y] * sam_prj[1][y], sam_prj[1][x], sam_prj[1][y], 1.0 },
                { sam_prj[2][x] * sam_prj[2][x], sam_prj[2][x] * sam_prj[2][y], sam_prj[2][y] * sam_prj[2][y], sam_prj[2][x], sam_prj[2][y], 1.0 },
                { sam_prj[3][x] * sam_prj[3][x], sam_prj[3][x] * sam_prj[3][y], sam_prj[3][y] * sam_prj[3][y], sam_prj[3][x], sam_prj[3][y], 1.0 },
                { sam_prj[4][x] * sam_prj[4][x], sam_prj[4][x] * sam_prj[4][y], sam_prj[4][y] * sam_prj[4][y], sam_prj[4][x], sam_prj[4][y], 1.0 },
                { sam_prj[5][x] * sam_prj[5][x], sam_prj[5][x] * sam_prj[5][y], sam_prj[5][y] * sam_prj[5][y], sam_prj[5][x], sam_prj[5][y], 1.0 }
            });

        VectorD[] kernel;
        if (coeff_6x6.Nullity() > 0.0)
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

        var x0 = (2 * C * D - A * E) / (B * B - 4.0 * A * C);
        var y0 = (2 * A * E - B * D) / (B * B - 4.0 * A * C);

        x0 = (Math.Abs(x0) < (double)0.00001m) ? 0.0 : x0;
        y0 = (Math.Abs(y0) < (double)0.00001m) ? 0.0 : y0;

        return (semiMinorRadius, semiMajorRadius, theta, x0, y0);
    }

    private (double A, double B, double C, double D, double E, double F, MatrixD plane2world, MatrixD world2plane)
        CalcEllipseIntersectionForCone(Vector3 plane_normal, float plane_dc, Vector3 cone_apex, float cone_base_r, float base_z_offset)
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

        Vector3 origin = cone_apex - Vector3.Dot(cone_apex, plane_normal) * plane_normal;

        var planexy_to_world = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, up_vec.X, 0.0f, origin.X },
            { right_vec.Y, up_vec.Y, 0.0f, origin.Y },
            { right_vec.Z, up_vec.Z, 0.0f, origin.Z },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var world_to_planexy = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(right_vec, origin) },
            { up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(up_vec, origin) },
            { 0.0, 0.0, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var zn = Vector3.Dot(plane_normal, cone_apex) + plane_dc;

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

            (var A, var B, var C, var D, var E, var F) = CalcEllipseImplicitForm(PV_mat, cone_base_r, base_z_offset);

            return (A, B, C, D, E, F, planexy_to_world, world_to_planexy);
        }

        return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, planexy_to_world, world_to_planexy);
    }

    private (double A, double B, double C, double D, double E, double F, MatrixD plane2world, MatrixD world2plane)
        CalcEllipseIntersectionForCylinder(Vector3 plane_normal, float shear_x, float shear_y, Vector3 offset, float base_r)
    {
        if (-Vector3.Dot(plane_normal, offset) > 0.0f)
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

        var eye = offset;
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
            { Math.Tan(shear_x), Math.Tan(shear_y), 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
         });

        var proj_to_plane = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, 0.0 },
            { up_vec.X, up_vec.Y, up_vec.Z, 0.0 },
            { 0.0, 0.0, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
        });
        var PV_mat = proj_to_plane * view_mat * oblique_proj_mat;

        (double A, double B, double C, double D, double E, double F) = CalcEllipseImplicitForm(PV_mat, base_r, 0.0);

        

        Vector3 origin = eye - Vector3.Dot(eye, plane_normal) * plane_normal;

        var planexy_to_world = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, up_vec.X, 0.0f, origin.X },
            { right_vec.Y, up_vec.Y, 0.0f, origin.Y },
            { right_vec.Z, up_vec.Z, 0.0f, origin.Z },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var world_to_planexy = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(right_vec, origin) },
            { up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(up_vec, origin) },
            { 0.0, 0.0, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        return (A, B, C, D, E, F, planexy_to_world, world_to_planexy);
    }

    private Vector3 GetConeApex()
    {
        var hdiff = 0.0f;
        var xdiff = 0.0f;
        var ydiff = 0.0f;
        var apex = new Vector3();

        var TopCenter = new Vector3(OffsetX, OffsetY, Height);

        if (RadiusBottom > RadiusTop)
        {
            hdiff = Height * RadiusTop / (RadiusBottom - RadiusTop);
            xdiff = OffsetX * RadiusTop / (RadiusBottom - RadiusTop);
            ydiff = OffsetY * RadiusTop / (RadiusBottom - RadiusTop);
            apex = TopCenter + (new Vector3(xdiff, ydiff, hdiff));
        }
        else
        {
            hdiff = Height * RadiusBottom / (RadiusTop - RadiusBottom);
            xdiff = OffsetX * RadiusBottom / (RadiusTop - RadiusBottom);
            ydiff = OffsetY * RadiusBottom / (RadiusTop - RadiusBottom);
            apex = new Vector3(-xdiff, -ydiff, -hdiff);
        }

        return apex;
    }

    public (double A, double B, double C, double D, double E, double F, MatrixD plane2world, MatrixD world2plane)
        GetTopEllipseImplicitForm()
    {
        // cones
        if (Math.Abs(RadiusBottom - RadiusTop) > (double)0.00001m)
        {
            var R = (RadiusBottom>0.0) ? RadiusBottom : RadiusTop;
            var base_offset_z = (RadiusBottom > 0.0) ? 0.0f : Height;

            var apex = GetConeApex();

            var TopCenter = new Vector3(OffsetX, OffsetY, Height);

            // plane that is defined by the top cap
            (var normal, var dc) = GetPlaneFromShearAndPoint(TopShearX, TopShearY, TopCenter);

            if (Math.Abs(RadiusTop) < (double)0.00001m)
            {
                (_, _, _, _, _, _, var plane2world, var world2plane) = CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z);
                return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, plane2world, world2plane);
            }
            return CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z);

        }
        //cylinders
        else
        {
            var slope = GetTopSlope().slope;

            // the most trivial case, cylinder with zero slope
            if (slope == 0)
            {
                var planexy_to_world = DenseMatrix.OfArray(new double[,] {
                    { 1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, Height },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                var world_to_planexy = DenseMatrix.OfArray(new double[,] {
                    { 1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                return (1.0/(RadiusTop* RadiusTop), 0.0, 1.0/(RadiusTop* RadiusTop), 0.0, 0.0, -1.0, planexy_to_world, world_to_planexy);
            }
            else
            {
                var Offset = new Vector3(OffsetX, OffsetY, Height);
                (var normal, _) = GetPlaneFromShearAndPoint(TopShearX, TopShearY, Offset);
                return CalcEllipseIntersectionForCylinder(normal, TopShearX, TopShearY, Offset, RadiusTop);
            }
        }
    }

    public (double semiMinorAxis, double semiMajorAxis, double theta, double x0, double y0, MatrixD plane2world, MatrixD world2plane)
        GetTopEllipsePolarForm()
    {
        // cones
        if (Math.Abs(RadiusBottom - RadiusTop) > (double)0.00001m)
        {
            var R = (RadiusBottom > 0.0) ? RadiusBottom : RadiusTop;
            var base_offset_z = (RadiusBottom > 0.0) ? 0.0f : Height;

            var apex = GetConeApex();

            var TopCenter = new Vector3(OffsetX, OffsetY, Height);

            // plane that is defined by the top cap
            (var normal, var dc) = GetPlaneFromShearAndPoint(TopShearX, TopShearY, TopCenter);

            (var A, var B, var C, var D, var E, var F, var plane2world, var world2plane) =
                CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z);

            if (Math.Abs(RadiusTop) < (double)0.00001m)
            {
                return (0.0, 0.0, 0.0, 0.0, 0.0, plane2world, world2plane);
            }

            (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
            return (b, a, theta, x0, y0, plane2world, world2plane);

        }
        //cylinders
        else
        {
            var slope = GetTopSlope().slope;

            // the most trivial case, cylinder with zero slope
            if(slope == 0 )
            {
                var planexy_to_world = DenseMatrix.OfArray(new double[,] {
                    { 1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, Height },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                var world_to_planexy = DenseMatrix.OfArray(new double[,] {
                    { 1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 1.0 }
                });

                return (RadiusTop, RadiusTop, 0.0f, 0.0f, 0.0f, planexy_to_world, world_to_planexy);
            }
            else
            {
                var Offset = new Vector3(OffsetX, OffsetY, Height);
                (var normal, _) = GetPlaneFromShearAndPoint(TopShearX, TopShearY, Offset);
                (var A, var B, var C, var D, var E, var F, var plane2world, var world2plane) =
                    CalcEllipseIntersectionForCylinder(normal, TopShearX, TopShearY, Offset, RadiusTop);
                (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
                return ((float)b, (float)a, (float)theta, (float)x0, (float)y0, plane2world, world2plane);
                
            }
        }
    }

    public (double A, double B, double C, double D, double E, double F, MatrixD plane2world, MatrixD world2plane)
        GetBottomEllipseImplicitForm()
    {
        // cones
        if (Math.Abs(RadiusBottom - RadiusTop) > (double)0.00001m)
        {
            var apex = GetConeApex();
            var R = (RadiusBottom > 0.0) ? RadiusBottom : RadiusTop;
            var base_offset_z = (RadiusBottom > 0.0) ? 0.0f : Height;

            // plane that is defined by the top cap
            var BottomCenter = new Vector3(0.0f, 0.0f, 0.0f);
            (var normal, var dc) = GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, BottomCenter);
            if (Math.Abs(RadiusBottom) < (double)0.00001m)
            {
                (_, _, _, _, _, _, var plane2world, var world2plane) = CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z);
                return (0.0, 0.0, 0.0, 0.0, 0.0, 0.0, plane2world, world2plane);
            }
            return CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z);
        }
        //cylinders
        else
        {
            var slope = GetTopSlope().slope;

            // the most trivial case, cylinder with zero slope
            if (slope == 0)
            {
                var planexy_to_world = DenseMatrix.OfArray(new double[,] {
                    { 1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 1.0 }
                });

                var world_to_planexy = planexy_to_world;

                return (1.0/(RadiusBottom* RadiusBottom), 0.0, 1.0/(RadiusBottom* RadiusBottom), 0.0, 0.0, -1.0, planexy_to_world, world_to_planexy);
            }
            else
            {
                var Offset = new Vector3(0.0f, 0.0f, 0.0f);
                (var normal, _) = GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, Offset);

                return CalcEllipseIntersectionForCylinder(normal, BottomShearX, BottomShearY, Offset, RadiusBottom);
                
            }
        }
    }

    public (double semiMinorAxis, double semiMajorAxis, double theta, double x0, double y0, MatrixD plane2world, MatrixD world2plane)
        GetBottomEllipsePolarForm()
    {
        // cones
        if (Math.Abs(RadiusBottom - RadiusTop) > (double)0.00001m)
        {
            var apex = GetConeApex();
            var R = (RadiusBottom > 0.0) ? RadiusBottom : RadiusTop;
            var base_offset_z = (RadiusBottom > 0.0) ? 0.0f : Height;

            // plane that is defined by the top cap
            var BottomCenter = new Vector3(0.0f, 0.0f, 0.0f);
            (var normal, var dc) = GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, BottomCenter);

            (var A, var B, var C, var D, var E, var F, MatrixD plane2world, MatrixD world2plane) =
                CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z);

            if (Math.Abs(RadiusBottom) < (double)0.00001m)
            {
                return (0.0, 0.0, 0.0, 0.0, 0.0, plane2world, world2plane);
            }

            (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
            return (b, a, theta, x0, y0, plane2world, world2plane);

        }
        //cylinders
        else
        {
            var slope = GetTopSlope().slope;

            // the most trivial case, cylinder with zero slope
            if (slope == 0)
            {
                var planexy_to_world = DenseMatrix.OfArray(new double[,] {
                    { 1.0, 0.0, 0.0, 0.0 },
                    { 0.0, 1.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 0.0 },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                var world_to_planexy = planexy_to_world;
                return (RadiusBottom, RadiusBottom, 0.0f, 0.0f, 0.0f, planexy_to_world, world_to_planexy);
            }
            else
            {
                var Offset = new Vector3(0.0f, 0.0f, 0.0f);
                (var normal, _) = GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, Offset);

                (var A, var B, var C, var D, var E, var F, MatrixD plane2world, MatrixD world2plane) =
                    CalcEllipseIntersectionForCylinder(normal, BottomShearX, BottomShearY, Offset, RadiusBottom);
                (var b, var a, var theta, var x0, var y0) = ConvertEllipseImplicitToPolarForm(A, B, C, D, E, F);
                return ((float)b, (float)a, (float)theta, (float)x0, (float)y0, plane2world, world2plane);

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