namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Drawing;
using System.Numerics;
using Utils;

public static class RvmCylinderConverter
{
    public static APrimitive ConvertToRevealPrimitive(
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

        // TODO
        // TODO
        // TODO
        // TODO
        // let rotation = Rotation3::rotation_between(&Vector3::z_axis(), &normal)
        //     .unwrap_or_else(|| Rotation3::from_axis_angle(&Vector3::x_axis(), PI));

        // TODO: create caps GeneralRing
        // TODO: create caps GeneralRing
        // TODO: create caps GeneralRing
        // TODO: create caps GeneralRing

        (Vector3 normal, float rotationAngle) = rotation.DecomposeQuaternion();

        var height = rvmCylinder.Height * scale.Z;
        var radius = rvmCylinder.Radius * scale.X;

        var halfHeight = height / 2f;
        var centerA = position + normal * halfHeight;
        var centerB = position - normal * halfHeight;

        var localZAxis = Vector3.Transform(Vector3.UnitZ, rotation);
        var planeA = new Vector4(localZAxis, halfHeight);
        var planeB = new Vector4(-localZAxis, halfHeight);

        var angle = Vector3.UnitZ.AngleTo(normal);
        var localXAxis = Vector3.Transform(Vector3.UnitX, rotation);

        return new GeneralCylinder(
            angle,
            MathF.PI * 2f,
            centerA,
            centerB,
            localXAxis,
            planeA,
            planeB,
            radius,
            treeIndex,
            color,
            rvmCylinder.CalculateAxisAlignedBoundingBox()
        );
    }
}