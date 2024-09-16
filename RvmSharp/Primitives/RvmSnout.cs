namespace RvmSharp.Primitives;

using System;
using System.Numerics;
using Commons.Utils;
using Operations;

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
    float TopShearY
) : RvmPrimitive(Version, RvmPrimitiveKind.Snout, Matrix, BoundingBoxLocal)
{
    public bool HasShear()
    {
        return BottomShearX != 0 || BottomShearY != 0 || TopShearX != 0 || TopShearY != 0;
    }

    public bool IsEccentric()
    {
        return OffsetX != 0 || OffsetY != 0;
    }

    public (Quaternion rotation, Vector3 normal, float slope) GetTopSlope()
    {
        return TranslateShearToSlope(TopShearX, TopShearY);
    }

    public (Quaternion rotation, Vector3 normal, float slope) GetBottomSlope()
    {
        return TranslateShearToSlope(BottomShearX, BottomShearY);
    }

    private bool IsCappedCylinder()
    {
        return Math.Abs(RadiusBottom - RadiusTop) < 0.01;
    }

    public Ellipse3D GetTopCapEllipse()
    {
        // plane that is defined by the top cap
        var topCapCenter = 0.5f * new Vector3(OffsetX, OffsetY, Height);
        var xPlane = GeometryHelper.GetPlaneFromShearAndPoint(TopShearX, TopShearY, topCapCenter);

        return GetCapEllipse(xPlane, topCapCenter, RadiusTop);
    }

    public Ellipse3D GetBottomCapEllipse()
    {
        // plane that is defined by the bottom cap
        var bottomCapCenter = -0.5f * new Vector3(OffsetX, OffsetY, Height);
        var xPlane = GeometryHelper.GetPlaneFromShearAndPoint(BottomShearX, BottomShearY, bottomCapCenter);

        return GetCapEllipse(xPlane, bottomCapCenter, RadiusBottom);
    }

    private Ellipse3D GetCapEllipse(PlaneImplicitForm xPlane, Vector3 capCenter, float capRadius)
    {
        // cones
        if (!IsCappedCylinder())
        {
            var offset = new Vector3(OffsetX, OffsetY, Height);
            var cone = ConicSectionsHelper.CreateConeFromSnout(RadiusBottom, RadiusTop, offset);

            return Math.Abs(capRadius) < 0.01
                ? ConicSectionsHelper.CreateDegenerateEllipse(xPlane, cone)
                : ConicSectionsHelper.CalcEllipseIntersectionForCone(xPlane, cone);
        }
        //cylinders
        var cosineSlope = Vector3.Dot(xPlane.Normal, new Vector3(0.0f, 0.0f, 1.0f));

        // the most trivial case, cylinder with zero slope
        return cosineSlope.ApproximatelyEquals(1)
            ? ConicSectionsHelper.CalcEllipseIntersectionForCylinderWithZeroCapSlope(RadiusBottom, capCenter)
            : ConicSectionsHelper.CalcEllipseIntersectionForCylinder(xPlane, RadiusBottom, capCenter);
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
