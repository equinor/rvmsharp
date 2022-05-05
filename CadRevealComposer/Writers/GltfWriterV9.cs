namespace CadRevealComposer.Writers;

using Primitives;
using SharpGLTF.Schema2;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using Buffer = System.Buffer;

public static class GltfWriterV9
{
    public static void WriteSector(APrimitive[] /* do NOT replace with IEnumerable */ primitives, Stream stream)
    {
        var model = ModelRoot.CreateModel();
        var scene = model.UseScene(null);
        model.DefaultScene = scene;

        var boxes = primitives.OfType<Box>().ToArray();
        var boxCount = boxes.Length;
        if (boxCount > 0)
        {
            var boxCollectionNode = scene.CreateNode("BoxCollection");
            var meshGpuInstancing = boxCollectionNode.UseExtension<MeshGpuInstancing>();
            var boxCollectionData = CreateBoxCollectionData(boxes);
            var boxCollectionBufferView = model.CreateBufferView(boxCollectionData.Length, boxCollectionData.Length / boxCount);
            Array.Copy(boxCollectionData, boxCollectionBufferView.Content.Array, boxCollectionData.Length);
            var treeIndexAccessor = model.CreateAccessor();
            treeIndexAccessor.SetData(boxCollectionBufferView, 0, boxCount, DimensionType.SCALAR, EncodingType.FLOAT, false);
            var colorAccessor = model.CreateAccessor();
            colorAccessor.SetData(boxCollectionBufferView, 4, boxCount, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
            var matrixAccessor = model.CreateAccessor();
            matrixAccessor.SetData(boxCollectionBufferView, 8, boxCount, DimensionType.MAT4, EncodingType.FLOAT, false);
            meshGpuInstancing.SetAccessor("_treeIndex", treeIndexAccessor);
            meshGpuInstancing.SetAccessor("_color", colorAccessor);
            meshGpuInstancing.SetAccessor("_instanceMatrix", matrixAccessor);
        }

        model.WriteGLB(stream);
    }

    private static byte[] CreateBoxCollectionData(Box[] boxes)
    {
        const int stride = 72; // id + color + matrix
        var size = stride * boxes.Length;
        var buffer = new byte[size];
        var bufferPos = 0;
        foreach (var box in boxes)
        {
            var fIndex = (float)box.TreeIndex;
            var color = box.Color;
            var matrix = box.Matrix;
            buffer.Write(fIndex, ref bufferPos);
            buffer.Write(color, ref bufferPos);
            buffer.Write(matrix, ref bufferPos);
        }
        return buffer;
    }

    public static void Write(this byte[] buffer, float value, ref int bufferPos)
    {
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, buffer, bufferPos, sizeof(float));
        bufferPos += sizeof(float);
    }

    public static void Write(this byte[] buffer, Color color, ref int bufferPos)
    {
        Buffer.BlockCopy(new[]{color.R, color.G, color.B, color.A}, 0, buffer, bufferPos, 4);
        bufferPos += 4;
    }

    public static void Write(this byte[] buffer, Matrix4x4 matrix, ref int bufferPos)
    {
        Buffer.BlockCopy(new float[]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        }, 0, buffer, bufferPos, sizeof(float) * 16);
        bufferPos += sizeof(float) * 16;
    }
}