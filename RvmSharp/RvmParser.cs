namespace RvmSharp;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Containers;
using Operations;
using Primitives;
#if DOTNET7_0_OR_GREATER
#else
using System;
// ReSharper disable once RedundantUsingDirective -- Used for StreamExtensions.ReadExactly
using Utils;
#endif

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

    /// <summary>
    /// Reads a double stored as two big-endian 32-bit words, low word first.
    /// </summary>
    private static double ReadWordSwappedDouble(Stream stream)
    {
        ulong lowWord = ReadUint(stream);
        ulong highWord = ReadUint(stream);
        return BitConverter.Int64BitsToDouble((long)(highWord << 32 | lowWord));
    }

    /// <summary>
    /// Reads three word-swapped doubles in millimetres and converts them to metres before rounding to float.
    /// </summary>
    private static Vector3 ReadDoubleTranslationInMeters(Stream stream)
    {
        const double mmToM = 0.001;
        return new Vector3(
            (float)(ReadWordSwappedDouble(stream) * mmToM),
            (float)(ReadWordSwappedDouble(stream) * mmToM),
            (float)(ReadWordSwappedDouble(stream) * mmToM)
        );
    }

    private static string ReadChunkHeader(
        Stream stream,
        string nameForDebugging,
        out uint nextHeaderOffset,
        out uint dunno
    )
    {
        var builder = new StringBuilder();
        Span<byte> bytes = stackalloc byte[4];
        for (int i = 0; i < 4; i++)
        {
            var read = stream.Read(bytes);
            if (read < bytes.Length)
                throw new IOException("Unexpected end of stream");
            if (bytes[0] != 0 || bytes[1] != 0 || bytes[2] != 0)
                throw new InvalidDataException(
                    $"Unexpected data in header at position {("0x" + (stream.Position - read).ToString("x8"))} when parsing node \"{nameForDebugging}\""
                );
            builder.Append((char)bytes[3]);
        }

        nextHeaderOffset = ReadUint(stream);
        dunno = ReadUint(stream);
        return builder.ToString();
    }

    /// <summary>
    /// Fails when a chunk was not read exactly up to the next chunk header, which means the chunk format is not
    /// fully understood. Without this check the parser misreads the following bytes and fails later with a
    /// misleading error.
    /// </summary>
    private static void EnsureChunkEnd(Stream stream, uint nextHeaderOffset, string chunkId, string nameForDebugging)
    {
        var position = stream.Position;
        if (position == nextHeaderOffset)
            return;

        var difference = nextHeaderOffset - position;
        var problem =
            difference > 0
                ? $"{difference} bytes were left unread"
                : $"{-difference} bytes past the next chunk header were read";
        throw new InvalidDataException(
            $"{chunkId} chunk in \"{nameForDebugging}\" ended at 0x{position:x8}, but the next chunk header is at 0x{nextHeaderOffset:x8}: {problem}."
        );
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

    private static RvmPrimitive ReadPrimitive(Stream stream, bool hasOpacity = false)
    {
        var version = ReadUint(stream);
        var kind = (RvmPrimitiveKind)ReadUint(stream);

        // some chunk types as obstruction or insulation have transparency
        // we need to treat them in order to process the rest of the stream,
        // but the opacity itself is not used in any way right now
        if (hasOpacity)
        {
            // ReSharper disable once UnusedVariable --
            var unusedOpacity = ReadUint(stream);
        }
        // csharpier-ignore -- Keep matrix formatting
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
                primitive = new RvmPyramid(
                    version,
                    matrix,
                    bBoxLocal,
                    bottomX,
                    bottomY,
                    topX,
                    topY,
                    offsetX,
                    offsetY,
                    height
                );
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
                primitive = new RvmRectangularTorus(
                    version,
                    matrix,
                    bBoxLocal,
                    radiusInner,
                    radiusOuter,
                    height,
                    angle
                );
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
                primitive = new RvmSnout(
                    version,
                    matrix,
                    bBoxLocal,
                    radiusBottom,
                    radiusTop,
                    height,
                    offsetX,
                    offsetY,
                    bottomShearX,
                    bottomShearY,
                    topShearX,
                    topShearY
                );
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
                primitive = new RvmFacetGroup(version, matrix, bBoxLocal, ReadFacetGroupPolygons(stream));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unexpected Kind");
        }

        return primitive;
        // transform bb to world?
    }

    private static RvmFacetGroup.RvmPolygon[] ReadFacetGroupPolygons(Stream stream)
    {
        // Reads the polygons, contours, and vertices for a facet group from the stream into an optimized
        // structure suitable for efficient access and shared buffers.

        var polygonCount = ReadUint(stream);
        var polygonRanges = new (int FirstContour, int ContourCount)[polygonCount];

        // Counts are interleaved with vertex data, so we need a list instead of preallocating arrays.
        var contourRanges = new List<(int FirstVertex, int VertexCount)>();
        var vertices = new List<(Vector3 Vertex, Vector3 Normal)>();
        for (var polygonIndex = 0; polygonIndex < polygonCount; polygonIndex++)
        {
            var contourCount = checked((int)ReadUint(stream));
            polygonRanges[polygonIndex] = (contourRanges.Count, contourCount);

            for (var contourIndex = 0; contourIndex < contourCount; contourIndex++)
            {
                var vertexCount = checked((int)ReadUint(stream));
                contourRanges.Add((vertices.Count, vertexCount));
                for (var vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    var vertex = ReadVector3(stream);
                    var normal = ReadVector3(stream);
                    vertices.Add((vertex, normal));
                }
            }
        }

        var vertexBuffer = vertices.ToArray();
        var contours = contourRanges
            .Select(range => new RvmFacetGroup.RvmContour(
                new ArraySegment<(Vector3 Vertex, Vector3 Normal)>(vertexBuffer, range.FirstVertex, range.VertexCount)
            ))
            .ToArray();

        // Sorting improves facet matching (~10% for Huldra) without changing the geometry.
        // OrderBy/ThenBy preserve file order for ties. Only polygon views move; contour order and holes stay intact.
        return polygonRanges
            .Select(range => new RvmFacetGroup.RvmPolygon(
                new ArraySegment<RvmFacetGroup.RvmContour>(contours, range.FirstContour, range.ContourCount)
            ))
            .OrderBy(polygon => polygon.Contours.Count)
            .ThenBy(polygon => polygon.Contours.Sum(contour => contour.Vertices.Count))
            .ToArray();
    }

    private static RvmNode ReadCntb(Stream stream, uint cntbNextHeaderOffset)
    {
        var version = ReadUint(stream);
        GuardKnownVersion(version);
        var name = ReadString(stream);

        const float mmToM = 0.001f;
        var translation = new Vector3(ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM);

        var materialId = ReadUint(stream);
        if (version is 3 or 4)
        {
            // In v3 "_insuobst" exports these bytes are 57 02 00 00 regardless of the node's OBST value.
            // We believe byte 1 is transparency-related and 2 is some other rendering flag. 3 and 4 are always 0 in the seen data.
            // Consume them without interpreting them in v3; v4 expects zero before its double translation.
            Span<byte> bytes = stackalloc byte[4];
            stream.ReadExactly(bytes);
            // We just consume these bytes without interpreting them for now.

            // V4 nodes have a trailing double-precision translation after these 4 bytes.
            if (version == 4)
            {
                // Note: v4 nodes seem to indicate the node is very far from the origin, hence the need for double-precision translation values.
                // This is not really supported in most float32 based viewers (including Echo), and is 99.999% of the time a 3D modelling error, not a genuine need for double-precision translation.
                // We can consider excluding all v4 primitive nodes in the future.

                // Convert to metres before rounding to float to preserve precision for large coordinates.
                translation = ReadDoubleTranslationInMeters(stream);
            }
        }

        // The CNTB offset points to the first child chunk, not past the matching CNTE.
        EnsureChunkEnd(stream, cntbNextHeaderOffset, "CNTB", name);

        var group = new RvmNode(version, name, translation, materialId);

        uint nextHeaderOffset;
        // ReSharper disable once NotAccessedOutParameterVariable -- This value is always 1 in all known data.
        uint dontKnowWhatThisValueDoes;

        var id = ReadChunkHeader(stream, name, out nextHeaderOffset, out dontKnowWhatThisValueDoes);
        while (id != "CNTE")
        {
            switch (id)
            {
                case "CNTB":
                    group.AddChild(ReadCntb(stream, nextHeaderOffset));
                    break;
                case "PRIM":
                    var primitive = ReadPrimitive(stream);
                    if (version == 4)
                        primitive = ReadVersion4PrimitiveTrailer(stream, primitive, nextHeaderOffset);
                    EnsureChunkEnd(stream, nextHeaderOffset, id, name);

                    if (Matrix4x4Helpers.MatrixContainsInfiniteValue(primitive.Matrix))
                    {
                        // This handles an issue on Oseberg where some models contained infite values. Not seen elsewhere.
                        Console.WriteLine(
                            "Invalid matrix found for primitive of " + name + ". " + " Discarding this primitive"
                        );
                    }
                    else
                    {
                        group.AddChild(primitive);
                    }

                    break;
                // types OBST (obstacle) and INSU (insulation) chunks were previously also throwing
                // a NotImplementedException causing the building process to terminate
                // now the corresponding stream is consumed, but they are skipped (not added to the scene)
                //
                // in order to add them to the scene, the primitive type needs to be extended as follows:
                // -- add a field for storing the type of the chunk (primitive, obstacle, insulation)
                // -- add a field for storing opacity if this should be supported by rendering
                case "OBST":
                    ReadPrimitive(stream, true);
                    EnsureChunkEnd(stream, nextHeaderOffset, id, name);
                    Console.WriteLine("Encountered OBST chunk, skipping.");
                    break;
                case "INSU":
                    ReadPrimitive(stream, true);
                    EnsureChunkEnd(stream, nextHeaderOffset, id, name);
                    Console.WriteLine("Encountered INSU chunk, skipping.");
                    break;
                default:
                    throw new NotImplementedException($"Unknown chunk: {id}");
            }

            id = ReadChunkHeader(stream, name, out nextHeaderOffset, out dontKnowWhatThisValueDoes);
        }

        // ReSharper disable once UnusedVariable
        var endVersion = ReadUint(stream);
        EnsureChunkEnd(stream, nextHeaderOffset, id, name);

        return group;
    }

    /// <summary>
    /// Some primitives in v4 nodes end with a double-precision copy of the matrix translation, in millimetres, but
    /// files with v4 nodes have also been parsed without it. The chunk header's absolute next-chunk offset tells
    /// whether the trailer is present; any other leftover bytes are reported by <see cref="EnsureChunkEnd"/>.
    /// OBST and INSU chunks in v4 nodes have not been observed, so they are not handled here.
    /// </summary>
    private static RvmPrimitive ReadVersion4PrimitiveTrailer(
        Stream stream,
        RvmPrimitive primitive,
        uint nextHeaderOffset
    )
    {
        const int trailerSize = 3 * sizeof(double);
        if (nextHeaderOffset - stream.Position != trailerSize)
            return primitive;

        var matrix = primitive.Matrix;
        matrix.Translation = ReadDoubleTranslationInMeters(stream);
        return primitive with { Matrix = matrix };
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

    /// <summary>
    /// Reads an RVM file. The stream must be seekable, because chunk boundaries are validated against the
    /// absolute offsets in the chunk headers.
    /// </summary>
    public static RvmFile ReadRvm(Stream stream)
    {
        // Chunk-boundary validation needs Position, but the parser does not seek. If seeking is needed we can make the check optional.
        if (!stream.CanSeek)
            throw new ArgumentException("RVM parsing requires a seekable stream.", nameof(stream));

        uint nextHeaderOffset;

        // ReSharper disable once NotAccessedOutParameterVariable -- Observed as 1 in all sampled files. Unknown use.
        uint dontKnowWhatThisRepresents;

        var head = ReadChunkHeader(stream, "HEADER", out nextHeaderOffset, out dontKnowWhatThisRepresents);
        if (head != "HEAD")
            throw new IOException($"Expected HEAD, found {head}");
        var header = ReadHead(stream);
        EnsureChunkEnd(stream, nextHeaderOffset, head, "file header");
        var modl = ReadChunkHeader(stream, "MODL", out nextHeaderOffset, out dontKnowWhatThisRepresents);
        if (modl != "MODL")
            throw new IOException($"Expected MODL, found {modl}");
        var modelParameters = ReadModelParameters(stream);
        EnsureChunkEnd(stream, nextHeaderOffset, modl, modelParameters.name);
        var modelChildren = new List<RvmNode>();
        var modelPrimitives = new List<RvmPrimitive>();
        var modelColors = new List<RvmColor>();

        var chunk = ReadChunkHeader(stream, "CHUNK HEADER", out nextHeaderOffset, out dontKnowWhatThisRepresents);
        while (chunk != "END:")
        {
            switch (chunk)
            {
                case "CNTB":
                    modelChildren.Add(ReadCntb(stream, nextHeaderOffset));
                    break;
                case "PRIM":
                    modelPrimitives.Add(ReadPrimitive(stream));
                    EnsureChunkEnd(stream, nextHeaderOffset, chunk, modelParameters.name);
                    break;
                case "COLR":
                    modelColors.Add(ReadColor(stream));
                    EnsureChunkEnd(stream, nextHeaderOffset, chunk, modelParameters.name);
                    break;
                default:
                    throw new NotImplementedException($"Unknown chunk: {chunk}");
            }

            chunk = ReadChunkHeader(stream, "CHUNK", out nextHeaderOffset, out dontKnowWhatThisRepresents);
        }

        return new RvmFile(
            header,
            new RvmModel(
                modelParameters.version,
                modelParameters.project,
                modelParameters.name,
                modelChildren,
                modelPrimitives,
                modelColors
            )
        );
    }

    /// <summary>
    /// Ensure the input version is known. If not throw an error!
    /// This is done to avoid parsing unknown versions without handling them explicitly
    /// </summary>
    private static void GuardKnownVersion(uint version)
    {
        switch (version)
        {
            case 1 or 2 or 3 or 4:
                return;
            default:
                throw new Exception(
                    "Got node with version "
                        + version
                        + ". This is untested. Edit the C# code to allow this node, and update the C# code when the version is handled ok!"
                );
        }
    }
}
