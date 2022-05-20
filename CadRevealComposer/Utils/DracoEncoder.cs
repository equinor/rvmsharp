namespace CadRevealComposer.Utils;

using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Mesh = RvmSharp.Tessellation.Mesh;

// https://github.com/atteneder/DracoUnity/blob/main/Runtime/Scripts/Encoder/DracoEncoder.cs

public struct EncodeResult
{
    public uint indexCount;
    public uint vertexCount;
    public NativeArray<byte> data;

    public void Dispose()
    {
        data.Dispose();
    }
}

// These values must be exactly the same as the values in draco_types.h.
// Attribute data type.
enum DataType
{
    DT_INVALID = 0,
    DT_INT8,
    DT_UINT8,
    DT_INT16,
    DT_UINT16,
    DT_INT32,
    DT_UINT32,
    DT_INT64,
    DT_UINT64,
    DT_FLOAT32,
    DT_FLOAT64,
    DT_BOOL
}

// These values must be exactly the same as the values in
// geometry_attribute.h.
// Attribute type.
enum AttributeType
{
    INVALID = -1,
    POSITION = 0,
    NORMAL,
    COLOR,
    TEX_COORD,
    // A special id used to mark attributes that are not assigned to any known
    // predefined use case. Such attributes are often used for a shader specific
    // data.
    GENERIC
}

public static class DracoEncoder
{

    const string DRACOENC_LIB = "dracoencoder";

    struct AttributeData
    {
        public int stream;
        public int offset;
    }

    /// <summary>
    /// Calculates the idea quantization value based on the largest dimension and desired precision
    /// </summary>
    /// <param name="largestDimension">Length of the largest dimension (width/depth/height)</param>
    /// <param name="precision">Desired minimum precision in world units</param>
    /// <returns></returns>
    static int GetIdealQuantization(float largestDimension, float precision)
    {
        var value = MathF.RoundToInt(largestDimension / precision);
        var mostSignificantBit = -1;
        while (value > 0)
        {
            mostSignificantBit++;
            value >>= 1;
        }
        return MathF.Clamp(mostSignificantBit, 4, 24);
    }

    /// <summary>
    /// Applies Draco compression to a given mesh and returns the encoded result.
    /// The quality and quantization parameters are calculated from the mesh's bounds, its worldScale and desired precision.
    /// The quantization parameters help to find a balance between compressed size and quality / precision.
    /// </summary>
    /// <param name="mesh">Input mesh</param>
    /// <param name="worldScale">Local-to-world scale this mesh is present in the scene</param>
    /// <param name="precision">Desired minimum precision in world units</param>
    /// <param name="encodingSpeed">Encoding speed level. 0 means slow and small. 10 is fastest.</param>
    /// <param name="decodingSpeed">Decoding speed level. 0 means slow and small. 10 is fastest.</param>
    /// <param name="normalQuantization">Normal quantization</param>
    /// <param name="texCoordQuantization">Texture coordinate quantization</param>
    /// <param name="colorQuantization">Color quantization</param>
    /// <param name="genericQuantization">Generic quantization (e.g. blend weights and indices). unused at the moment</param>
    /// <returns></returns>
    public static unsafe EncodeResult[] EncodeMesh(
        Mesh mesh,
        Vector3 worldScale,
        float precision = .001f,
        int encodingSpeed = 0,
        int decodingSpeed = 4,
        int normalQuantization = 10,
        int texCoordQuantization = 12,
        int colorQuantization = 8,
        int genericQuantization = 12
        )
    {
        var bounds = mesh.GetBounds();
        var scale = new Vector3(MathF.Abs(worldScale.X), MathF.Abs(worldScale.Y), MathF.Abs(worldScale.Z));
        var maxSize = MathF.Max(bounds.extents.x * scale.X, MathF.Max(bounds.extents.y * scale.Y, bounds.extents.z * scale.Z)) * 2;
        var positionQuantization = GetIdealQuantization(maxSize, precision);

        return EncodeMesh(
            mesh,
            encodingSpeed,
            decodingSpeed,
            positionQuantization,
            normalQuantization,
            texCoordQuantization,
            colorQuantization,
            genericQuantization
            );
    }

