namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class CylinderSphericalDishComparer
{
    public static bool ShowCap(CapData<RvmCylinder> cylinderCapData, CapData<RvmSphericalDish> sphericalDishCapData)
    {
        var rvmCylinder = cylinderCapData.Primitive;
        var rvmSphericalDish = sphericalDishCapData.Primitive;

        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var rvmSphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (cylinderCapData.IsCurrentPrimitive)
        {
            if (rvmSphericalDishRadius + CapVisibility.CapOverlapTolerance >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + CapVisibility.CapOverlapTolerance >= rvmSphericalDishRadius)
            {
                return false;
            }
        }

        return true;
    }
}
