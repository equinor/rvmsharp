namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Drawing;

public static class RvmSphereConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmSphere rvmSphere,
        ulong treeIndex,
        Color color,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (!rvmSphere.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSphere.Matrix);
        }

        if (!rvmSphere.CanBeConverted(scale, rotation, failedPrimitivesLogObject))
            yield break;

        var (normal, _) = rotation.DecomposeQuaternion();

        var radius = rvmSphere.Radius * scale.X;
        var diameter = radius * 2f;
        yield return new EllipsoidSegment(
            radius,
            radius,
            diameter,
            position,
            normal,
            treeIndex,
            color,
            rvmSphere.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox()
        );
    }
}
