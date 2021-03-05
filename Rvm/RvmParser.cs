using System;
using System.IO;
using System.Text;

namespace rvmsharp.Rvm
{
    public static class RvmParser
    {
        private static byte ReadByte(Stream stream)
        {
            var value = stream.ReadByte();
            if (value == -1)
                throw new IOException("Unexpected end of stream");
            return (byte)value;
        }

        private static uint ReadUint(Stream stream)
        {
            var bytes = new byte[4];
            if (stream.Read(bytes) != bytes.Length)
                throw new IOException("Unexpected end of stream");
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes);
        }

        private static float ReadFloat(Stream stream)
        {
            var bytes = new byte[4];
            if (stream.Read(bytes) != bytes.Length)
                throw new IOException("Unexpected end of stream");
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes);
        }

        private static string ReadString(Stream stream)
        {
            var length = 4 * ReadUint(stream);
            var bytes = new byte[length];
            var read = stream.Read(bytes, 0, (int)length);
            if (read < length)
                throw new IOException($"Unexpected end of stream, expected {length} bytes, but got {read}");
            int end;
            for (end = 0; end < bytes.Length; end++)
            {
                if (bytes[end] == 0)
                    break;
            }
            return Encoding.UTF8.GetString(bytes, 0, end);
        }

        private static string ReadChunkHeader(Stream stream, out uint len, out uint dunno)
        {
            var builder = new StringBuilder();
            var bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                var read = stream.Read(bytes);
                if (read < bytes.Length)
                    throw new IOException("Unexpected end of stream");
                if (bytes[0] != 0 || bytes[1] != 0 || bytes[2] != 0)
                    throw new IOException("Unexpected data in header");
                builder.Append((char)bytes[3]);
            }
            len = 0;
            dunno = 0;
            
            try
            {
                if (stream.Position != stream.Length)
                {
                    len = ReadUint(stream);
                    dunno = ReadUint(stream);
                }
            } catch (IOException)
            {
                // can we ignore EOF?
                throw; // let's throw for now
            }
            return builder.ToString();
        }

        private static RvmModel ReadModel(Stream stream)
        {
            var version = ReadUint(stream);
            var project = ReadString(stream);
            var name = ReadString(stream);
            return new RvmModel(version, project, name);
        }

        private static RvmPrimitive ReadPrimitive(Stream stream)
        {
            var version = ReadUint(stream);
            var kind = (RvmPrimitiveKind)ReadUint(stream);
            var matrix = new float[3, 4];
            var bBoxLocal = new float[2, 3];

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    matrix[row, column] = ReadFloat(stream);
                }
            }

            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    bBoxLocal[row, column] = ReadFloat(stream);
                }
            }
            RvmPrimitive primitive = null;
            switch (kind)
            {
                case RvmPrimitiveKind.Pyramid:
                    
                    var bottomX = ReadFloat(stream);
                    var bottomY = ReadFloat(stream);
                    var topX = ReadFloat(stream);
                    var topY = ReadFloat(stream);
                    var offsetX = ReadFloat(stream);
                    var offsetY = ReadFloat(stream);
                    var height = ReadFloat(stream);
                    primitive = new RvmPyramid(version, kind, matrix, bBoxLocal, bottomX, bottomY, topX, topY, offsetX, offsetY, height);
                    break;
                case RvmPrimitiveKind.Box:
                    var lengthX = ReadFloat(stream);
                    var lengthY = ReadFloat(stream);
                    var lengthZ = ReadFloat(stream);
                    primitive = new RvmBox(version, kind, matrix, bBoxLocal, lengthX, lengthY, lengthZ);
                    break;
                case RvmPrimitiveKind.RectangularTorus:
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    /*
                     * g->kind = Geometry::Kind::RectangularTorus;
                    p = read_float32_be(g->rectangularTorus.inner_radius, p, e);
                    p = read_float32_be(g->rectangularTorus.outer_radius, p, e);
                    p = read_float32_be(g->rectangularTorus.height, p, e);
                    p = read_float32_be(g->rectangularTorus.angle, p, e);
                    break;*/
                    break;
                case RvmPrimitiveKind.CircularTorus:
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    /* g->kind = Geometry::Kind::CircularTorus;
                    p = read_float32_be(g->circularTorus.offset, p, e);
                    p = read_float32_be(g->circularTorus.radius, p, e);
                    p = read_float32_be(g->circularTorus.angle, p, e);
                    break;*/
                    break;
                case RvmPrimitiveKind.EllipticalDish:
                    ReadFloat(stream);
                    ReadFloat(stream);
                    /* g->kind = Geometry::Kind::EllipticalDish;
                    p = read_float32_be(g->ellipticalDish.baseRadius, p, e);
                    p = read_float32_be(g->ellipticalDish.height, p, e);
                    break;*/
                    break;
                case RvmPrimitiveKind.SphericalDish:
                    ReadFloat(stream);
                    ReadFloat(stream);
                    /*g->kind = Geometry::Kind::SphericalDish;
                    p = read_float32_be(g->sphericalDish.baseRadius, p, e);
                    p = read_float32_be(g->sphericalDish.height, p, e);
                    break;*/
                    break;
                case RvmPrimitiveKind.Snout:
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    ReadFloat(stream);
                    /*g->kind = Geometry::Kind::Snout;
                    p = read_float32_be(g->snout.radius_b, p, e);
                    p = read_float32_be(g->snout.radius_t, p, e);
                    p = read_float32_be(g->snout.height, p, e);
                    p = read_float32_be(g->snout.offset[0], p, e);
                    p = read_float32_be(g->snout.offset[1], p, e);
                    p = read_float32_be(g->snout.bshear[0], p, e);
                    p = read_float32_be(g->snout.bshear[1], p, e);
                    p = read_float32_be(g->snout.tshear[0], p, e);
                    p = read_float32_be(g->snout.tshear[1], p, e);*/
                    break;
                case RvmPrimitiveKind.Cylinder:
                    ReadFloat(stream);
                    ReadFloat(stream);
                    /* g->kind = Geometry::Kind::Cylinder;
                    p = read_float32_be(g->cylinder.radius, p, e);
                    p = read_float32_be(g->cylinder.height, p, e);
                    break;*/
                    break;
                case RvmPrimitiveKind.Sphere:
                    ReadFloat(stream);
                    break;
                case RvmPrimitiveKind.Line:
                    ReadFloat(stream);
                    ReadFloat(stream);
                    break;
                case RvmPrimitiveKind.FacetGroup:
                    var polygonCount = ReadUint(stream);
                    for (var i = 0; i < polygonCount; i++)
                    {
                        var contourCount = ReadUint(stream);

                        for (var k = 0; k < contourCount; k++)
                        {
                            var vertexCount = ReadUint(stream);

                            for (var n = 0; n < vertexCount; n++)
                            {
                                var vertex = new[] { ReadFloat(stream), ReadFloat(stream), ReadFloat(stream) };
                                var normal = new[] { ReadFloat(stream), ReadFloat(stream), ReadFloat(stream) };
                            }
                        }
                    }
                    break;
            }
            return primitive;
            // transform bb to world?
            /*
                    
                    break;

                case 8:
                   

                case 9:
                    g->kind = Geometry::Kind::Sphere;
                    p = read_float32_be(g->sphere.diameter, p, e);
                    break;

                case 10:
                    g->kind = Geometry::Kind::Line;
                    p = read_float32_be(g->line.a, p, e);
                    p = read_float32_be(g->line.b, p, e);
                    break;

                case 11:
                    g->kind = Geometry::Kind::FacetGroup;

                    
                    break;

                default:
                    snprintf(ctx->buf, ctx->buf_size, "In PRIM, unknown primitive kind %d", kind);
                    ctx->store->setErrorString(ctx->buf);
                    return nullptr;
            }
            return p;*/
            }

            public static RvmGroup ReadCntb(Stream stream)
        {
            var version = ReadUint(stream);
            var name = ReadString(stream);
            var group = new RvmGroup(version, name);
            const float mmToM = 0.001f;
            var translation = new[] { ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM };
            var materialId = ReadUint(stream);
            uint len, dunno;
            var id = ReadChunkHeader(stream, out len, out dunno);
            while (id != "CNTE")
            {
                switch (id)
                {
                    case "CNTB":
                        group.AddChild(ReadCntb(stream));
                        break;
                    case "PRIM":
                        group.AddPrimitive(ReadPrimitive(stream));
                        break;
                    default:
                        throw new NotImplementedException($"Unknown chunk: {id}");
                }
                id = ReadChunkHeader(stream, out len, out dunno);
            }
            if (id == "CNTE")
            {
                var endVersion = ReadUint(stream);
            }
            return group;
        }

        public static RvmColor ReadColor(Stream stream)
        {
            var colorKind = ReadUint(stream);
            var colorIndex = ReadUint(stream);
            byte[] rgb = new[] { ReadByte(stream), ReadByte(stream), ReadByte(stream) };
            return new RvmColor(colorKind, colorIndex, rgb);
        }

        public static RvmFile ReadHead(Stream stream)
        {
            var version = ReadUint(stream);
            var info = ReadString(stream);
            var note = ReadString(stream);
            var date = ReadString(stream);
            var user = ReadString(stream);
            var encoding = (version >= 2) ? ReadString(stream) : "";
            return new RvmFile(version, info, note, date, user, encoding);
        }

        public static RvmFile ReadRvm(Stream stream)
        {
            uint len, dunno;
            var head = ReadChunkHeader(stream, out len, out dunno);
            if (head != "HEAD")
                throw new IOException($"Expected HEAD, found {head}");
            var file = ReadHead(stream);
            var modl = ReadChunkHeader(stream, out len, out dunno);
            if (modl != "MODL")
                throw new IOException($"Expected MODL, found {modl}");
            var model = ReadModel(stream);
            file.Model = model;

            var chunk = ReadChunkHeader(stream, out len, out dunno);
            while (chunk != "END:")
            {
                switch (chunk)
                {
                    case "CNTB":
                        model.AddChild(ReadCntb(stream));
                        break;
                    case "PRIM":
                        model.AddPrimitive(ReadPrimitive(stream));
                        break;
                    case "COLR":
                        model.AddColor(ReadColor(stream));
                        break;
                    default:
                        throw new NotImplementedException($"Unknown chunk: {chunk}");
                }
                chunk = ReadChunkHeader(stream, out len, out dunno);
            }
            return file;
        }
    }
}
