namespace RvmSharp.Primitives;

using System;
using System.Numerics;
using Operations;

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



    private (EllipseImplicitForm ellipseImpl, MatrixD xplane_to_model, MatrixD model_to_xplane)
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
            EllipseImplicitForm ellImpl = ConicSectionsHelper.CalcEllipseImplicitForm(PV_mat, cone_base_r, cone_base_z_offset);

            return (ellImpl, planexy_to_world, world_to_planexy);
        }

        return (ConicSectionsHelper.zeroEllipseImplicit, planexy_to_world, world_to_planexy);
    }

    private (EllipseImplicitForm, MatrixD, MatrixD)
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

        var ellipseImplicitForm = ConicSectionsHelper.CalcEllipseImplicitForm(PV_mat, base_r, eye.Z);

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

        return (ellipseImplicitForm, planexy_to_world, world_to_planexy);
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


    public (EllipsePolarForm polarEq, MatrixD xplane2ModelCoords, MatrixD modelCoord2xplane) GetTopCapEllipse()
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

            (EllipseImplicitForm ellipseImplicit, var xplane_to_model, var model_to_xplane) =
                CalcEllipseIntersectionForCone(normal, dc, apex, R, base_offset_z, OriginOffset);

            if (Math.Abs(RadiusTop) < (double)0.00001m)
            {
                return (ConicSectionsHelper.zeroEllipsePolar, xplane_to_model, model_to_xplane);
            }

            var ellipsePolar = ConicSectionsHelper.ConvertEllipseImplicitToPolarForm(ellipseImplicit);
            return (ellipsePolar, xplane_to_model, model_to_xplane);

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

                return (new EllipsePolarForm(RadiusTop, RadiusTop, 0.0f, 0.0f, 0.0f), planexy_to_model, model_to_planexy);
            }
            else
            {
                var Offset = new Vector3(OffsetX, OffsetY, Height);
                (var normal, _) = GetPlaneFromShearAndPoint(TopShearX, TopShearY, Offset);
                (var ellipseImplicit, var xplane_to_model, var model_to_xplane) =
                    CalcEllipseIntersectionForCylinder(normal, Offset, RadiusTop, OriginOffset);
                var ellipsePolar = ConicSectionsHelper.ConvertEllipseImplicitToPolarForm(ellipseImplicit);
                return (ellipsePolar, xplane_to_model, model_to_xplane);
                
            }
        }
    }

    public (EllipsePolarForm polarEq, MatrixD xplane2ModelCoords, MatrixD modelCoord2xplane) GetBottomCapEllipse()
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

            (var ellipseImpl, MatrixD xplane_to_model, MatrixD model_to_xplane) =
                CalcEllipseIntersectionForCone(normal, dc, apex, R, 0.0f, OriginOffset);

            if (Math.Abs(RadiusBottom) < (double)0.00001m)
            {
                return (ConicSectionsHelper.zeroEllipsePolar, xplane_to_model, model_to_xplane);
            }

            var ellipsePolar = ConicSectionsHelper.ConvertEllipseImplicitToPolarForm(ellipseImpl);
            return (ellipsePolar, xplane_to_model, model_to_xplane);

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
                return (new EllipsePolarForm(RadiusBottom, RadiusBottom, 0.0, 0.0, 0.0), planexy_to_model, model_to_planexy);
            }
            else
            {
                var Offset = new Vector3(0.0f, 0.0f, 0.0f);
                (var normal, _) = GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, Offset);

                (var ellipseImpl, MatrixD xplane_to_model, MatrixD model_to_xplane) =
                    CalcEllipseIntersectionForCylinder(normal, Offset, RadiusBottom, OriginOffset);
                var ellipsePolar = ConicSectionsHelper.ConvertEllipseImplicitToPolarForm(ellipseImpl);
                return (ellipsePolar, xplane_to_model, model_to_xplane);

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