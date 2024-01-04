namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using CapVisibilityHelpers;
using RvmSharp.Primitives;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

public static class RvmSphericalDishConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmSphericalDish rvmSphericalDish,
        ulong treeIndex,
        Color color,
        FailedPrimitivesLogObject? failedPrimitivesLogObject = null
    )
    {
        if (!rvmSphericalDish.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmSphericalDish.Matrix);
        }
        if (!rvmSphericalDish.CanBeConverted(scale, rotation, failedPrimitivesLogObject))
            yield break;

        (Vector3 normal, _) = rotation.DecomposeQuaternion();

        var height = rvmSphericalDish.Height * scale.X;
        var baseRadius = rvmSphericalDish.BaseRadius * scale.X;
        var baseDiameter = baseRadius * 2.0f;
        // radius R = h / 2 + c^2 / (8 * h), where c is the cord length or 2 * baseRadius
        var sphereRadius = height / 2 + baseRadius * baseRadius / (2 * height);
        var d = sphereRadius - height;
        var upVector = Vector3.Transform(Vector3.UnitZ, Matrix4x4.CreateFromQuaternion(rotation));
        var center = position - upVector * d;
        var bbBox = rvmSphericalDish.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

        var matrixCap =
            Matrix4x4.CreateScale(baseDiameter, baseDiameter, 1f)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        yield return new EllipsoidSegment(sphereRadius, sphereRadius, height, center, normal, treeIndex, color, bbBox);

        var showCap = CapVisibility.IsCapVisible(rvmSphericalDish, position);

        if (showCap)
        {
            yield return CircleConverterHelper.ConvertCircle(matrixCap, -normal, treeIndex, color);
        }
    }
}
