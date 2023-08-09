namespace CadRevealComposer.Shadow;

using Primitives;
using System;
using System.Numerics;
using Utils;

public static class CylinderShadowCreator
{
    public static APrimitive CreateShadow(this GeneralCylinder cylinder)
    {
        if (!cylinder.InstanceMatrix.DecomposeAndNormalize(out _, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + cylinder.InstanceMatrix);
        }

        var height = Vector3.Distance(cylinder.CenterA, cylinder.CenterB);
        var scale = new Vector3(cylinder.Radius * 2, cylinder.Radius * 2, height);

        var shadowBoxMatrix =
            Matrix4x4.CreateScale(scale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(position);

        return new Box(shadowBoxMatrix, cylinder.TreeIndex, cylinder.Color, cylinder.AxisAlignedBoundingBox);
    }
}
