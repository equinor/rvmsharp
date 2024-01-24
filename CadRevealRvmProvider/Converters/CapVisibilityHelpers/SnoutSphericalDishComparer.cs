namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;

public static class SnoutSphericalDishComparer
{
    public static bool ShowCap(CapData<RvmSnout> snoutCapData, CapData<RvmSphericalDish> sphericalDishCapData)
    {
        var rvmSnout = snoutCapData.Primitive;
        var rvmSphericalDish = sphericalDishCapData.Primitive;

        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var isSnoutCapTop = snoutCapData.CapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().Ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().Ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.SemiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.SemiMajorAxis * snoutScale.X;

        var sphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (snoutCapData.IsCurrentPrimitive)
        {
            if (sphericalDishRadius + CapVisibility.CapOverlapTolerance >= semiMajorRadius)
            {
                return false;
            }
        }
        else
        {
            if (semiMinorRadius + CapVisibility.CapOverlapTolerance >= sphericalDishRadius)
            {
                return false;
            }
        }

        return true;
    }
}
