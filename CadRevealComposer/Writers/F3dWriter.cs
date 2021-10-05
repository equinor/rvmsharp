namespace CadRevealComposer.Writers
{
    using System;
    using System.Collections.Immutable;
    using System.Drawing;
    using System.IO;
    using System.Numerics;

    public static class F3dWriter
    {
        private const uint MAGIC_BYTES = 0x4644_3346;

        public record Scene(ImmutableArray<Sector> Sectors, int RootSectorId);

        public record Sector(uint MagicBytes, uint FormatVersion, uint OptimizerVersion, ulong SectorId, ulong? ParentSectorId, Vector3 BboxMin, Vector3 BboxMax, SectorContents? SectorContents);

        public record SectorContents(ImmutableArray<uint> GridSize, Vector3 Origin, float GridIncrement, ImmutableArray<Node> Nodes);

        public record Node(CompressFlags CompressType, ulong NodeId, ulong TreeIndex, Color Color, ImmutableArray<Face> Faces);

        public record Face(FaceFlags FaceFlags, byte Repetitions, ulong Index, Color Color);

        [Flags]
        public enum FaceFlags : byte
        {
            POSITIVE_X_VISIBLE = 0b0000_0001,
            POSITIVE_Y_VISIBLE = 0b0000_0010,
            POSITIVE_Z_VISIBLE = 0b0000_0100,
            NEGATIVE_X_VISIBLE = 0b0000_1000,
            NEGATIVE_Y_VISIBLE = 0b0001_0000,
            NEGATIVE_Z_VISIBLE = 0b0010_0000,
            // 0b0100_0000 is reserved and left out to trigger an error if used
            MULTIPLE = 0b1000_0000
        }

        [Flags]
        public enum CompressFlags : byte
        {
            POSITIVE_X_REPEAT_Z = 0b0000_0001,
            POSITIVE_Y_REPEAT_Z = 0b0000_0010,
            POSITIVE_Z_REPEAT_Y = 0b0000_0100,
            NEGATIVE_X_REPEAT_Z = 0b0000_1000,
            NEGATIVE_Y_REPEAT_Z = 0b0001_0000,
            NEGATIVE_Z_REPEAT_Y = 0b0010_0000,
            HAS_COLOR_ON_EACH_CELL = 0b0100_0000,
            INDEX_IS_LONG = 0b1000_0000
        }


        public static void WriteSector(Scene scene, Stream stream)
        {
            foreach (var sector in scene.Sectors)
            {
                WriteSector(sector, stream);
            }
        }

        private static void WriteSector(Sector sector, Stream stream)
        {
            stream.WriteUint32(MAGIC_BYTES);
            stream.WriteUint32(sector.FormatVersion);
            stream.WriteUint32(sector.OptimizerVersion);
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
                stream.WriteUint32(0);
            }
            else
            {
                stream.WriteUint32((uint)sector.SectorContents.Nodes.Length);
                stream.WriteUint32(sector.SectorContents.GridSize[0]);
                stream.WriteUint32(sector.SectorContents.GridSize[1]);
                stream.WriteUint32(sector.SectorContents.GridSize[2]);
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
            var hasColorOnEachCell = (node.CompressType & CompressFlags.HAS_COLOR_ON_EACH_CELL) == CompressFlags.HAS_COLOR_ON_EACH_CELL;
            var indexAsUInt64 = (node.CompressType & CompressFlags.INDEX_IS_LONG) == CompressFlags.INDEX_IS_LONG;

            stream.WriteUint64(node.NodeId);
            stream.WriteUint64(node.TreeIndex);
            stream.WriteUint32((uint)node.Faces.Length);
            stream.WriteByte((byte)node.CompressType);

            if (!hasColorOnEachCell)
            {
                // TODO: verify component order
                // TODO: verify component order
                // TODO: verify component order

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
            var multiple = (face.FaceFlags & FaceFlags.MULTIPLE) == FaceFlags.MULTIPLE;

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
                // TODO: verify component order
                // TODO: verify component order
                // TODO: verify component order

                stream.WriteByte(face.Color.R);
                stream.WriteByte(face.Color.G);
                stream.WriteByte(face.Color.B);
            }
        }
    }
}