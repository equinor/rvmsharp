namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;

public static class EllipticalDishSnoutComparer
{
    public static bool ShowCap(CapData<RvmEllipticalDish> ellipticalDishCapData, CapData<RvmSnout> snoutCapData)
    {
        var rvmEllipticalDish = ellipticalDishCapData.Primitive;
        var rvmSnout = snoutCapData.Primitive;

        rvmEllipticalDish.Matrix.DecomposeAndNormalize(out var ellipticalDishScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var ellipticalDishRadius = rvmEllipticalDish.BaseRadius * ellipticalDishScale.X;

        var isSnoutCapTop = snoutCapData.CapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        return ComparerHelper.CheckOverlap(
            ellipticalDishCapData.IsCurrentPrimitive,
            ellipticalDishRadius,
            (float)semiMinorRadius,
            (float)semiMajorRadius,
            CapVisibility.CapOverlapTolerance
        );
    }
}
