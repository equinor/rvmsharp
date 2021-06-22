namespace CadRevealComposer.Writers
{
    using Primitives;
    using System;
    using System.Collections.Immutable;
    using System.IO;
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
        public static void WriteRgbaArray(this Stream stream, int[][] value)
        {
            stream.WriteUint32((uint)value.Length);
            stream.WriteByte(4);

            foreach (var c in value)
            {
                stream.WriteByte((byte)c[0]);
                stream.WriteByte((byte)c[1]);
                stream.WriteByte((byte)c[2]);
                stream.WriteByte((byte)c[3]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteNormalArray(this Stream stream, float[][] value)
        {
            stream.WriteUint32((uint)value.Length);
            stream.WriteByte(sizeof(float) * 3);

            foreach (var n in value)
            {
                stream.WriteFloat(n[0]);
                stream.WriteFloat(n[1]);
                stream.WriteFloat(n[2]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTextureArray(this Stream stream, TriangleMesh.Texture[] value)
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