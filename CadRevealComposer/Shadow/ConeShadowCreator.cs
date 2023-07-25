namespace CadRevealComposer.Shadow;

using Primitives;
using System;
using System.Numerics;
using Utils;

public static class ConeShadowCreator
{
    public static APrimitive CreateShadow(this Cone cone)
    {
        if (!cone.InstanceMatrix.DecomposeAndNormalize(out _, out var coneRotation, out var conePosition))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + cone.InstanceMatrix);
        }

        var coneHeight = Vector3.Distance(cone.CenterA, cone.CenterB);
        var radius = float.Max(cone.RadiusA, cone.RadiusB);
        var shadowConeScale = new Vector3(radius * 2, radius * 2, coneHeight);

        var shadowBoxMatrix =
            Matrix4x4.CreateScale(shadowConeScale)
            * Matrix4x4.CreateFromQuaternion(coneRotation)
            * Matrix4x4.CreateTranslation(conePosition);

        return new Box(shadowBoxMatrix, cone.TreeIndex, cone.Color, cone.AxisAlignedBoundingBox);
    }
}
