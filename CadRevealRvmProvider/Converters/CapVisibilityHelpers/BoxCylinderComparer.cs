namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class BoxCylinderComparer
{
    public static bool ShowCap(CapData<RvmBox> boxCapData, CapData<RvmCylinder> cylinderCapData)
    {
        var rvmBox = boxCapData.Primitive;
        var rvmCylinder = cylinderCapData.Primitive;

        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        // Only check for the cylinder, because a box does not have any caps
        if (!cylinderCapData.IsCurrentPrimitive)
        {
            return true;
        }

        // TODO: Is it possible to find out which sides to compare with?
        if (cylinderRadius < halfLengthX && cylinderRadius < halfLengthY && cylinderRadius < halfLengthZ)
        {
            return false;
        }

        return true;
    }
}
