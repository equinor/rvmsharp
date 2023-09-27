namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;

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
            ? rvmSnout.GetTopCapEllipse().ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().ellipse2DPolar;

        Trace.Assert(MathF.Abs(snoutScale.X / snoutScale.Y - 1.0f) < 0.00001f);
        var semiMinorRadius = snoutEllipse.semiMinorAxis * snoutScale.X;
        var semiMajorRadius = snoutEllipse.semiMajorAxis * snoutScale.X;

        if (cylinderCapData.IsCurrentPrimitive)
        {
            if (semiMinorRadius + CapVisibility.CapOverlapTolerance >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + CapVisibility.CapOverlapTolerance >= semiMajorRadius)
            {
                return false;
            }
        }

        return true;
    }
}
