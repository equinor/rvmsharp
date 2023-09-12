namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;

public class CylinderSnoutComparer : ICapComparer
{
    public bool ShowCap(CapData cylinderCapData, CapData snoutCapData)
    {
        var rvmCylinder = (RvmCylinder)cylinderCapData.Primitive;
        var rvmSnout = (RvmSnout)snoutCapData.Primitive;

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
            if (semiMinorRadius + CapVisibility.CapComparingBuffer >= cylinderRadius)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius + CapVisibility.CapComparingBuffer >= semiMajorRadius)
            {
                return false;
            }
        }

        return true;
    }
}
