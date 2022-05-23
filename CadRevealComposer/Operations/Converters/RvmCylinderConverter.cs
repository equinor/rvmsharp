namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using Utils;

public static class RvmCylinderConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmCylinder rvmCylinder,
        ulong treeIndex,
        Color color)
    {
        if (!rvmCylinder.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmCylinder.Matrix);
        }

        // TODO: if scale is not uniform on X,Y, we should create something else
        if (!scale.X.ApproximatelyEquals(scale.Y, 0.001))
        {
            throw new Exception("Non uniform X,Y scale is not implemented.");
        }

        var (normal, _) = rotation.DecomposeQuaternion();

        var bbox = rvmCylinder.CalculateAxisAlignedBoundingBox();

        var height = rvmCylinder.Height * scale.Z;
        var radius = rvmCylinder.Radius * scale.X;
        var halfHeight = height / 2f;
        var diameter = 2f * radius;
        var localXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        var normalA = normal;
        var normalB = -normal;

        var centerA = position + normalA * halfHeight;
        var centerB = position + normalB * halfHeight;

        var matrixCapA =
            Matrix4x4.CreateScale(diameter)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerA);

        var matrixCapB =
            Matrix4x4.CreateScale(diameter)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(centerB);

        yield return new Cone(
            Angle: 0f,
            ArcAngle: 2f * MathF.PI,
            centerA,
            centerB,
            localXAxis,
            radius,
            radius,
            treeIndex,
            color,
            bbox
        );

        yield return new Circle(
            InstanceMatrix: matrixCapA,
            Normal: normalA,
            treeIndex,
            color,
            bbox
        );

        yield return new Circle(
            InstanceMatrix: matrixCapB,
            Normal: normalB,
            treeIndex,
            color,
            bbox
        );
    }
}