    /// <summary>
    /// Applies Draco compression to a given mesh and returns the encoded result.
    /// The quantization parameters help to find a balance between encoded size and quality / precision.
    /// </summary>
    /// <param name="mesh">Input mesh</param>
    /// <param name="encodingSpeed">Encoding speed level. 0 means slow and small. 10 is fastest.</param>
    /// <param name="decodingSpeed">Decoding speed level. 0 means slow and small. 10 is fastest.</param>
    /// <param name="positionQuantization">Vertex position quantization</param>
    /// <param name="normalQuantization">Normal quantization</param>
    /// <param name="texCoordQuantization">Texture coordinate quantization</param>
    /// <param name="colorQuantization">Color quantization</param>
    /// <param name="genericQuantization">Generic quantization (e.g. blend weights and indices). unused at the moment</param>
    /// <returns></returns>
    public static unsafe EncodeResult EncodeMesh(
        MeshPrimitive mesh,
        SharpGLTF.Schema2.Buffer buffer,
        int vertexCount,
        int indexLength,
        int encodingSpeed = 0,
        int decodingSpeed = 4,
        int positionQuantization = 14,
        int normalQuantization = 10,
        int texCoordQuantization = 12,
        int colorQuantization = 8,
        int genericQuantization = 12
        )
    {
        var dracoEncoder = dracoEncoderCreate(vertexCount);

        foreach (var vertexAccessor in mesh.VertexAccessors)
        {
            var attribute = vertexAccessor.Key;
            var format = GetVertexAttributeFormat(attribute);
            var dimension = GetVertexAttributeDimension(attribute);
            attributeIds[attribute] = dracoEncoderSetAttribute(
                dracoEncoder,
                (int)GetAttributeType(attribute),
                GetDataType(format),
                dimension,
                vertexAccessor.Value.SourceBufferView.ByteStride,
                vertexAccessor.Value.ByteOffset);
        }
        
        fixed (IntPtr indicesData = buffer.Content)
        {
            dracoEncoderSetIndices(dracoEncoder, DataType.DT_UINT32, (uint)indexLength, indicesData);
        }

        // For both encoding and decoding (0 = slow and best compression; 10 = fast) 
        dracoEncoderSetCompressionSpeed(dracoEncoder, Math.Clamp(encodingSpeed, 0, 10), Math.Clamp(decodingSpeed, 0, 10));
        dracoEncoderSetQuantizationBits(
            dracoEncoder,
            Math.Clamp(positionQuantization, 4, 24),
            Math.Clamp(normalQuantization, 4, 24),
            Math.Clamp(texCoordQuantization, 4, 24),
            Math.Clamp(colorQuantization, 4, 24),
            Math.Clamp(genericQuantization, 4, 24)
        );

        dracoEncoderEncode(dracoEncoder, false);

        var dracoDataSize = (int)dracoEncoderGetByteLength(dracoEncoder);

        var dracoData = new NativeArray<byte>(dracoDataSize, Allocator.Persistent);
        dracoEncoderCopy(dracoEncoder, dracoData.GetUnsafePtr());

        var result = new EncodeResult
        {
            indexCount = dracoEncoderGetEncodedIndexCount(dracoEncoder),
            vertexCount = dracoEncoderGetEncodedVertexCount(dracoEncoder),
            data = dracoData
        };

        dracoEncoderRelease(dracoEncoder);

        return result;
    }

