namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using Microsoft.EntityFrameworkCore.Update;
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
        if (rvmCylinder.Height == 0 || rvmCylinder.Radius == 0)
        {
            Console.WriteLine("Removed cylinder becuse height or radius were 0");
            yield break;
        }

        if (!rvmCylinder.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmCylinder.Matrix);
        }

        if (
            !(
                float.IsFinite(rotation.X)
                && float.IsFinite(rotation.Y)
                && float.IsFinite(rotation.Z)
                && float.IsFinite(rotation.W)
            )
        )
        {
            Console.WriteLine($"Cylinder was removed due to invalid rotation values: {rotation}");
            yield break;
        }

        if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            Console.WriteLine("Warning: Found cylinder with non-uniform X and Y scale");
        }

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

        if (scale.X != 0 && scale.Y == 0)
        {
            Console.WriteLine("Warning: Found cylinder where X scale was non-zero and Y scale was zero");
        }
        else if (!scale.X.ApproximatelyEquals(scale.Y, 0.0001))
        {
            throw new Exception("Cylinders with non-uniform scale is not implemented!");
        }

        var diameter = 2f * radius;
        var height = rvmCylinder.Height * scale.Z;
        var halfHeight = height / 2f;

        var localToWorldXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var normalA = normal;
        var normalB = -normal;

        var centerA = position + normalA * halfHeight;
        var centerB = position + normalB * halfHeight;

        var (showCapA, showCapB) = PrimitiveCapHelper.CalculateCapVisibility(rvmCylinder, centerA, centerB);

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

        if (showCapA)
        {
            var matrixCapA =
                Matrix4x4.CreateScale(diameter, diameter, 1f)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerA);

            yield return new Circle(InstanceMatrix: matrixCapA, Normal: normalA, treeIndex, color, bbox);

            // var matrixBox =
            //     Matrix4x4.CreateScale(diameter, diameter, 0.001f)
            //     * Matrix4x4.CreateFromQuaternion(rotation)
            //     * Matrix4x4.CreateTranslation(centerA);
            // yield return new Box(matrixBox, treeIndex, Color.Yellow, bbox);
        }

        if (showCapB)
        {
            var matrixCapB =
                Matrix4x4.CreateScale(diameter, diameter, 1f)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(centerB);

            yield return new Circle(InstanceMatrix: matrixCapB, Normal: normalB, treeIndex, color, bbox);

            // var matrixBox =
            //     Matrix4x4.CreateScale(diameter, diameter, 0.001f)
            //     * Matrix4x4.CreateFromQuaternion(rotation)
            //     * Matrix4x4.CreateTranslation(centerA);
            // yield return new Box(matrixBox, treeIndex, Color.Yellow, bbox);
        }
    }
}
