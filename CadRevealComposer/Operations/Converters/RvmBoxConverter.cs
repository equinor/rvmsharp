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

        var connections = rvmBox.Connections.WhereNotNull();

        //if (connections != null)
        //{
        //    int count = 0;
        //    foreach (var connection in connections)
        //    {
        //        count++;
        //    }

            //if (count >= 2)
            //{
            //    //var boxConnections = connections.Where(x => string.Equals(x.GetType(), "RvmBox"));

            //    //if (boxConnections.Count() >= 2)
            //    //    color = Color.Red;
            //}

            //if (count > 0)
            //{

            //}



            //if (count == 1)
            //{
            //    color = Color.Red;
            //}
            //else if (count == 2)
            //{
            //    color = Color.Blue;
            //}
            //else if (count >= 3)
            //{
            //    color = Color.Green;
            //}

            //Console.WriteLine($"#######################Found connection: {count}");
        //}

        var unitBoxScale = Vector3.Multiply(
            scale,
            new Vector3(rvmBox.LengthX, rvmBox.LengthY, rvmBox.LengthZ));

        //var matrix =
        //    Matrix4x4.CreateScale(unitBoxScale)
        //    * Matrix4x4.CreateFromQuaternion(rotation)
        //    * Matrix4x4.CreateTranslation(position);

        //yield return new Box(
        //    matrix,
        //    treeIndex,
        //    color,
        //    rvmBox.CalculateAxisAlignedBoundingBox());

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

        yield return new Quad(
            quadMatrix1,
            treeIndex,
            color,
            bbBox);

        yield return new Quad(
            quadMatrix2,
            treeIndex,
            color,
            bbBox);

        yield return new Quad(
            quadMatrix3,
            treeIndex,
            color,
            bbBox);

        yield return new Quad(
            quadMatrix4,
            treeIndex,
            color,
            bbBox);

        yield return new Quad(
            quadMatrix5,
            treeIndex,
            color,
            bbBox);

        yield return new Quad(
            quadMatrix6,
            treeIndex,
            color,
            bbBox);

    }
}