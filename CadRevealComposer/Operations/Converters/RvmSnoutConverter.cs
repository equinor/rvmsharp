namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Drawing;
using System.Numerics;
using Utils;

public static class RvmSnoutConverter
{
    public static APrimitive ConvertToRevealPrimitive(
        this RvmSnout rvmSnout,
        ulong treeIndex,
        Color color)
    {
    }
}

public static class RvmSnoutConverter
{
    public static APrimitive ConvertToRevealPrimitive(
        this RvmSnout rvmSnout,
        ulong treeIndex,
        Color color)
    {
        if (HasShear(rvmSnout))
        {
            if (IsEccentric(rvmSnout))
            {
                throw new NotImplementedException(
                    "This type of primitive is missing from CadReveal, should convert to mesh?");
            }
            else
            {
                (float slopeA, float zangleA) = TranslateShearToSlope((rvmSnout.TopShearX, rvmSnout.TopShearY));
                (float slopeB, float zangleB) = TranslateShearToSlope((rvmSnout.BottomShearX, rvmSnout.BottomShearY));
                if (rvmSnout.RadiusTop.ApproximatelyEquals(rvmSnout.RadiusBottom))
                {
                    // General cylinder
                    return new GeneralCylinder(rvmSnout.
                        rvmSnout,
                        2 * MathF.PI,
                        revealNode.TreeIndex,
                        container.GetColor(),
                        revealNode.BoundingBoxAxisAligned);
                } else {
                    // General cone
                    return new Cone();
                }
            }
        }
        else
        {
            if (IsEccentric(rvmSnout))
            {
                return new EccentricCone();
            }
            else
            {
                return new Cone();
            }
        }
    }

    private static bool IsEccentric(RvmSnout rvmSnout)
    {
        return rvmSnout.OffsetX != 0 || rvmSnout.OffsetY != 0;
    }

    private static bool HasShear(RvmSnout rvmSnout)
    {
        return rvmSnout.BottomShearX != 0 || rvmSnout.BottomShearY != 0 || rvmSnout.TopShearX != 0 ||
               rvmSnout.TopShearY != 0;
    }

    private static (float slope, float zangle) TranslateShearToSlope((float shearX, float shearY) input)
    {
        var rotationAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -input.shearX);
        var rotationAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, input.shearY);
        var rotationTotal = rotationAroundX * rotationAroundY;
        var capNormal = Vector3.Transform(Vector3.UnitZ, rotationTotal);
        var angleAroundZ = MathF.Atan2(capNormal.Y, capNormal.X);
        var slope = MathF.PI / 2 - MathF.Atan2(capNormal.Z, MathF.Sqrt(capNormal.X * capNormal.X + capNormal.Y * capNormal.Y));

        return (slope, angleAroundZ);
    }
}