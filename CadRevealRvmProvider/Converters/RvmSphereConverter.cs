namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using Commons.Utils;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

public static class RvmSphereConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmSphere rvmSphere,
        ulong treeIndex,
        Color color
    )
    {
        if (!rvmSphere.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSphere.Matrix);
        }

        if (!IsValid(scale, rotation))
        {
            Console.WriteLine(
                $"Removed Sphere because of invalid data. Scale: {scale.ToString()} Rotation: {rotation.ToString()}"
            );
            yield break;
        }

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

    private static bool IsValid(Vector3 scale, Quaternion rotation)
    {
        Trace.Assert(scale.IsUniform(), $"Expected Uniform Scale. Was: {scale}");

        if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0)
            return false;

        if (QuaternionHelpers.ContainsInfiniteValue(rotation))
            return false;

        return true;
    }
}
