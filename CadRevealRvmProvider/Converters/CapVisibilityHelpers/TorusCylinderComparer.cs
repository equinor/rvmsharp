namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public class TorusCylinderComparer : ICapComparer
{
    public static bool ShowCap(CapData<RvmCircularTorus> torusCapData, CapData<RvmCylinder> cylinderCapData)
    {
        var rvmCircularTorus = torusCapData.Primitive;
        var rvmCylinder = cylinderCapData.Primitive;

        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var circularTorusRadius = rvmCircularTorus.Radius * circularTorusScale.X;
        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        if (torusCapData.IsCurrentPrimitive)
        {
            if (cylinderRadius + CapVisibility.CapOverlapTolerance >= circularTorusRadius)
            {
                return false;
            }
        }
        else
        {
            if (circularTorusRadius + CapVisibility.CapOverlapTolerance >= cylinderRadius)
            {
                return false;
            }
        }

        return true;
    }
}