    private static DataType GetDataType(VertexAttributeFormat format)
    {
        switch (format)
        {
            case VertexAttributeFormat.Float32:
            case VertexAttributeFormat.Float16:
                return DataType.DT_FLOAT32;
            case VertexAttributeFormat.UNorm8:
            case VertexAttributeFormat.UInt8:
                return DataType.DT_UINT8;
            case VertexAttributeFormat.SNorm8:
            case VertexAttributeFormat.SInt8:
                return DataType.DT_INT8;
            case VertexAttributeFormat.UInt16:
            case VertexAttributeFormat.UNorm16:
                return DataType.DT_UINT16;
            case VertexAttributeFormat.SInt16:
            case VertexAttributeFormat.SNorm16:
                return DataType.DT_INT16;
            case VertexAttributeFormat.UInt32:
            case VertexAttributeFormat.SInt32:
                return DataType.DT_INT32;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private static AttributeType GetAttributeType(VertexAttribute attribute)
    {
        switch (attribute)
        {
            case VertexAttribute.Position:
                return AttributeType.POSITION;
            case VertexAttribute.Normal:
                return AttributeType.NORMAL;
            case VertexAttribute.Color:
                return AttributeType.COLOR;
            case VertexAttribute.TexCoord0:
            case VertexAttribute.TexCoord1:
            case VertexAttribute.TexCoord2:
            case VertexAttribute.TexCoord3:
            case VertexAttribute.TexCoord4:
            case VertexAttribute.TexCoord5:
            case VertexAttribute.TexCoord6:
            case VertexAttribute.TexCoord7:
                return AttributeType.TEX_COORD;
            case VertexAttribute.Tangent:
            case VertexAttribute.BlendWeight:
            case VertexAttribute.BlendIndices:
                return AttributeType.GENERIC;
            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null);
        }
    }

    private static unsafe int GetAttributeSize(VertexAttributeFormat format)
    {
        switch (format)
        {
            case VertexAttributeFormat.Float32:
                return sizeof(float);
            case VertexAttributeFormat.Float16:
                return sizeof(half);
            case VertexAttributeFormat.UNorm8:
                return sizeof(byte);
            case VertexAttributeFormat.SNorm8:
                return sizeof(sbyte);
            case VertexAttributeFormat.UNorm16:
                return sizeof(ushort);
            case VertexAttributeFormat.SNorm16:
                return sizeof(short);
            case VertexAttributeFormat.UInt8:
                return sizeof(byte);
            case VertexAttributeFormat.SInt8:
                return sizeof(sbyte);
            case VertexAttributeFormat.UInt16:
                return sizeof(ushort);
            case VertexAttributeFormat.SInt16:
                return sizeof(short);
            case VertexAttributeFormat.UInt32:
                return sizeof(uint);
            case VertexAttributeFormat.SInt32:
                return sizeof(int);
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    [DllImport(DRACOENC_LIB)]
    static extern IntPtr dracoEncoderCreate(int vertexCount);

    [DllImport(DRACOENC_LIB)]
    static extern void dracoEncoderRelease(IntPtr encoder);

    [DllImport(DRACOENC_LIB)]
    static extern void dracoEncoderSetCompressionSpeed(IntPtr encoder, int encodingSpeed, int decodingSpeed);

    [DllImport(DRACOENC_LIB)]
    static extern void dracoEncoderSetQuantizationBits(IntPtr encoder, int position, int normal, int uv, int color, int generic);

    [DllImport(DRACOENC_LIB)]
    static extern bool dracoEncoderEncode(IntPtr encoder, bool preserveTriangleOrder);

    [DllImport(DRACOENC_LIB)]
    static extern uint dracoEncoderGetEncodedVertexCount(IntPtr encoder);

    [DllImport(DRACOENC_LIB)]
    static extern uint dracoEncoderGetEncodedIndexCount(IntPtr encoder);

    [DllImport(DRACOENC_LIB)]
    static extern ulong dracoEncoderGetByteLength(IntPtr encoder);

    [DllImport(DRACOENC_LIB)]
    static extern unsafe void dracoEncoderCopy(IntPtr encoder, void* data);

    [DllImport(DRACOENC_LIB)]
    static extern unsafe bool dracoEncoderSetIndices(IntPtr encoder, DataType indexComponentType, uint indexCount, IntPtr indices);

    [DllImport(DRACOENC_LIB)]
    static extern uint dracoEncoderSetAttribute(IntPtr encoder, int attributeType, DataType dracoDataType, int componentCount, int stride, IntPtr data);
}
