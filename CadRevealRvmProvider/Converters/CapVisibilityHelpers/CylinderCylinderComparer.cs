namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public class CylinderCylinderComparer : ICapComparer
{
    public bool ShowCap(CapData cylinderData1, CapData cylinderData2)
    {
        var rvmCylinder1 = (RvmCylinder)cylinderData1.Primitive;
        var rvmCylinder2 = (RvmCylinder)cylinderData2.Primitive;

        rvmCylinder1.Matrix.DecomposeAndNormalize(out var cylinderScale1, out _, out _);
        rvmCylinder2.Matrix.DecomposeAndNormalize(out var cylinderScale2, out _, out _);

        var cylinderRadius1 = rvmCylinder1.Radius * cylinderScale1.X;
        var cylinderRadius2 = rvmCylinder2.Radius * cylinderScale2.X;

        if (cylinderData1.IsCurrentPrimitive)
        {
            if (cylinderRadius2 + CapVisibility.CapComparingBuffer >= cylinderRadius1)
            {
                return false;
            }
        }
        else
        {
            if (cylinderRadius1 + CapVisibility.CapComparingBuffer >= cylinderRadius2)
            {
                return false;
            }
        }

        return true;
    }
}
