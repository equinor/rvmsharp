namespace CadRevealRvmProvider.Converters.CapVisibilityHelpers;

using CadRevealComposer.Utils;
using RvmSharp.Primitives;

public static class BoxSnoutComparer
{
    public static bool ShowCap(CapData<RvmBox> boxCapData, CapData<RvmSnout> snoutCapData)
    {
        var rvmBox = boxCapData.Primitive;
        var rvmSnout = snoutCapData.Primitive;

        rvmBox.Matrix.DecomposeAndNormalize(out var boxScale, out _, out _);
        rvmSnout.Matrix.DecomposeAndNormalize(out var snoutScale, out _, out _);

        var halfLengthX = rvmBox.LengthX * boxScale.X / 2.0f;
        var halfLengthY = rvmBox.LengthY * boxScale.Y / 2.0f;
        var halfLengthZ = rvmBox.LengthZ * boxScale.Z / 2.0f;

        var isSnoutCapTop = snoutCapData.CapIndex == 1;

        var snoutEllipse = isSnoutCapTop
            ? rvmSnout.GetTopCapEllipse().Ellipse2DPolar
            : rvmSnout.GetBottomCapEllipse().Ellipse2DPolar;

        var snoutMajorAxis = snoutEllipse.SemiMajorAxis * snoutScale.X;

        // Only check for the snout, because a box does not have any caps
        if (snoutCapData.IsCurrentPrimitive)
        {
            // TODO: Is it possible to find out which sides to compare with?
            if (snoutMajorAxis < halfLengthX && snoutMajorAxis < halfLengthY && snoutMajorAxis < halfLengthZ)
            {
                return false;
            }
        }

        return true;
    }
}
