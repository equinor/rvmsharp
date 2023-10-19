namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using CapVisibilityHelpers;
using Commons.Utils;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

public static class RvmCylinderConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmCylinder rvmCylinder,
        ulong treeIndex,
        Color color
    )
    {
        if (!rvmCylinder.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmCylinder.Matrix);
        }

        if (!IsValid(scale, rotation, rvmCylinder.Radius))
        {
            Console.WriteLine(
                $"Removed cylinder because of invalid data. Scale: {scale.ToString()} Rotation: {rotation.ToString()} Radius: {rvmCylinder.Radius}"
            );
            yield break;
        }

        var (normal, _) = rotation.DecomposeQuaternion();
        var bbox = rvmCylinder.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();
        var radius = rvmCylinder.Radius * scale.X;

        var diameter = 2f * radius;
        var height = rvmCylinder.Height * scale.Z;
        var halfHeight = height / 2f;

        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var normalA = normal;
        var normalB = -normal;

        var centerA = position + normalA * halfHeight;
        var centerB = position + normalB * halfHeight;

        var (showCapA, showCapB) = CapVisibility.IsCapsVisible(rvmCylinder, centerA, centerB);

        if (height != 0) // If height is zero, just return a Circle only
        {
            yield return new Cone(
                Angle: 0f,
                ArcAngle: 2f * MathF.PI,
                centerA,
                centerB,
                localToWorldXAxis,
                radius,
                radius,
                treeIndex,
                color,
                bbox
            );
        }

        if (radius == 0) //Don't add caps if radius is zero
            yield break;

        if (showCapA)
        {
            var matrixCapA =
                Matrix4x4.CreateScale(diameter, diameter, 1f)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerA);

            yield return CircleConverterHelper.ConvertCircle(matrixCapA, normalA, treeIndex, color);
        }

        if (showCapB && height != 0) // If height is zero, return a Circle only
        {
            var matrixCapB =
                Matrix4x4.CreateScale(diameter, diameter, 1f)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerB);

            yield return CircleConverterHelper.ConvertCircle(matrixCapB, normalB, treeIndex, color);
        }
    }

    private static bool IsValid(Vector3 scale, Quaternion rotation, float radius)
    {
        if (QuaternionHelpers.ContainsInfiniteValue(rotation))
        {
            return false;
        }

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            throw new Exception("Cylinders with non-uniform scale is not implemented!");
        }

        if (radius <= 0)
        {
            return false;
        }

        return true;
    }
}
