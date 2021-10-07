namespace CadRevealComposer.Writers
{
    using System;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.IO;
    using System.Numerics;

    public static class F3dWriter
    {
        private const uint MagicBytes = 0x4644_3346;
        private const uint FormatVersion = 8;
        private const uint OptimizerVersion = 1;

        public record Scene(ImmutableArray<Sector> Sectors, int RootSectorId);

        public record Sector(ulong SectorId, ulong? ParentSectorId, Vector3 BboxMin, Vector3 BboxMax, SectorContents? SectorContents);

        public record SectorContents(uint GridSizeX, uint GridSizeY, uint GridSizeZ, Vector3 Origin, float GridIncrement, ImmutableArray<Node> Nodes);

        public record Node(CompressFlags CompressType, ulong NodeId, ulong TreeIndex, Color Color, ImmutableArray<Face> Faces);

        public record Face(FaceFlags FaceFlags, byte Repetitions, ulong Index, Color Color);

        [Flags]
        public enum FaceFlags : byte
        {
            PositiveXVisible = 0b0000_0001,
            PositiveYVisible = 0b0000_0010,
            PositiveZVisible = 0b0000_0100,
            NegativeXVisible = 0b0000_1000,
            NegativeYVisible = 0b0001_0000,
            NegativeZVisible = 0b0010_0000,
            // 0b0100_0000 is reserved and left out to trigger an error if used
            Multiple = 0b1000_0000
        }

        [Flags]
        public enum CompressFlags : byte
        {
            PositiveXRepeatZ = 0b0000_0001,
            PositiveYRepeatZ = 0b0000_0010,
            PositiveZRepeatY = 0b0000_0100,
            NegativeXRepeatZ = 0b0000_1000,
            NegativeYRepeatZ = 0b0001_0000,
            NegativeZRepeatY = 0b0010_0000,
            HasColorOnEachCell = 0b0100_0000,
            IndexIsLong = 0b1000_0000
        }

        public static void WriteScene(Scene scene, Stream stream)
        {
            foreach (var sector in scene.Sectors)
            {
                WriteSector(sector, stream);
            }
        }

        private static void WriteSector(Sector sector, Stream stream)
        {
            stream.WriteUint32(MagicBytes);
            stream.WriteUint32(FormatVersion);
            stream.WriteUint32(OptimizerVersion);
            stream.WriteUint64(sector.SectorId);
            stream.WriteUint64(sector.ParentSectorId ?? ulong.MaxValue);

            stream.WriteFloat(sector.BboxMin.X);
            stream.WriteFloat(sector.BboxMin.Y);
            stream.WriteFloat(sector.BboxMin.Z);
            stream.WriteFloat(sector.BboxMax.X);
            stream.WriteFloat(sector.BboxMax.Y);
            stream.WriteFloat(sector.BboxMax.Z);

            if (sector.SectorContents is null)
            {
                stream.WriteUint32(0); // node count
            }
            else
            {
                stream.WriteUint32((uint)sector.SectorContents.Nodes.Length);
                stream.WriteUint32(sector.SectorContents.GridSizeX);
                stream.WriteUint32(sector.SectorContents.GridSizeY);
                stream.WriteUint32(sector.SectorContents.GridSizeZ);
                stream.WriteFloat(sector.SectorContents.Origin.X);
                stream.WriteFloat(sector.SectorContents.Origin.Y);
                stream.WriteFloat(sector.SectorContents.Origin.Z);
                stream.WriteFloat(sector.SectorContents.GridIncrement);

                foreach (var node in sector.SectorContents.Nodes)
                {
                    WriteNode(node, stream);
                }
            }
        }

        private static void WriteNode(Node node, Stream stream)
        {
            var hasColorOnEachCell = node.CompressType.HasFlag(CompressFlags.HasColorOnEachCell);
            var indexAsUInt64 = node.CompressType.HasFlag(CompressFlags.IndexIsLong);

            stream.WriteUint64(node.NodeId);
            stream.WriteUint64(node.TreeIndex);
            stream.WriteUint32((uint)node.Faces.Length);
            stream.WriteByte((byte)node.CompressType);

            if (!hasColorOnEachCell)
            {
                stream.WriteByte(node.Color.R);
                stream.WriteByte(node.Color.G);
                stream.WriteByte(node.Color.B);
            }

            foreach (var face in node.Faces)
            {
                WriteFace(face, stream, hasColorOnEachCell, indexAsUInt64);
            }
        }

        private static void WriteFace(Face face, Stream stream, bool hasColorOnEachCell, bool indexAsUInt64)
        {
            if (!indexAsUInt64 && face.Index > int.MaxValue)
            {
                throw new InvalidOperationException($"Face.Index is set to UInt32 and the value is outside bounds. Value is {face.Index}");
            }

            var multiple = face.FaceFlags.HasFlag(FaceFlags.Multiple);

            if (indexAsUInt64)
            {
                stream.WriteUint64(face.Index);
            }
            else
            {
                stream.WriteUint32((uint)face.Index);
            }
            stream.WriteByte((byte)face.FaceFlags);
            stream.WriteByte(multiple ? face.Repetitions : (byte)0);
            if (hasColorOnEachCell)
            {
                stream.WriteByte(face.Color.R);
                stream.WriteByte(face.Color.G);
                stream.WriteByte(face.Color.B);
            }
        }
    }
}