namespace RvmSharp
{
    using Containers;
    using Primitives;
    using System;
    using System.IO;
    using System.Numerics;
    using System.Text;

    public static class RvmParser
    {
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

        private static Vector3 ReadVector3(Stream stream)
        {
            var x = ReadFloat(stream);
            var y = ReadFloat(stream);
            var z = ReadFloat(stream);
            return new Vector3(x, y, z);
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

        private static string ReadChunkHeader(Stream stream, out uint nextHeaderOffset, out uint dunno)
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
            nextHeaderOffset = ReadUint(stream);
            dunno = ReadUint(stream);
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
            var matrix = new Matrix4x4(ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 0,
                ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 0,
                ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 0,
                ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 1);

            var bBoxLocal = new RvmBoundingBox { Min = ReadVector3(stream), Max = ReadVector3(stream) };

            RvmPrimitive primitive = null;
            switch (kind)
            {
                case RvmPrimitiveKind.Pyramid:
                {
                    var bottomX = ReadFloat(stream);
                    var bottomY = ReadFloat(stream);
                    var topX = ReadFloat(stream);
                    var topY = ReadFloat(stream);
                    var offsetX = ReadFloat(stream);
                    var offsetY = ReadFloat(stream);
                    var height = ReadFloat(stream);
                    primitive = new RvmPyramid(version, matrix, bBoxLocal, bottomX, bottomY, topX, topY, offsetX,
                        offsetY, height);
                    break;
                }
                case RvmPrimitiveKind.Box:
                    var lengthX = ReadFloat(stream);
                    var lengthY = ReadFloat(stream);
                    var lengthZ = ReadFloat(stream);
                    primitive = new RvmBox(version, matrix, bBoxLocal, lengthX, lengthY, lengthZ);
                    break;
                case RvmPrimitiveKind.RectangularTorus:
                {
                    var radiusInner = ReadFloat(stream);
                    var radiusOuter = ReadFloat(stream);
                    var height = ReadFloat(stream);
                    var angle = ReadFloat(stream);
                    primitive = new RvmRectangularTorus(version, matrix, bBoxLocal, radiusInner, radiusOuter, height, angle);
                    break;
                }
                case RvmPrimitiveKind.CircularTorus:
                {
                    var offset = ReadFloat(stream);
                    var radius = ReadFloat(stream);
                    var angle = ReadFloat(stream);
                    primitive = new RvmCircularTorus(version, matrix, bBoxLocal, offset, radius, angle);
                    break;
                }
                case RvmPrimitiveKind.EllipticalDish:
                {
                    var baseRadius = ReadFloat(stream);
                    var height = ReadFloat(stream);
                    primitive = new RvmEllipticalDish(version, matrix, bBoxLocal, baseRadius, height);
                    break;
                }
                case RvmPrimitiveKind.SphericalDish:
                {
                    var baseRadius = ReadFloat(stream);
                    var height = ReadFloat(stream);
                    primitive = new RvmSphericalDish(version, matrix, bBoxLocal, baseRadius, height);
                    break;
                }
                case RvmPrimitiveKind.Snout:
                {
                    var radiusBottom = ReadFloat(stream);
                    var radiusTop = ReadFloat(stream);
                    var height = ReadFloat(stream);
                    var offsetX = ReadFloat(stream);
                    var offsetY = ReadFloat(stream);
                    var bottomShearX = ReadFloat(stream);
                    var bottomShearY = ReadFloat(stream);
                    var topShearX = ReadFloat(stream);
                    var topShearY = ReadFloat(stream);
                    primitive = new RvmSnout(version, matrix, bBoxLocal, radiusBottom, radiusTop, height,
                        offsetX, offsetY, bottomShearX, bottomShearY, topShearX, topShearY);
                    break;
                }
                case RvmPrimitiveKind.Cylinder:
                {
                    var radius = ReadFloat(stream);
                    var height = ReadFloat(stream);
                    primitive = new RvmCylinder(version, matrix, bBoxLocal, radius, height);
                    break;
                }
                case RvmPrimitiveKind.Sphere:
                    var diameter = ReadFloat(stream);
                    primitive = new RvmSphere(version, matrix, bBoxLocal, diameter);
                    break;
                case RvmPrimitiveKind.Line:
                    var a = ReadFloat(stream);
                    var b = ReadFloat(stream);
                    primitive = new RvmLine(version, matrix, bBoxLocal, a, b);
                    break;
                case RvmPrimitiveKind.FacetGroup:
                    var polygonCount = ReadUint(stream);
                    var polygons = new RvmFacetGroup.RvmPolygon[polygonCount];
                    for (var i = 0; i < polygonCount; i++)
                    {
                        var contourCount = ReadUint(stream);
                        var contours = new RvmFacetGroup.RvmContour[contourCount];

                        for (var k = 0; k < contourCount; k++)
                        {
                            var vertexCount = ReadUint(stream);
                            var vertices = new(Vector3 v, Vector3 n)[vertexCount];

                            for (var n = 0; n < vertexCount; n++)
                            {
                                var vertex = ReadVector3(stream);
                                var normal = ReadVector3(stream);
                                vertices[n] = (vertex, normal);
                            }

                            contours[k] = new RvmFacetGroup.RvmContour(vertices);
                        }

                        polygons[i] = new RvmFacetGroup.RvmPolygon(contours);
                    }

                    primitive = new RvmFacetGroup(version, matrix, bBoxLocal, polygons);
                    break;
            }
            return primitive;
            // transform bb to world?
            }

            public static RvmNode ReadCntb(Stream stream)
        {
            var version = ReadUint(stream);
            var name = ReadString(stream);
            const float mmToM = 0.001f;
            var translation = new Vector3(ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM);
            var materialId = ReadUint(stream);
            if (version == 3)
            {
                // FIXME: On version 3 there is an unknown value
                ReadUint(stream);
            }
                
            var group = new RvmNode(version, name, translation, materialId);
            uint nextHeaderOffset, dunno;
            var id = ReadChunkHeader(stream, out nextHeaderOffset, out dunno);
            while (id != "CNTE")
            {
                switch (id)
                {
                    case "CNTB":
                        group.AddChild(ReadCntb(stream));
                        break;
                    case "PRIM":
                        group.AddChild(ReadPrimitive(stream));
                        break;
                    default:
                        throw new NotImplementedException($"Unknown chunk: {id}");
                }
                id = ReadChunkHeader(stream, out nextHeaderOffset, out dunno);
            }
            if (id == "CNTE")
            {
                var endVersion = ReadUint(stream);
            }
            return group;
        }

        private static RvmColor ReadColor(Stream stream)
        {
            var colorKind = ReadUint(stream);
            var colorIndex = ReadUint(stream);
            var color = ReadUint(stream);
            byte[] rgb = {
                (byte)((color) >> 24 & 0xff),
                (byte)((color) >> 16 & 0xff),
                (byte)((color) >> 8 & 0xff),
            };
            return new RvmColor(colorKind, colorIndex, rgb);
        }

        private static RvmFile ReadHead(Stream stream)
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
