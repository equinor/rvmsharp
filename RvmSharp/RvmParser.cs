﻿namespace RvmSharp
{
    using Containers;
    using Operations;
    using Primitives;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text;

    public static class RvmParser
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReadUint(Stream stream)
        {
            Span<byte> bytes = stackalloc byte[4];
            if (stream.Read(bytes) != bytes.Length)
                throw new IOException("Unexpected end of stream");
            if (BitConverter.IsLittleEndian)
                bytes.Reverse();
            return BitConverter.ToUInt32(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ReadFloat(Stream stream)
        {
            Span<byte> bytes = stackalloc byte[4];
            if (stream.Read(bytes) != bytes.Length)
                throw new IOException("Unexpected end of stream");
            if (BitConverter.IsLittleEndian)
                bytes.Reverse();
            return BitConverter.ToSingle(bytes);
        }

        private static string ReadChunkHeader(Stream stream, out uint nextHeaderOffset, out uint dunno)
        {
            var builder = new StringBuilder();
            Span<byte> bytes = stackalloc byte[4];
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

        private static (uint version, string project, string name) ReadModelParameters(Stream stream)
        {
            var version = ReadUint(stream);
            var project = ReadString(stream);
            var name = ReadString(stream);
            return (version, project, name);
        }

        private static RvmPrimitive ReadPrimitive(Stream stream, RvmReadOptions options)
        {
            var version = ReadUint(stream);
            var kind = (RvmPrimitiveKind)ReadUint(stream);
            var matrix = new Matrix4x4(ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 0,
                ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 0,
                ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 0,
                ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), 1);

            var bBoxLocal = new RvmBoundingBox(Min: ReadVector3(stream), Max: ReadVector3(stream));

            RvmPrimitive primitive;
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

                        if (options.PyramidToBox &&
                            MathF.Abs(bottomX - topX) < options.OptimizationTolerance &&
                            MathF.Abs(bottomY - topY) < options.OptimizationTolerance &&
                            offsetX == 0 && offsetY == 0)
                        {
                            primitive = new RvmBox(version, matrix, bBoxLocal, bottomX, bottomY, height);
                        } else
                        {
                            primitive = new RvmPyramid(version, matrix, bBoxLocal, bottomX, bottomY, topX, topY,
                                offsetX,
                                offsetY, height);
                        }
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
                        primitive = new RvmRectangularTorus(version, matrix, bBoxLocal, radiusInner, radiusOuter,
                            height,
                            angle);
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
                        if (options.SnoutToCylinder &&
                            Math.Abs(radiusBottom - radiusTop) < options.OptimizationTolerance &&
                            bottomShearX == 0 && bottomShearY == 0 &&
                            topShearX == 0 && topShearY == 0 &&
                            offsetX == 0 && offsetY == 0)
                        {
                                primitive = new RvmCylinder(version, matrix, bBoxLocal, radiusBottom, height);
                        }
                        else
                        {
                            primitive = new RvmSnout(version, matrix, bBoxLocal, radiusBottom, radiusTop, height,
                                offsetX, offsetY, bottomShearX, bottomShearY, topShearX, topShearY);
                        }
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
                            var vertices = new (Vector3 Vertex, Vector3 Normal)[vertexCount];

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

                    // We order the polygons here so that we can have a better match rate when doing facet group matching.
                    // This simple change can improve facet matching depending on 3D model (~10% for Huldra), while the output is visually equal.

                    var polygonsOrdered = polygons
                        .OrderBy(p => p.Contours.Length) // OrderBy uses a stable sorting algorithm which preserves original ordering upon ordering equals
                        .ThenBy(p => p.Contours.Sum(c => c.Vertices.Length))
                        .ToArray();
                    var facetGroup = new RvmFacetGroup(version, matrix, bBoxLocal, polygonsOrdered);
                    if (options.FacetGroupToBox && BoxDetector.IsBox(facetGroup, out var box))
                    {
                        primitive = box;
                    }
                    else
                    {
                        primitive = facetGroup;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unexpected Kind");
            }

            return primitive;
            // transform bb to world?
        }

        private static RvmNode ReadCntb(Stream stream, RvmReadOptions options)
        {
            var version = ReadUint(stream);
            var name = ReadString(stream);

            const float mmToM = 0.001f;
            var translation = new Vector3(
                ReadFloat(stream) * mmToM,
                ReadFloat(stream) * mmToM,
                ReadFloat(stream) * mmToM);

            var materialId = ReadUint(stream);
            if (version == 3)
            {
                // FIXME: On version 3 there is an unknown value
                ReadUint(stream);
            }

            var group = new RvmNode(version, name, translation, materialId);

            uint unusedNextHeaderOffset;
            uint dontKnowWhatThisValueDoes;

            var id = ReadChunkHeader(stream, out unusedNextHeaderOffset, out dontKnowWhatThisValueDoes);
            while (id != "CNTE")
            {
                switch (id)
                {
                    case "CNTB":
                        group.AddChild(ReadCntb(stream, options));
                        break;
                    case "PRIM":
                        group.AddChild(ReadPrimitive(stream, options));
                        break;
                    default:
                        throw new NotImplementedException($"Unknown chunk: {id}");
                }

                id = ReadChunkHeader(stream, out unusedNextHeaderOffset, out dontKnowWhatThisValueDoes);
            }

            if (id == "CNTE")
            {
                // ReSharper disable once UnusedVariable
                var endVersion = ReadUint(stream);
            }

            return group;
        }

        private static RvmColor ReadColor(Stream stream)
        {
            var colorKind = ReadUint(stream);
            var colorIndex = ReadUint(stream);
            var color = ReadUint(stream);
            // TODO: Is it correct to discard the alpha byte of the color?
            var rgb = ((byte)((color) >> 24 & 0xff), (byte)((color) >> 16 & 0xff), (byte)((color) >> 8 & 0xff));
            return new RvmColor(colorKind, colorIndex, rgb);
        }

        private static RvmFile.RvmHeader ReadHead(Stream stream)
        {
            var version = ReadUint(stream);
            var info = ReadString(stream);
            var note = ReadString(stream);
            var date = ReadString(stream);
            var user = ReadString(stream);
            var encoding = (version >= 2) ? ReadString(stream) : "";

            return new RvmFile.RvmHeader(version, info, note, date, user, encoding);
        }

        public static RvmFile ReadRvm(Stream stream, RvmReadOptions? options = null)
        {
            options ??= new RvmReadOptions();
            uint len, dunno;

            var head = ReadChunkHeader(stream, out len, out dunno);
            if (head != "HEAD")
                throw new IOException($"Expected HEAD, found {head}");
            var header = ReadHead(stream);
            var modl = ReadChunkHeader(stream, out len, out dunno);
            if (modl != "MODL")
                throw new IOException($"Expected MODL, found {modl}");
            var modelParameters = ReadModelParameters(stream);
            var modelChildren = new List<RvmNode>();
            var modelPrimitives = new List<RvmPrimitive>();
            var modelColors = new List<RvmColor>();

            var chunk = ReadChunkHeader(stream, out len, out dunno);
            while (chunk != "END:")
            {
                switch (chunk)
                {
                    case "CNTB":
                        modelChildren.Add(ReadCntb(stream, options));
                        break;
                    case "PRIM":
                        modelPrimitives.Add(ReadPrimitive(stream, options));
                        break;
                    case "COLR":
                        modelColors.Add(ReadColor(stream));
                        break;
                    default:
                        throw new NotImplementedException($"Unknown chunk: {chunk}");
                }

                chunk = ReadChunkHeader(stream, out len, out dunno);
            }

            return new RvmFile(header,
                new RvmModel(modelParameters.version, modelParameters.project, modelParameters.name,
                    modelChildren, modelPrimitives, modelColors));
        }
    }
}