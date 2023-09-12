namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public class CylinderSphericalDishComparer : ICapComparer
{
    public bool ShowCap(CapData cylinderCapData, CapData sphericalDishCapData)
    {
        var rvmCylinder = (RvmCylinder)cylinderCapData.Primitive;
        var rvmSphericalDish = (RvmSphericalDish)sphericalDishCapData.Primitive;

        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;
        var rvmSphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (cylinderCapData.IsCurrentPrimitive)
        {
            if (rvmSphericalDishRadius + CapVisibility.CapComparingBuffer >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + CapVisibility.CapComparingBuffer >= rvmSphericalDishRadius)
            {
                return false;
            }
        }

        return true;
    }
}
