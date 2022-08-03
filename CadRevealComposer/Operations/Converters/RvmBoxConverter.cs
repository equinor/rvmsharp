namespace CadRevealComposer.Operations.Converters;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Utils;

public static class RvmBoxConverter
{
    private static int QuadInsteadThreshold = 3; // Number of large enough connections before using quad instead

    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(
        this RvmBox rvmBox,
        ulong treeIndex,
        Color color)
    {
        if (!rvmBox.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmBox.Matrix);
        }

        var bbBox = rvmBox.CalculateAxisAlignedBoundingBox();

        var unitBoxScale = Vector3.Multiply(
            scale,
            new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

        var connections = rvmBox.Connections.WhereNotNull();

        var connectionDirections = new List<Vector3>();

        foreach (var connection in connections)
        {
            var temp = MathF.Min(rvmBox.LengthX * unitBoxScale.X, rvmBox.LengthY * unitBoxScale.Y);
            var smallestBoxSide = MathF.Min(temp, rvmBox.LengthZ * unitBoxScale.Z);

            if (connection.HasConnectionType(RvmConnection.ConnectionType.HasCircularSide))
            {
                if ()
                {
                    connectionDirections.Add(connection.Direction);
                }
            }
            else
            {
                if ()
                {
                    connectionDirections.Add(connection.Direction);
                }
            }
        }

        if (connections.Count() >= QuadInsteadThreshold)
        {
            var matrix =
                Matrix4x4.CreateScale(unitBoxScale)
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(position);

            yield return new Box(
                matrix,
                treeIndex,
                color,
                rvmBox.CalculateAxisAlignedBoundingBox());
        }
        else
        {
            color = Color.Red;

            var halfPiAroundX = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
            var halfPiAroundY = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2f);

            var (normal, _) = rotation.DecomposeQuaternion();

            var newPositionUp = position + normal * unitBoxScale.Z / 2.0f;
            var newPositionDown = position - normal * unitBoxScale.Z / 2.0f;

            var newRotationFrontAndBack = rotation * halfPiAroundY;
            var (normalFront, _) = newRotationFrontAndBack.DecomposeQuaternion();
            var newPositionFront = position + normalFront * unitBoxScale.X / 2.0f;
            var newPostionBack = position - normalFront * unitBoxScale.X / 2.0f;

            var newRotationRightAndLeft = rotation * halfPiAroundX;
            var (normalRight, _) = newRotationRightAndLeft.DecomposeQuaternion();
            var newPositionRight = position + normalRight * unitBoxScale.Y / 2.0f;
            var newPositionLeft = position - normalRight * unitBoxScale.Y / 2.0f;

            // Up
            var quadMatrix1 =
                Matrix4x4.CreateScale(new Vector3(unitBoxScale.X, unitBoxScale.Y, 0))
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(newPositionUp);

            // Down
            var quadMatrix2 =
                Matrix4x4.CreateScale(new Vector3(unitBoxScale.X, unitBoxScale.Y, 0))
                * Matrix4x4.CreateFromQuaternion(rotation)
                * Matrix4x4.CreateTranslation(newPositionDown);

            // Front
            var quadMatrix3 =
                Matrix4x4.CreateScale(new Vector3(unitBoxScale.Z, unitBoxScale.Y, 0))
                * Matrix4x4.CreateFromQuaternion(newRotationFrontAndBack)
                * Matrix4x4.CreateTranslation(newPositionFront);

            // Back
            var quadMatrix4 =
                Matrix4x4.CreateScale(new Vector3(unitBoxScale.Z, unitBoxScale.Y, 0))
                * Matrix4x4.CreateFromQuaternion(newRotationFrontAndBack)
                * Matrix4x4.CreateTranslation(newPostionBack);

            // Right
            var quadMatrix5 =
                Matrix4x4.CreateScale(new Vector3(unitBoxScale.X, unitBoxScale.Z, 0))
                * Matrix4x4.CreateFromQuaternion(newRotationRightAndLeft)
                * Matrix4x4.CreateTranslation(newPositionRight);

            // Left
            var quadMatrix6 =
                Matrix4x4.CreateScale(new Vector3(unitBoxScale.X, unitBoxScale.Z, 0))
                * Matrix4x4.CreateFromQuaternion(newRotationRightAndLeft)
                * Matrix4x4.CreateTranslation(newPositionLeft);

            color = Color.Red;

            bool up = false;
            bool down = false;
            bool front = false;
            bool back = false;
            bool right = false;
            bool left = false;

            foreach (var direction in connectionDirections)
            {
                if (direction.EqualsWithinTolerance(-normal, 0.1f))
                    up = true;
                else if (direction.EqualsWithinTolerance(normal, 0.1f))
                    down = true;
                else if (direction.EqualsWithinTolerance(-normalFront, 0.1f))
                    front = true;
                else if (direction.EqualsWithinTolerance(normalFront, 0.1f))
                    back = true;
                else if (direction.EqualsWithinTolerance(-normalRight, 0.1f))
                    right = true;
                else if (direction.EqualsWithinTolerance(normalRight, 0.1f))
                    left = true;
            }

            // Up
            yield return new Quad(
                quadMatrix1,
                treeIndex,
                up ? color : Color.Green,
                bbBox);

            // Down
            yield return new Quad(
                quadMatrix2,
                treeIndex,
                down ? color : Color.Green,
                bbBox);

            // Front
            yield return new Quad(
                quadMatrix3,
                treeIndex,
                front ? color : Color.Green,
                bbBox);

            // Back
            yield return new Quad(
                quadMatrix4,
                treeIndex,
                back ? color : Color.Green,
                bbBox);

            // Right
            yield return new Quad(
                quadMatrix5,
                treeIndex,
                right ? color : Color.Green,
                bbBox);

            // Left
            yield return new Quad(
                quadMatrix6,
                treeIndex,
                left ? color : Color.Green,
                bbBox);
        }
    }
}