namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class CylinderCylinderComparer
{
    public static bool ShowCap(CapData<RvmCylinder> cylinderData1, CapData<RvmCylinder> cylinderData2)
    {
        var rvmCylinder1 = cylinderData1.Primitive;
        var rvmCylinder2 = cylinderData2.Primitive;

        rvmCylinder1.Matrix.DecomposeAndNormalize(out var cylinderScale1, out _, out _);
        rvmCylinder2.Matrix.DecomposeAndNormalize(out var cylinderScale2, out _, out _);

        var cylinderRadius1 = rvmCylinder1.Radius * cylinderScale1.X;
        var cylinderRadius2 = rvmCylinder2.Radius * cylinderScale2.X;

        return ComparerHelper.IsVisible(
            cylinderData1.IsCurrentPrimitive,
            cylinderRadius1,
            cylinderRadius2,
            CapVisibility.CapOverlapTolerance
        );
    }
}
