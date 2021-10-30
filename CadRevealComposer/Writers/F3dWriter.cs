namespace CadRevealComposer.Writers
{
    using Faces;
    using System;
    using System.IO;

    public static class F3dWriter
    {
        private const uint MagicBytes = 0x4644_3346;
        private const uint FormatVersion = 8;
        private const uint OptimizerVersion = 1;

        public static void WriteSector(SectorFaces sectorFaces, Stream stream)
        {
            stream.WriteUint32(MagicBytes);
            stream.WriteUint32(FormatVersion);
            stream.WriteUint32(OptimizerVersion);
            stream.WriteUint64(sectorFaces.SectorId);
            stream.WriteUint64(sectorFaces.ParentSectorId ?? ulong.MaxValue);

            stream.WriteVector3(sectorFaces.BboxMin);
            stream.WriteVector3(sectorFaces.BboxMax);
            if (sectorFaces.SectorContents is null)
            {
                stream.WriteUint32(0); // node count
            }
            else
            {
                stream.WriteUint32((uint)sectorFaces.SectorContents.Nodes.Length);
                stream.WriteUint32(sectorFaces.SectorContents.GridParameters.GridSizeX);
                stream.WriteUint32(sectorFaces.SectorContents.GridParameters.GridSizeY);
                stream.WriteUint32(sectorFaces.SectorContents.GridParameters.GridSizeZ);
                stream.WriteVector3(sectorFaces.SectorContents.GridParameters.GridOrigin);
                stream.WriteFloat(sectorFaces.SectorContents.GridParameters.GridIncrement);

                foreach (var node in sectorFaces.SectorContents.Nodes)
                {
                    WriteNode(node, stream);
                }
            }
        }

        private static void WriteNode(Node node, Stream stream)
        {
            var hasColorOnEachCell = node.CompressFlags.HasFlag(CompressFlags.HasColorOnEachCell);
            var indexAsUInt64 = node.CompressFlags.HasFlag(CompressFlags.IndexIsLong);

            stream.WriteUint64(node.NodeId);
            stream.WriteUint64(node.TreeIndex);
            stream.WriteUint32((uint)node.Faces.Length);
            stream.WriteByte((byte)node.CompressFlags);

            if (!hasColorOnEachCell)
            {
                stream.WriteColorRgb(node.Color!.Value);
            }

            foreach (var face in node.Faces)
            {
                WriteFace(face, stream, hasColorOnEachCell, indexAsUInt64);
            }
        }

        private static void WriteFace(Face face, Stream stream, bool hasColorOnEachCell, bool indexIsLong)
        {
            if (!indexIsLong && face.Index > int.MaxValue)
            {
                throw new InvalidOperationException($"Face.Index is set to UInt32 and the value is outside bounds. Value is {face.Index}");
            }

            var multipleFaces = face.FaceFlags.HasFlag(FaceFlags.Multiple);

            if (indexIsLong)
            {
                stream.WriteUint64(face.Index);
            }
            else
            {
                stream.WriteUint32((uint)face.Index);
            }
            stream.WriteByte((byte)face.FaceFlags);
            if (multipleFaces) stream.WriteByte(face.Repetitions);
            if (hasColorOnEachCell) stream.WriteColorRgb(face.Color!.Value);
        }
    }
}