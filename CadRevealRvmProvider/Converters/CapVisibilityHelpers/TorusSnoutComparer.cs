namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;

public class TorusSnoutComparer : ICapComparer
{
    public bool ShowCap(CapData torusCapData, CapData snoutCapData)
    {
        var rvmCircularTorus = (RvmCircularTorus)torusCapData.Primitive;
        var rvmSnout = (RvmSnout)snoutCapData.Primitive;

        rvmCircularTorus.Matrix.DecomposeAndNormalize(out var circularTorusScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var torusRadius = rvmCircularTorus.Radius * circularTorusScale.X;

        var isSnoutCapTop = snoutCapData.CapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        if (torusCapData.IsCurrentPrimitive)
        {
            if (semiMinorRadius + CapVisibility.CapComparingBuffer >= torusRadius)
            {
                return false;
            }
        }
        else
        {
            if (torusRadius + CapVisibility.CapComparingBuffer >= semiMajorRadius)
            {
                return false;
            }
        }

        return true;
    }
}