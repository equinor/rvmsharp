namespace CadRevealRvmProvider.Converters;

using RvmSharp.Operations;
using RvmSharp.Primitives;
using System.Numerics;

public static class RvmSnoutExtensions
{
    public static bool HasShear(this RvmSnout rvmSnout)
    {
        return rvmSnout.BottomShearX != 0 || rvmSnout.BottomShearY != 0 || rvmSnout.TopShearX != 0 || rvmSnout.TopShearY != 0;
    }

    public static bool IsEccentric(this RvmSnout rvmSnout)
    {
        return rvmSnout.OffsetX != 0 || rvmSnout.OffsetY != 0;
    }

    public static (Quaternion rotation, Vector3 normal, float slope) GetTopSlope(this RvmSnout rvmSnout)
    {
        return TranslateShearToSlope(rvmSnout.TopShearX, rvmSnout.TopShearY);
    }

    public static (Quaternion rotation, Vector3 normal, float slope) GetBottomSlope(this RvmSnout rvmSnout)
    {
        return TranslateShearToSlope(rvmSnout.BottomShearX, rvmSnout.BottomShearY);
    }

    public static bool IsCappedCylinder(this RvmSnout rvmSnout)
    {
        return Math.Abs(rvmSnout.RadiusBottom - rvmSnout.RadiusTop) < 0.01;
    }

    public static Ellipse3D GetTopCapEllipse(this RvmSnout rvmSnout)
    {
        // plane that is defined by the top cap
        var topCapCenter = 0.5f * new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height);
        var xPlane = GeometryHelper.GetPlaneFromShearAndPoint(rvmSnout.TopShearX, rvmSnout.TopShearY, topCapCenter);

        return rvmSnout.GetCapEllipse(xPlane, topCapCenter, rvmSnout.RadiusTop);
    }

    public static Ellipse3D GetBottomCapEllipse(this RvmSnout rvmSnout)
    {
        // plane that is defined by the bottom cap
        var bottomCapCenter = -0.5f * new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height);
        var xPlane = GeometryHelper.GetPlaneFromShearAndPoint(rvmSnout.BottomShearX, rvmSnout.BottomShearY, bottomCapCenter);

        return rvmSnout.GetCapEllipse(xPlane, bottomCapCenter, rvmSnout.RadiusBottom);
    }

    private static Ellipse3D GetCapEllipse(this RvmSnout rvmSnout, PlaneImplicitForm xPlane, Vector3 capCenter, float capRadius)
    {
        // cones
        if (!rvmSnout.IsCappedCylinder())
        {
            var offset = new Vector3(rvmSnout.OffsetX, rvmSnout.OffsetY, rvmSnout.Height);
            var cone = ConicSectionsHelper.CreateConeFromSnout(rvmSnout.RadiusBottom, rvmSnout.RadiusTop, offset);

            if (Math.Abs(capRadius) < 0.01)
            {
                return ConicSectionsHelper.CreateDegenerateEllipse(xPlane, cone);
            }
            return ConicSectionsHelper.CalcEllipseIntersectionForCone(xPlane, cone);
        }
        //cylinders
        else
        {
            var cosineSlope = Vector3.Dot(xPlane.normal, new Vector3(0.0f, 0.0f, 1.0f));

            // the most trivial case, cylinder with zero slope
            if (cosineSlope == 1)
            {
                return ConicSectionsHelper.CalcEllipseIntersectionForCylinderWithZeroCapSlope(rvmSnout.RadiusBottom, capCenter);
            }
            else
            {
                return ConicSectionsHelper.CalcEllipseIntersectionForCylinder(xPlane, rvmSnout.RadiusBottom, capCenter);
            }
        }
    }

    private static (Quaternion rotation, Vector3 normal, float slope) TranslateShearToSlope(float shearX, float shearY)
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
}
