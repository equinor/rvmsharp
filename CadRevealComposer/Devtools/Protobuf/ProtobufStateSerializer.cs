namespace CadRevealComposer.Devtools.Protobuf;

using Primitives;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;

public static class ProtobufStateSerializer
{
    private static bool _hasAlreadyRegisteredSurrogates;

    public static void WriteAPrimitiveStateToStream(Stream targetStream, APrimitive[] primitives)
    {
        var timer = Stopwatch.StartNew();
        EnsureSurrogatesRegistered();
        Serializer.Serialize(targetStream, primitives, PrefixStyle.Base128);
        Console.WriteLine(
            $"Serialized {primitives.Length, 10} primitives into {targetStream.Length / 1024 / 1024, 5}MB in {timer.Elapsed}"
        );
    }

    public static APrimitive[] ReadAPrimitiveStateFromStream(Stream targetStream)
    {
        var timer = Stopwatch.StartNew();
        EnsureSurrogatesRegistered();

        var primitives = Serializer.Deserialize<APrimitive[]>(targetStream);
        Console.WriteLine(
            $"Deserialized {primitives.Length, 8} primitives from {targetStream.Length / 1024 / 1024, 5}MB in {timer.Elapsed}"
        );
        return primitives;
    }

    private static void EnsureSurrogatesRegistered()
    {
        if (_hasAlreadyRegisteredSurrogates)
            return;
        // A surrogate is a way in Protobuf-net to serialize/deserialize non-owned types
        RuntimeTypeModel.Default[typeof(Color)].SetSurrogate(typeof(ProtoColor));
        RuntimeTypeModel.Default[typeof(Matrix4x4)].SetSurrogate(typeof(ProtoMatrix4x4));
        RuntimeTypeModel.Default[typeof(Vector3)].SetSurrogate(typeof(ProtoVector3));
        RuntimeTypeModel.Default[typeof(Vector4)].SetSurrogate(typeof(ProtoVector4));
        _hasAlreadyRegisteredSurrogates = true;
    }

    [Serializable]
    [ProtoContract]
    private struct ProtoColor
    {
        [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
        public uint Argb;

        public static implicit operator Color(ProtoColor c)
        {
            return Color.FromArgb((int)c.Argb);
        }

        public static implicit operator ProtoColor(Color c)
        {
            return new ProtoColor { Argb = (uint)c.ToArgb() };
        }
    }

    [ProtoContract]
    public struct ProtoMatrix4x4
    {
        [ProtoMember(1, OverwriteList = true)]
        public float[] M { get; set; } // Using an array for better binary packing

        public static implicit operator Matrix4x4(ProtoMatrix4x4 c)
        {
            return new Matrix4x4()
            {
                M11 = c.Matrix[0],
                M12 = c.Matrix[1],
                M13 = c.Matrix[2],
                M14 = c.Matrix[3],
                M21 = c.Matrix[4],
                M22 = c.Matrix[5],
                M23 = c.Matrix[6],
                M24 = c.Matrix[7],
                M31 = c.Matrix[8],
                M32 = c.Matrix[9],
                M33 = c.Matrix[10],
                M34 = c.Matrix[11],
                M41 = c.Matrix[12],
                M42 = c.Matrix[13],
                M43 = c.Matrix[14],
                M44 = c.Matrix[15],
            };
        }

        public static implicit operator ProtoMatrix4x4(Matrix4x4 c)
        {
            return new ProtoMatrix4x4()
            {
                Matrix = new[]
                {
                    c.M11,
                    c.M12,
                    c.M13,
                    c.M14,
                    c.M21,
                    c.M22,
                    c.M23,
                    c.M24,
                    c.M31,
                    c.M32,
                    c.M33,
                    c.M34,
                    c.M41,
                    c.M42,
                    c.M43,
                    c.M44,
                }
            };
        }
    }

    [Serializable]
    [ProtoContract]
    private class ProtoVector3
    {
        [ProtoMember(1)]
        public float X { get; set; }

        [ProtoMember(2)]
        public float Y { get; set; }

        [ProtoMember(3)]
        public float Z { get; set; }

        public ProtoVector3() { }

        public ProtoVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static implicit operator Vector3(ProtoVector3 p)
        {
            return new Vector3(p.X, p.Y, p.Z);
        }

        public static implicit operator ProtoVector3(Vector3 v)
        {
            return new ProtoVector3(v.X, v.Y, v.Z);
        }
    }

    [Serializable]
    [ProtoContract]
    private class ProtoVector4
    {
        [ProtoMember(1)]
        public float X { get; set; }

        [ProtoMember(2)]
        public float Y { get; set; }

        [ProtoMember(3)]
        public float Z { get; set; }

        [ProtoMember(4)]
        public float W { get; set; }

        public ProtoVector4() { }

        public ProtoVector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static implicit operator Vector4(ProtoVector4 p)
        {
            return new Vector4(p.X, p.Y, p.Z, p.W);
        }

        public static implicit operator ProtoVector4(Vector4 v)
        {
            return new ProtoVector4(v.X, v.Y, v.Z, v.W);
        }
    }
}
