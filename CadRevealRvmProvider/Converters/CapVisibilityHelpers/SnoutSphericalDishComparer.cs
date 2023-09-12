namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;

public class SnoutSphericalDishComparer : ICapComparer
{
    public bool ShowCap(CapData snoutCapData, CapData sphericalDishCapData)
    {
        var rvmSnout = (RvmSnout)snoutCapData.Primitive;
        var rvmSphericalDish = (RvmSphericalDish)sphericalDishCapData.Primitive;

        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);
        rvmSphericalDish.Matrix.DecomposeAndNormalize(out var sphericalDishScale, out _, out _);

        var isSnoutCapTop = snoutCapData.CapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        var sphericalDishRadius = rvmSphericalDish.BaseRadius * sphericalDishScale.X;

        if (snoutCapData.IsCurrentPrimitive)
        {
            if (sphericalDishRadius + CapVisibility.CapComparingBuffer >= semiMajorRadius)
            {
                return false;
            }
        }
        else
        {
            if (semiMinorRadius + CapVisibility.CapComparingBuffer >= sphericalDishRadius)
            {
                return false;
            }
        }

        return true;
    }
}
