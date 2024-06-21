namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using System.Diagnostics;
using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class CylinderSnoutComparer
{
    public static bool ShowCap(CapData<RvmCylinder> cylinderCapData, CapData<RvmSnout> snoutCapData)
    {
        var rvmCylinder = cylinderCapData.Primitive;
        var rvmSnout = snoutCapData.Primitive;

        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        var isSnoutCapTop = snoutCapData.CapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().Ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().Ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.SemiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.SemiMajorAxis * snoutScale.X;

        return ComparerHelper.IsVisible(
            cylinderCapData.IsCurrentPrimitive,
            cylinderRadius,
            (float)semiMinorRadius,
            (float)semiMajorRadius,
            CapVisibility.CapOverlapTolerance
        );
    }
}
