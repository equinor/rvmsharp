namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public class TorusCylinderComparer : ICapComparer
{
    public bool ShowCap(CapData torusCapData, CapData cylinderCapData)
    {
        var rvmCircularTorus = (RvmCircularTorus)torusCapData.Primitive;
        var rvmCylinder = (RvmCylinder)cylinderCapData.Primitive;

        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var circularTorusRadius = rvmCircularTorus.Radius * circularTorusScale.X;
        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        if (torusCapData.IsCurrentPrimitive)
        {
            if (cylinderRadius + CapVisibility.CapComparingBuffer >= circularTorusRadius)
            {
                return false;
            }
        }
        else
        {
            if (circularTorusRadius + CapVisibility.CapComparingBuffer >= cylinderRadius)
            {
                return false;
            }
        }

        return true;
    }
}
