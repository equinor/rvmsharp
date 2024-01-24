namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class TorusTorusComparer
{
    public static bool ShowCap(CapData<RvmCircularTorus> torusCapData1, CapData<RvmCircularTorus> torusCapData2)
    {
        var rvmCircularTorus1 = torusCapData1.Primitive;
        var rvmCircularTorus2 = torusCapData2.Primitive;

        rvmCircularTorus1.Matrix.DecomposeAndNormalize(out var torusScale1, out _, out _);
        rvmCircularTorus2.Matrix.DecomposeAndNormalize(out var torusScale2, out _, out _);

        var torusRadius1 = rvmCircularTorus1.Radius * torusScale1.X;
        var torusRadius2 = rvmCircularTorus2.Radius * torusScale2.X;

        return ComparerHelper.IsVisible(
            torusCapData1.IsCurrentPrimitive,
            torusRadius1,
            torusRadius2,
            CapVisibility.CapOverlapTolerance
        );
    }
}
