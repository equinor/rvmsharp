namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public class CylinderEllipticalDishComparer : ICapComparer
{
    public bool ShowCap(CapData cylinderCapData, CapData ellipticalDishCapData)
    {
        var rvmCylinder = (RvmCylinder)cylinderCapData.Primitive;
        var rvmEllipticalDish = (RvmEllipticalDish)ellipticalDishCapData.Primitive;

        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        if (cylinderCapData.IsCurrentPrimitive)
        {
            if (ellipticalDishRadius + CapVisibility.CapComparingBuffer >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + CapVisibility.CapComparingBuffer >= ellipticalDishRadius)
            {
                return false;
            }
        }

        return true;
    }
}
