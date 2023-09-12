namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public class BoxCylinderComparer : ICapComparer
{
    public bool ShowCap(CapData boxCapData, CapData cylinderCapData)
    {
        var rvmBox = (RvmBox)boxCapData.Primitive;
        var rvmCylinder = (RvmCylinder)cylinderCapData.Primitive;

        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmCylinder.Matrix.DecomposeAndNormalize(out var cylinderScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var cylinderRadius = rvmCylinder.Radius * cylinderScale.X;

        // Only check for the cylinder, because a box does not have any caps
        if (cylinderCapData.IsCurrentPrimitive)
        {
            // TODO: Is it possible to find out which sides to compare with?
            if (cylinderRadius < halfLengthX && cylinderRadius < halfLengthY && cylinderRadius < halfLengthZ)
            {
                return false;
            }
        }

        return true;
    }
}
