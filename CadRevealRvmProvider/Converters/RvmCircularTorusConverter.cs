namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;
using System;
using System.Collections.Generic;

public static class RvmCircularTorusConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmCircularTorus rvmCircularTorus,
        ulong treeIndex,
        Color color)
    {
        if (!rvmCircularTorus.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmCircularTorus.Matrix);
        }

        var (normal, _) = rotation.DecomposeQuaternion();

        var bbox = rvmCircularTorus.CalculateAxisAlignedBoundingBox()!.ToCadRevealBoundingBox();

        const float oneDegree = 2 * MathF.PI / 360f;
        var arcAngle = rvmCircularTorus.Angle;
        var isTorusSegment = !arcAngle.ApproximatelyEquals(2f * MathF.PI, acceptableDifference: oneDegree);

        yield return new TorusSegment(
            arcAngle,
            rvmCircularTorus.Matrix,
            Radius: rvmCircularTorus.Offset,
            TubeRadius: rvmCircularTorus.Radius,
            treeIndex,
            color,
            bbox
        );

        if (isTorusSegment)
        {
            var offset = rvmCircularTorus.Offset * scale.X;
            var radius = rvmCircularTorus.Radius * scale.X;
            var diameter = 2f * radius;

            var localToWorldXAxisA = Vector3.Transform(Vector3.UnitX, rotation);
            var arcRotation = rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, arcAngle);
            var localToWorldXAxisB = Vector3.Transform(Vector3.UnitX, arcRotation);

            var positionCapA = position + localToWorldXAxisA * offset;
            var positionCapB = position + localToWorldXAxisB * offset;

            var normalCapA = Vector3.Normalize(Vector3.Cross(normal, localToWorldXAxisA));
            var normalCapB = -Vector3.Normalize(Vector3.Cross(normal, localToWorldXAxisB));

            var (showCapA, showCapB) = PrimitiveCapHelper.CalculateCapVisibility(rvmCircularTorus, positionCapA, positionCapB);

            if (showCapA)
            {
                var matrixCapA =
                    Matrix4x4.CreateScale(diameter, diameter, 1f)
                    * Matrix4x4.CreateFromQuaternion(rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f))
                    * Matrix4x4.CreateTranslation(positionCapA);

                yield return new Circle(
                    matrixCapA,
                    normalCapA,
                    treeIndex,
                    color,
                    bbox
                );
            }

            if (showCapB)
            {
                var matrixCapB =
                    Matrix4x4.CreateScale(diameter, diameter, 1f)
                    * Matrix4x4.CreateFromQuaternion(arcRotation * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f))
                    * Matrix4x4.CreateTranslation(positionCapB);

                yield return new Circle(
                    matrixCapB,
                    normalCapB,
                    treeIndex,
                    color,
                    bbox
                );
            }
        }
    }
}