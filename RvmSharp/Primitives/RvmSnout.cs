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

    public bool IsCappedCylinder()
    {
        return Math.Abs(RadiusBottom - RadiusTop) < (double)0.00001m;
    }

    private (EllipseImplicitForm ellipseImpl, MatrixD xplane_to_model, MatrixD model_to_xplane)
        CalcEllipseIntersectionForCone(PlaneImplicitForm capPlane, Vector3 cone_apex, float cone_base_r, Vector3 origoCalcCoord, Vector3 origoModelCoord)
    {
   
        PlaneImplicitForm xPlane = (capPlane.d > 0.0f)
            ? new PlaneImplicitForm(-capPlane.normal, -capPlane.d)
            : new PlaneImplicitForm(capPlane.normal, capPlane.d);

        var view_vec = - xPlane.normal;
        var up_to_proj = new Vector3(0.0f, 1.0f, 0.0f);
        if (xPlane.normal.Y == 1.0f)
        {
            up_to_proj = new Vector3(1.0f, 0.0f, 0.0f);
        }
        var up_vec = up_to_proj - Vector3.Dot(up_to_proj, view_vec) * view_vec;
        up_vec = Vector3.Normalize(up_vec);

        var right_vec = Vector3.Normalize(Vector3.Cross(up_vec, view_vec));

        // distance of the apex to the intersection plane (cap)
        var zn = Vector3.Dot(xPlane.normal, cone_apex) + xPlane.d;

        // project apex to the cap plane and add offset
        Vector3 originModelCoord = cone_apex - zn * xPlane.normal - origoCalcCoord + origoModelCoord;

        var planexy_to_world = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, up_vec.X, view_vec.X, originModelCoord.X},
            { right_vec.Y, up_vec.Y, view_vec.Y, originModelCoord.Y},
            { right_vec.Z, up_vec.Z, view_vec.Z, originModelCoord.Z},
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var world_to_planexy = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(right_vec, originModelCoord) },
            { up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(up_vec, originModelCoord) },
            { view_vec.X, view_vec.Y, view_vec.Z, -Vector3.Dot(view_vec, originModelCoord) },
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
            EllipseImplicitForm ellImpl = ConicSectionsHelper.CalcEllipseImplicitForm(PV_mat, cone_base_r);

            return (ellImpl, planexy_to_world, world_to_planexy);
        }

        return (ConicSectionsHelper.zeroEllipseImplicit, planexy_to_world, world_to_planexy);
    }

    private (EllipseImplicitForm, MatrixD, MatrixD)
        CalcEllipseIntersectionForCylinder(PlaneImplicitForm capPlane, float base_r, Vector3 capCenterCalcCoord, Vector3 capCenterModelCoord)
    {
        PlaneImplicitForm xPlane = (capPlane.d > 0.0f)
            ? new PlaneImplicitForm(-capPlane.normal, -capPlane.d)
            : new PlaneImplicitForm(capPlane.normal, capPlane.d);

        var eye = capCenterCalcCoord; // eye is placed in the origin

        var view_vec = -xPlane.normal;
        var up_to_proj = new Vector3(0.0f, 1.0f, 0.0f);
        if (xPlane.normal.Y == 1.0f)
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

        var rot_to_plane = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, 0.0 },
            { up_vec.X, up_vec.Y, up_vec.Z, 0.0 },
            { view_vec.X, view_vec.Y, view_vec.Z, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        // TODO: this should take account of oblique cylinders as well
        var oblique_proj_mat = DenseMatrix.OfArray(new double[,] {
            { 1.0, 0.0, 0.0, 0.0 },
            { 0.0, 1.0, 0.0, 0.0 },
            { -xPlane.normal.X / xPlane.normal.Z, -xPlane.normal.Y / xPlane.normal.Z, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
         });

        var proj = DenseMatrix.OfArray(new double[,] {
            { 1.0, 0.0, 0.0, 0.0 },
            { 0.0, 1.0, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 0.0 },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var PV_mat = proj * rot_to_plane * oblique_proj_mat * view_mat;

        var ellipseImplicitForm = ConicSectionsHelper.CalcEllipseImplicitForm(PV_mat, base_r);

        //Vector3 origin = eye - Vector3.Dot(eye, plane_normal) * plane_normal;
        
        Vector3 originModelCoord = eye - capCenterCalcCoord + capCenterModelCoord;
        var planexy_to_world = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, up_vec.X, view_vec.X, originModelCoord.X },
            { right_vec.Y, up_vec.Y, view_vec.Y, originModelCoord.Y },
            { right_vec.Z, up_vec.Z, view_vec.Z, originModelCoord.Z },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        var world_to_planexy = DenseMatrix.OfArray(new double[,] {
            { right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(right_vec, originModelCoord) },
            { up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(up_vec, originModelCoord) },
            { view_vec.X, view_vec.Y, view_vec.Z, -Vector3.Dot(view_vec, originModelCoord) },
            { 0.0, 0.0, 0.0, 1.0 }
        });

        return (ellipseImplicitForm, planexy_to_world, world_to_planexy);
    }

    


    public (EllipsePolarForm polarEq, MatrixD xplane2ModelCoords, MatrixD modelCoord2xplane) GetTopCapEllipse()
    {
        var capCenterModelCoords = 0.5f * new Vector3(OffsetX, OffsetY, Height);
        var capCenterCalcCoords = 0.5f * new Vector3(OffsetX, OffsetY, Height);

        // plane that is defined by the top cap
        var topCenter = capCenterCalcCoords;
        var xPlane = GeometryHelper.GetPlaneFromShearAndPoint(TopShearX, TopShearY, topCenter);

        // cones
        if (!IsCappedCylinder())
        {
            var offset = new Vector3(OffsetX, OffsetY, Height);
            var cone = ConicSectionsHelper.getConeFromSnout(RadiusBottom, RadiusTop, offset);

            (EllipseImplicitForm ellipseImplicit, var xplane_to_model, var model_to_xplane) =
                CalcEllipseIntersectionForCone(xPlane, cone.apex, cone.baseR, capCenterCalcCoords, capCenterModelCoords);

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

            // the most trivial case, cylinder with zero slope of the cap
            if (slope == 0)
            {
                var planexy_to_model = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, capCenterModelCoords.X },
                    { 0.0, 1.0, 0.0, capCenterModelCoords.Y },
                    { 0.0, 0.0, -1.0, capCenterModelCoords.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                var model_to_planexy = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, capCenterModelCoords.X },
                    { 0.0, 1.0, 0.0, -capCenterModelCoords.Y },
                    { 0.0, 0.0, -1.0, capCenterModelCoords.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });

                var rsq = RadiusTop * RadiusTop;
                var elImplicit = new EllipseImplicitForm(1.0 / rsq, 0.0, 1.0 / rsq, 0.0, 0.0, -1.0);
                var elPolar = new EllipsePolarForm(RadiusTop, RadiusTop, 0.0, 0.0, 0.0, elImplicit);
                return (elPolar, planexy_to_model, model_to_planexy);
            }
            else
            {
                (var ellipseImplicit, var xplane_to_model, var model_to_xplane) =
                    CalcEllipseIntersectionForCylinder(xPlane, RadiusTop, capCenterCalcCoords, capCenterModelCoords);
                var ellipsePolar = ConicSectionsHelper.ConvertEllipseImplicitToPolarForm(ellipseImplicit);
                return (ellipsePolar, xplane_to_model, model_to_xplane);
                
            }
        }
    }

    public (EllipsePolarForm polarEq, MatrixD xplane2ModelCoords, MatrixD modelCoord2xplane) GetBottomCapEllipse()
    {
        var capCenterModelCoords = -0.5f * new Vector3(OffsetX, OffsetY, Height);
        var capCenterCalcCoords = -0.5f * new Vector3(OffsetX, OffsetY, Height);

        // plane that is defined by the top cap
        var BottomCenter = capCenterCalcCoords;
        var xPlane = GeometryHelper.GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, BottomCenter);

        // cones
        if (Math.Abs(RadiusBottom - RadiusTop) > (double)0.00001m)
        {
            var offset = new Vector3(OffsetX, OffsetY, Height);
            var cone = ConicSectionsHelper.getConeFromSnout(RadiusBottom, RadiusTop, offset);

            (var ellipseImpl, MatrixD xplane_to_model, MatrixD model_to_xplane) =
                CalcEllipseIntersectionForCone(xPlane, cone.apex, cone.baseR, capCenterCalcCoords, capCenterModelCoords);

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
            var slope = GetBottomSlope().slope;

            // the most trivial case, cylinder with zero slope
            if (slope == 0)
            {
                var planexy_to_model = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, capCenterModelCoords.X },
                    { 0.0, 1.0, 0.0, capCenterModelCoords.Y },
                    { 0.0, 0.0, -1.0, capCenterModelCoords.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                var model_to_planexy = DenseMatrix.OfArray(new double[,] {
                    { -1.0, 0.0, 0.0, capCenterModelCoords.X },
                    { 0.0, 1.0, 0.0, -capCenterModelCoords.Y },
                    { 0.0, 0.0, -1.0, capCenterModelCoords.Z },
                    { 0.0, 0.0, 0.0, 1.0 }
                });
                //var world_to_planexy = planexy_to_world;
                var rsq = RadiusBottom * RadiusBottom;
                var elImplicit = new EllipseImplicitForm(1.0 / rsq, 0.0, 1.0 / rsq, 0.0, 0.0, -1.0);
                var elPolar = new EllipsePolarForm(RadiusBottom, RadiusBottom, 0.0, 0.0, 0.0, elImplicit);
                return (elPolar, planexy_to_model, model_to_planexy);
            }
            else
            {
                (var ellipseImpl, MatrixD xplane_to_model, MatrixD model_to_xplane) =
                    CalcEllipseIntersectionForCylinder(xPlane, RadiusBottom, capCenterCalcCoords, capCenterModelCoords);
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