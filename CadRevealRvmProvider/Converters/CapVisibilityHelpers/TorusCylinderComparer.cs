namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class TorusCylinderComparer
{
    public static bool ShowCap(CapData<RvmCircularTorus> torusCapData, CapData<RvmCylinder> cylinderCapData)
    {
        var rvmCircularTorus = torusCapData.Primitive;
        var rvmCylinder = cylinderCapData.Primitive;

        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var circularTorusRadius = rvmCircularTorus.Radius * circularTorusScale.X;
        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        return ComparerHelper.IsVisible(
            torusCapData.IsCurrentPrimitive,
            circularTorusRadius,
            cylinderRadius,
            CapVisibility.CapOverlapTolerance
        );
    }
}
