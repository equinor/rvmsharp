namespace RvmSharp.Primitives;

using System;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

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

    public (float semiMinorAxis, float semiMajorAxis) GetTopRadii()
    {
        if (RadiusBottom != RadiusTop)
        {
            var hdiff = 0.0f;
            var xdiff = 0.0f;
            var ydiff = 0.0f;
            var R = 0.0f;

            if (RadiusBottom > RadiusTop)
            {
                hdiff = Height * RadiusTop / (RadiusBottom - RadiusTop);
                xdiff = OffsetX * RadiusTop / (RadiusBottom - RadiusTop);
                R = RadiusBottom;
            }
            else
            {
                hdiff = Height * RadiusBottom / (RadiusTop - RadiusBottom);
                ydiff = OffsetY * RadiusBottom / (RadiusTop - RadiusBottom);
                R = RadiusTop;
            }

            var TopCenter = new Vector3(OffsetX, OffsetY, Height);
            var apex = new Vector3(OffsetX + xdiff, OffsetY + ydiff, Height + hdiff);

            // plane that is defined by the top cap
            var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -TopShearX);
            var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, TopShearY);
            var rotation = rotationAroundX * rotationAroundY;
            var normal = Vector3.Transform(Vector3.UnitZ, rotation);
            normal = Vector3.Normalize(normal);

            var dc = -Vector3.Dot(normal, TopCenter);
            if (dc > 0.0f)
            {
                normal = -normal;
                dc = -dc;
            }

            var view_vec = -normal;
            var up_to_proj = new Vector3(0.0f, 1.0f, 0.0f);
            if (normal.Y == 1.0f) {
                up_to_proj = new Vector3(1.0f, 0.0f, 0.0f);
            }
            var up_vec = up_to_proj - Vector3.Dot(up_to_proj, view_vec) * view_vec;
            up_vec = Vector3.Normalize(up_vec);

            var right_vec = Vector3.Normalize(Vector3.Cross(up_vec, view_vec));

            var zn = Vector3.Dot(normal, apex) + dc;

            var view_mat = new Matrix4x4(
                right_vec.X, right_vec.Y, right_vec.Z, -Vector3.Dot(apex, right_vec),
                up_vec.X, up_vec.Y, up_vec.Z, -Vector3.Dot(apex, up_vec),
                view_vec.X, view_vec.Y, view_vec.Z, -Vector3.Dot(apex, view_vec),
                0.0f, 0.0f, 0.0f, 1.0f);
            var proj_mat = new Matrix4x4(
                zn, 0.0f, 0.0f, 0.0f,
                0.0f, zn, 0.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, 0.0f
                );
            var PV_mat = Matrix4x4.Multiply(proj_mat, view_mat);

            var thetas = new float[5];
            thetas[0] = 0.0f;
            thetas[1] = MathF.PI / 3.0f;
            thetas[2] = 2.0f * MathF.PI / 3.0f;
            thetas[3] = 4.0f * MathF.PI / 3.0f;
            thetas[4] = MathF.PI;

            var circle_samples = new Vector4[5];
            var sam_prj = new Vector4[5];
            var index = 0;
            foreach (var theta in thetas)
            {
                circle_samples[index] = new Vector4(R * MathF.Cos(theta), R * MathF.Sin(theta), 0.0f, 1.0f);
                // for some strange reason the transform function multiplies the vector and the matrix w the vector on the left
                sam_prj[index] = Vector4.Transform(circle_samples[index], Matrix4x4.Transpose(PV_mat));
                sam_prj[index] = Vector4.Divide(sam_prj[index], sam_prj[index].W);
                index++;
            }

            Matrix<double> coeff_mat = DenseMatrix.OfArray(new double[,] {
                { sam_prj[0].X * sam_prj[0].X, sam_prj[0].X * sam_prj[0].Y, sam_prj[0].Y * sam_prj[0].Y, sam_prj[0].X, sam_prj[0].Y },
                { sam_prj[1].X * sam_prj[1].X, sam_prj[1].X * sam_prj[1].Y, sam_prj[1].Y * sam_prj[1].Y, sam_prj[1].X, sam_prj[1].Y },
                { sam_prj[2].X * sam_prj[2].X, sam_prj[2].X * sam_prj[2].Y, sam_prj[2].Y * sam_prj[2].Y, sam_prj[2].X, sam_prj[2].Y },
                { sam_prj[3].X * sam_prj[3].X, sam_prj[3].X * sam_prj[3].Y, sam_prj[3].Y * sam_prj[3].Y, sam_prj[3].X, sam_prj[3].Y },
                { sam_prj[4].X * sam_prj[4].X, sam_prj[4].X * sam_prj[4].Y, sam_prj[4].Y * sam_prj[4].Y, sam_prj[4].X, sam_prj[4].Y }
            });

            var F = -1.0;
            var f_vec = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(new double[] { -F, -F, -F, -F, -F });
            
            var res_vec = coeff_mat.Solve(f_vec); // res_vec = [A, B, C, D, E] from Ax2 + Bxy + Cy2 + Dx + Ey = F
            if (res_vec[0] < 0.0) {
                res_vec.Multiply(-1.0);
                F *= -1.0;
            }
            var A = res_vec[0];
            var B = res_vec[1];
            var C = res_vec[2];
            var D = res_vec[3];
            var E = res_vec[4];

            // aka semi major axis a
            var semiMajorRadius = -Math.Sqrt(2.0 * (A * E * E + C * D * D - B * D * E + (B * B - 4.0 * A * C) * F) * ((A + C) + Math.Sqrt((A - C) * (A - C) + B * B))) / (B * B - 4.0 * A * C);
            var semiMinorRadius = -Math.Sqrt(2.0 * (A * E * E + C * D * D - B * D * E + (B * B - 4.0 * A * C) * F) * ((A + C) - Math.Sqrt((A - C) * (A - C) + B * B))) / (B * B - 4.0 * A * C);

            return ((float)semiMinorRadius, (float)semiMajorRadius);
        }
        else
        {
            var slope = GetTopSlope().slope;
            var semiMajorRadius = slope != 0 ? RadiusTop / MathF.Cos(slope) : RadiusTop;

            return (RadiusTop, semiMajorRadius);
        }

        
    }

    public (float semiMinorAxis, float semiMajorAxis) GetBottomRadii()
    {
        var slope = GetBottomSlope().slope;
        var semiMajorRadius = slope != 0 ? RadiusBottom / MathF.Cos(slope) : RadiusBottom;

        return (RadiusBottom, semiMajorRadius);
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