namespace CadRevealComposer.Writers
{
    using Primitives;
    using System;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.IO;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public static class ByteWriterUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUint16(this Stream stream, ushort value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(ushort));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUint32(this Stream stream, uint value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(uint));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUint48(this Stream stream, ulong value)
        {
            for (int i = 0; i < 48; i += 8)
            {
                stream.WriteByte((byte)((value >> i) & 0xff));
            }
        }

        public static void WriteUint64(this Stream stream, ulong value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(ulong));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(this Stream stream, float value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(float));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this Stream stream, double value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, sizeof(double));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloatArray(this Stream stream, float[] value)
        {
            stream.WriteUint32((uint)value.Length);
            stream.WriteByte(sizeof(float));

            foreach (float v in value)
            {
                stream.WriteFloat(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloatArray(this Stream stream, ImmutableSortedSet<float> value)
        {
            stream.WriteUint32((uint)value.Count);
            stream.WriteByte(sizeof(float));

            foreach (float v in value)
            {
                stream.WriteFloat(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUint64Array(this Stream stream, ulong[] value)
        {
            stream.WriteUint32((uint)value.Length);
            stream.WriteByte(sizeof(ulong));

            foreach (ulong v in value)
            {
                stream.WriteUint64(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteRgbaArray(this Stream stream, ImmutableSortedSet<Color> values)
        {
            stream.WriteUint32((uint)values.Count);
            stream.WriteByte(4);

            foreach (var c in values)
            {
                stream.WriteByte((byte)(255 - c.R));
                stream.WriteByte((byte)(255 - c.G));
                stream.WriteByte((byte)(255 - c.B));
                stream.WriteByte((byte)(255 - c.A));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteNormalArray(this Stream stream, ImmutableSortedSet<Vector3> value)
        {
            stream.WriteUint32((uint)value.Count);
            stream.WriteByte(sizeof(float) * 3);

            foreach (var n in value)
            {
                stream.WriteFloat(n.X);
                stream.WriteFloat(n.Y);
                stream.WriteFloat(n.Z);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTextureArray(this Stream stream, Texture[] value)
        {
            stream.WriteUint32((uint)value.Length);
            stream.WriteByte(16);

            foreach (var t in value)
            {
                stream.WriteDouble(t.FileId);
                stream.WriteUint16(t.Width);
                stream.WriteUint16(t.Height);
                stream.WriteUint32(0); // reserved
            }
        }
    }
}