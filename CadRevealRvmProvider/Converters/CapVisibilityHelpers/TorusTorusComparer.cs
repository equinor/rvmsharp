namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public class TorusTorusComparer : ICapComparer
{
    public bool ShowCap(CapData torusCapData1, CapData torusCapData2)
    {
        var rvmCircularTorus1 = (RvmCircularTorus)torusCapData1.Primitive;
        var rvmCircularTorus2 = (RvmCircularTorus)torusCapData2.Primitive;

        rvmCircularTorus1.Matrix.DecomposeAndNormalize(out var torusScale1, out _, out _);
        rvmCircularTorus2.Matrix.DecomposeAndNormalize(out var torusScale2, out _, out _);

        var torusRadius1 = rvmCircularTorus1.Radius * torusScale1.X;
        var torusRadius2 = rvmCircularTorus2.Radius * torusScale2.X;

        if (torusCapData1.IsCurrentPrimitive)
        {
            if (torusRadius2 + CapVisibility.CapComparingBuffer >= torusRadius1)
            {
                return false;
            }
        }
        else
        {
            if (torusRadius1 + CapVisibility.CapComparingBuffer >= torusRadius2)
            {
                return false;
            }
        }

        return true;
    }
}
