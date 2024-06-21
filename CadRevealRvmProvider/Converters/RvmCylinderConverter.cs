namespace CadRevealRvmProvider.Converters;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using CapVisibilityHelpers;
using RvmSharp.Primitives;

public static class RvmCylinderConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmCylinder rvmCylinder,
        ulong treeIndex,
        Color color,
        FailedPrimitivesLogObject failedPrimitivesLogObject
    )
    {
        if (!rvmCylinder.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmCylinder.Matrix);
        }

        if (!rvmCylinder.CanBeConverted(scale, rotation, failedPrimitivesLogObject))
            yield break;

        var (normal, _) = rotation.DecomposeQuaternion();

        var bbox = rvmCylinder.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

        /*
        * One case of non-uniform XY-scale on a cylinder on JSB (JS P2) was throwing an exception. Since this was the only case,
        * it was assumed that this was an error in incoming data.
        *
        * To fix this specific case the largest from X and Y is chosen as the scale. Other cases with non-uniform scales should still throw an exception.
        *
        * https://dev.azure.com/EquinorASA/DT%20%E2%80%93%20Digital%20Twin/_workitems/edit/72816/
        */
        var radius = rvmCylinder.Radius * MathF.Max(scale.X, scale.Y);

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

        if (showCapA)
        {
            var matrixCapA =
                Matrix4x4.CreateScale(diameter, diameter, 1f)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerA);

            yield return CircleConverterHelper.ConvertCircle(matrixCapA, normalA, treeIndex, color);
        }

        if (!showCapB || height == 0) // If height is zero, return a Circle only
        {
            yield break;
        }

        var matrixCapB =
            Matrix4x4.CreateScale(diameter, diameter, 1f)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerB);

        yield return CircleConverterHelper.ConvertCircle(matrixCapB, normalB, treeIndex, color);
    }
}
