namespace RvmSharp;

using System;
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
using Utils;
#endif

public static class RvmParser
{
    /// <summary>
    /// These are the known bytes we have seen in v4 nodes. We have no documentation on what the data represents.
    /// Add docs to the code if you find out what the data represents.
    /// </summary>
    // csharpier-ignore -- Dont reformat
    private static readonly byte[][] KnownV4Bytes = new byte[][]{
        [0x00,0x00,0x00,0x00,0x78,0xB5,0x8C,0x52,0x44,0x15,0xAF,0x1D,0x78,0xB5,0x8C,0x46,0x44,0x15,0xAF,0x1D,0x78,0xB5,0x8C,0x61,0x44,0x15,0xAF,0x1D],
        [0x00,0x00,0x00,0x00,0x45,0x6D,0x20,0x76,0x42,0xBB,0x77,0xCF,0x50,0x49,0xB8,0x02,0x40,0xFF,0xFB,0x4C,0xFF,0xDB,0x9D,0xB8,0x40,0xDA,0x01,0x9B],
        [0x00,0x00,0x00,0x00,0x2F,0x1E,0x55,0x56,0x40,0xF7,0x81,0x05,0xC5,0x0A,0x09,0x8F,0x43,0x43,0x09,0xDF,0xFF,0x9C,0xB1,0x9D,0x40,0xDC,0xE2,0xD5],
        [0x00,0x00,0x00,0x00,0x48,0xD5,0x3D,0xA5,0x42,0xBB,0x79,0xAE,0xE6,0x02,0x39,0x76,0x40,0xF7,0x63,0xA6,0xEB,0x56,0xBD,0xD3,0x40,0xDA,0x01,0x39],
        [0x00,0x00,0x00,0x00,0x2C,0x9A,0xEB,0x9A,0x42,0xBE,0x90,0xD6,0x04,0x18,0x93,0x74,0x40,0xF7,0x34,0xE0,0xC6,0x69,0x8E,0x42,0x40,0xD9,0x5F,0x3B]};

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
                primitive = new RvmFacetGroup(version, matrix, bBoxLocal, polygonsOrdered);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unexpected Kind");
        }

        return primitive;
        // transform bb to world?
    }

    private static RvmNode ReadCntb(Stream stream)
    {
        var version = ReadUint(stream);
        GuardKnownVersion(version);
        var name = ReadString(stream);

        const float mmToM = 0.001f;
        var translation = new Vector3(ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM, ReadFloat(stream) * mmToM);

        var materialId = ReadUint(stream);
        if (version == 3)
        {
            Span<byte> bytes = stackalloc byte[4];
            stream.ReadExactly(bytes);
            // Ignore the bytes for now.
            // Byte 1 may be transparency
            // byte 2 may be a type identifier
            // Byte 3 and 4 are always 0 in the known data with version 3
        }
        else if (version == 4)
        {
            Console.WriteLine("--- Node with name " + name + " was a v4 node"); // TODO: Remove me when we have more controll of the v4 nodes. Currently may be related to "ARCHIV 1, but not known"
            Span<byte> bytes = stackalloc byte[4 * 7];
            stream.ReadExactly(bytes);

            var bytesArray = bytes.ToArray();

            if (!IsKnownVersion4Node(bytesArray))
            {
                Console.Error.WriteLine(
                    "New bytes in version 4 cntb. Was: "
                        + String.Join(",", bytesArray.Select(x => "0x" + x.ToString("X")).ToArray())
                        + "\n Please update the C# code to allow the new values. This error only exists to try to decipher the values in the v4 format."
                );
            }
        }

        var group = new RvmNode(version, name, translation, materialId);

        uint unusedNextHeaderOffset;
        uint dontKnowWhatThisValueDoes;

        var id = ReadChunkHeader(stream, name, out unusedNextHeaderOffset, out dontKnowWhatThisValueDoes);
        while (id != "CNTE")
        {
            switch (id)
            {
                case "CNTB":
                    group.AddChild(ReadCntb(stream));
                    break;
                case "PRIM":
                    var primitive = ReadPrimitive(stream);
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
                    Console.WriteLine("Encountered OBST chunk, skipping.");
                    break;
                case "INSU":
                    ReadPrimitive(stream, true);
                    Console.WriteLine("Encountered INSU chunk, skipping.");
                    break;
                default:
                    throw new NotImplementedException($"Unknown chunk: {id}");
            }

            id = ReadChunkHeader(stream, name, out unusedNextHeaderOffset, out dontKnowWhatThisValueDoes);
        }

        if (id == "CNTE")
        {
            // ReSharper disable once UnusedVariable
            var endVersion = ReadUint(stream);
        }

        return group;
    }

    private static bool IsKnownVersion4Node(byte[] bytesArray)
    {
        return KnownV4Bytes.Any(x => x.SequenceEqual(bytesArray));
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

    public static RvmFile ReadRvm(Stream stream)
    {
        uint len,
            dunno;

        var head = ReadChunkHeader(stream, "HEADER", out len, out dunno);
        if (head != "HEAD")
            throw new IOException($"Expected HEAD, found {head}");
        var header = ReadHead(stream);
        var modl = ReadChunkHeader(stream, "MODL", out len, out dunno);
        if (modl != "MODL")
            throw new IOException($"Expected MODL, found {modl}");
        var modelParameters = ReadModelParameters(stream);
        var modelChildren = new List<RvmNode>();
        var modelPrimitives = new List<RvmPrimitive>();
        var modelColors = new List<RvmColor>();

        var chunk = ReadChunkHeader(stream, "CHUNK HEADER", out len, out dunno);
        while (chunk != "END:")
        {
            switch (chunk)
            {
                case "CNTB":
                    modelChildren.Add(ReadCntb(stream));
                    break;
                case "PRIM":
                    modelPrimitives.Add(ReadPrimitive(stream));
                    break;
                case "COLR":
                    modelColors.Add(ReadColor(stream));
                    break;
                default:
                    throw new NotImplementedException($"Unknown chunk: {chunk}");
            }

            chunk = ReadChunkHeader(stream, "CHUNK", out len, out dunno);
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
            case 1
            or 2
            or 3
            or 4:
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
