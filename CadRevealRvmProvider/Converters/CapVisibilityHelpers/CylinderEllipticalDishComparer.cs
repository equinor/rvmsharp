namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class CylinderEllipticalDishComparer
{
    public static bool ShowCap(CapData<RvmCylinder> cylinderCapData, CapData<RvmEllipticalDish> ellipticalDishCapData)
    {
        var rvmCylinder = cylinderCapData.Primitive;
        var rvmEllipticalDish = ellipticalDishCapData.Primitive;

        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        if (cylinderCapData.IsCurrentPrimitive)
        {
            if (ellipticalDishRadius + CapVisibility.CapOverlapTolerance >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + CapVisibility.CapOverlapTolerance >= ellipticalDishRadius)
            {
                return false;
            }
        }

        return true;
    }
}
