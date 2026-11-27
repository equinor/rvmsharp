namespace RvmSharp.Tests;

using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using RvmSharp.Primitives;

[TestFixture]
public class RvmParserTests
{
    [Test]
    public void CanReadBasicRvmFile()
    {
        using var rvmFile = TestFileHelpers.GetTestfile(TestFileHelpers.BasicRvmTestFile);

        var rvm = RvmParser.ReadRvm(rvmFile);

        Assert.That(rvm, Is.Not.Null);
        Assert.That(rvm.Header.Date, Is.EqualTo("Mon Dec 28 16:55:23 2020"));
        Assert.That(rvm.Header.Encoding, Is.EqualTo("Unicode UTF-8"));
        Assert.That(
            rvm.Header.Info,
            Is.EqualTo("AVEVA Everything3D Design Mk2.1.0.25[Z21025-12]  (WINDOWS-NT 6.3)  (25 Feb 2020 : 17:59)")
        );
        Assert.That(rvm.Header.Note, Is.EqualTo("Level 1 to 6"));
        Assert.That(rvm.Header.User, Is.EqualTo("f_pdmsbatch@WS3208"));
        Assert.That(rvm.Header.Version, Is.EqualTo(2));

        rvm.AttachAttributes(TestFileHelpers.BasicTxtAttTestFile);
    }

    [Test]
    public void FacetGroupsShareBuffersAndPreserveStablePolygonOrdering()
    {
        using var writer = new RvmTestWriter();
        var stream = writer.Stream;

        writer.WriteFileHeader();
        writer.WriteChunk("CNTB");
        writer.WriteUint(1);
        for (var field = 0; field < 5; field++)
            writer.WriteUint(0);
        writer.WriteChunk("PRIM");
        writer.WriteUint(1);
        writer.WriteUint((uint)RvmPrimitiveKind.FacetGroup);
        foreach (var component in new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 })
            writer.WriteFloat(component);
        for (var component = 0; component < 6; component++)
            writer.WriteFloat(0);

        int[][] polygonContourSizes =
        [
            [3, 0],
            [4],
            [3],
            [],
            [3],
            [0],
        ];
        Vector2[] contourVertices = [new(0, 0), new(3, 0), new(3, 3), new(0, 3)];
        writer.WriteUint((uint)polygonContourSizes.Length);
        for (var polygonIndex = 0; polygonIndex < polygonContourSizes.Length; polygonIndex++)
        {
            var contourSizes = polygonContourSizes[polygonIndex];
            writer.WriteUint((uint)contourSizes.Length);
            foreach (var vertexCount in contourSizes)
            {
                writer.WriteUint((uint)vertexCount);
                for (var vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    var vertex = contourVertices[vertexIndex];
                    writer.WriteFloat(polygonIndex * 10 + vertex.X);
                    writer.WriteFloat(vertex.Y);
                    writer.WriteFloat(0);
                    writer.WriteFloat(0);
                    writer.WriteFloat(0);
                    writer.WriteFloat(1);
                }
            }
        }

        writer.WriteChunk("CNTE");
        writer.WriteUint(1);
        writer.WriteChunk("END:");
        stream.Position = 0;

        var model = RvmParser.ReadRvm(stream).Model;
        var facetGroup = (RvmFacetGroup)model.Children.Single().Children.Single();
        var polygons = facetGroup.Polygons;
        var contours = polygons.SelectMany(polygon => polygon.Contours).ToArray();

        Assert.That(polygons.Select(polygon => polygon.Contours.Count), Is.EqualTo(new[] { 0, 1, 1, 1, 1, 2 }));
        Assert.That(contours.Select(contour => contour.Vertices.Count), Is.EqualTo(new[] { 0, 3, 3, 4, 3, 0 }));
        Assert.That(
            contours.Where(contour => contour.Vertices.Count > 0).Select(contour => contour.Vertices[0].Vertex.X),
            Is.EqualTo(new float[] { 20, 40, 10, 0 })
        );
        foreach (var polygon in polygons)
            Assert.That(polygon.Contours.Array, Is.SameAs(polygons[0].Contours.Array));
        foreach (var contour in contours)
            Assert.That(contour.Vertices.Array, Is.SameAs(contours[0].Vertices.Array));
        Assert.That(contours[0].Vertices.Array, Has.Length.EqualTo(13));
        Assert.That(
            contours.SelectMany(contour => contour.Vertices).Select(vertex => vertex.Normal),
            Is.All.EqualTo(Vector3.UnitZ)
        );
        foreach (var contour in contours.Where(contour => contour.Vertices.Count > 0))
        {
            var vertices = contour.Vertices;
            Assert.That(
                Vector3.Cross(vertices[1].Vertex - vertices[0].Vertex, vertices[2].Vertex - vertices[0].Vertex).Z,
                Is.GreaterThan(0)
            );
        }
        Assert.That(
            facetGroup.CalculateBoundingBoxFromVertexPositions(),
            Is.EqualTo(new RvmBoundingBox(Vector3.Zero, new Vector3(43, 3, 0)))
        );
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
    }

    [Test]
    [TestCase(0L)]
    [TestCase(0x100000000L)]
    public void ReadRvm_WhenNodeIsVersion4_UsesDoublePrecisionTranslations(long positionOffset)
    {
        // Word-swapped doubles (mm) copied from a real v4 node and its facet group primitive.
        // csharpier-ignore -- Keep byte formatting
        byte[] doubleTranslationMm =
        [
            0x00, 0xAA, 0x1B, 0xAF, 0xC1, 0xF8, 0xDD, 0x32, 0x37, 0x07, 0x8C, 0x97, 0xC1, 0xEE,
            0xCC, 0x13, 0xB8, 0x9C, 0x0B, 0xC6, 0x40, 0xF7, 0x2D, 0xE0
        ];
        var expectedTranslation = new Vector3(-6674391f, -4133526f, 94.942045f);
        using var writer = new RvmTestWriter(positionOffset);
        var stream = writer.Stream;

        void WriteFacetGroupPrim(bool withDoubleTranslation)
        {
            writer.WriteChunk("PRIM");
            writer.WriteUint(2);
            writer.WriteUint((uint)RvmPrimitiveKind.FacetGroup);
            // Identity matrix with zero float translation, so a non-zero result can only come from the trailing doubles.
            foreach (var component in new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 })
                writer.WriteUint(BitConverter.SingleToUInt32Bits(component));
            for (var boundingBoxComponent = 0; boundingBoxComponent < 6; boundingBoxComponent++)
                writer.WriteUint(0);
            writer.WriteUint(0); // Polygon count
            if (withDoubleTranslation)
                stream.Write(doubleTranslationMm);
        }

        writer.WriteFileHeader();
        writer.WriteChunk("CNTB");
        writer.WriteUint(4);
        writer.WriteUint(0);
        // Float translation (mm) copied from the same real node.
        foreach (var component in new float[] { -6674391040f, -4133526016f, 94942.046875f })
            writer.WriteUint(BitConverter.SingleToUInt32Bits(component));
        writer.WriteUint(0);
        writer.WriteUint(0);
        stream.Write(doubleTranslationMm);

        // Primitives in v4 nodes may or may not end with a double-precision translation; both have been parsed in real files.
        WriteFacetGroupPrim(withDoubleTranslation: true);
        WriteFacetGroupPrim(withDoubleTranslation: false);

        writer.WriteChunk("CNTE");
        writer.WriteUint(1);
        writer.WriteChunk("END:");
        stream.Position = writer.PositionOffset;

        var node = RvmParser.ReadRvm(stream).Model.Children.Single();

        // The float path would give (-6674391.5, -4133526.25, ...), about 45 cm and 31 cm off.
        Assert.That(node.Translation, Is.EqualTo(expectedTranslation));
        Assert.That(
            node.Children.OfType<RvmPrimitive>().Select(primitive => primitive.Matrix.Translation),
            Is.EqualTo(new[] { expectedTranslation, Vector3.Zero })
        );
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
    }

    [TestCase(0x57020000u)]
    [TestCase(0x00020080u)]
    [NonParallelizable] // Redirects process-wide console output.
    public void ReadRvm_WhenNodeIsVersion3_ParsesWithoutLoggingExtraBytes(uint extraBytes)
    {
        using var writer = new RvmTestWriter();
        writer.WriteFileHeader();
        writer.WriteChunk("CNTB");
        writer.WriteUint(3);
        writer.WriteUint(1); // Name length in 4-byte words
        writer.Stream.Write("NODE"u8);
        writer.WriteFloat(1000);
        writer.WriteFloat(0);
        writer.WriteFloat(0);
        writer.WriteUint(42); // Material ID
        writer.WriteUint(extraBytes);
        writer.WriteChunk("CNTE");
        writer.WriteUint(1);
        writer.WriteChunk("END:");
        writer.Stream.Position = 0;

        using var output = new StringWriter();
        var originalOutput = Console.Out;
        try
        {
            Console.SetOut(output);
            var node = RvmParser.ReadRvm(writer.Stream).Model.Children.Single();
            Assert.That(node.Translation, Is.EqualTo(Vector3.UnitX));
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        Assert.That(output.ToString(), Is.Empty);
        Assert.That(writer.Stream.Position, Is.EqualTo(writer.Stream.Length));
    }

    [Test]
    public void ReadRvm_WhenChunkHasUnreadBytes_ThrowsWithChunkAndNodeName()
    {
        using var writer = new RvmTestWriter();
        writer.WriteFileHeader();
        writer.WriteChunk("CNTB");
        writer.WriteUint(2);
        writer.WriteUint(1); // Name length in 4-byte words
        writer.Stream.Write("NODE"u8);
        for (var field = 0; field < 4; field++)
            writer.WriteUint(0);
        writer.WriteChunk("PRIM");
        writer.WriteUint(2);
        writer.WriteUint((uint)RvmPrimitiveKind.FacetGroup);
        for (var field = 0; field < 12 + 6 + 1; field++)
            writer.WriteUint(0);
        writer.WriteUint(0xDEADBEEF); // Unknown extra data
        writer.WriteChunk("CNTE");
        writer.WriteUint(1);
        writer.WriteChunk("END:");
        writer.Stream.Position = 0;

        var exception = Assert.Throws<InvalidDataException>(() => RvmParser.ReadRvm(writer.Stream));

        Assert.That(exception, Is.Not.Null);
        Assert.That(exception.Message, Does.StartWith("PRIM chunk in \"NODE\""));
        Assert.That(exception.Message, Does.Contain("4 bytes were left unread"));
    }

    /// <summary>
    /// Writes big-endian RVM data and fills in each chunk's next-header offset when the following chunk starts.
    /// </summary>
    private sealed class RvmTestWriter : IDisposable
    {
        private long _pendingNextHeaderOffsetPosition = -1;

        public long PositionOffset { get; }
        public Stream Stream { get; }

        public RvmTestWriter(long positionOffset = 0)
        {
            PositionOffset = positionOffset;
            var memoryStream = new MemoryStream();
            Stream = positionOffset == 0 ? memoryStream : new OffsetStream(memoryStream, positionOffset);
        }

        public void WriteUint(uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            Stream.Write(bytes);
        }

        public void WriteFloat(float value) => WriteUint(BitConverter.SingleToUInt32Bits(value));

        public void WriteFileHeader()
        {
            WriteChunk("HEAD");
            WriteUint(1);
            for (var field = 0; field < 4; field++)
                WriteUint(0);
            WriteChunk("MODL");
            WriteUint(1);
            WriteUint(0);
            WriteUint(0);
        }

        public void WriteChunk(string name)
        {
            if (_pendingNextHeaderOffsetPosition >= 0)
            {
                var chunkStart = Stream.Position;
                Stream.Position = _pendingNextHeaderOffsetPosition;
                WriteUint((uint)chunkStart);
                Stream.Position = chunkStart;
            }

            foreach (var character in name)
                WriteUint(character);
            _pendingNextHeaderOffsetPosition = Stream.Position;
            WriteUint(0);
            WriteUint(0);
        }

        public void Dispose() => Stream.Dispose();
    }

    // Simulates positions past 4 GiB so offset-wrap tests need only a small in-memory file.
    private sealed class OffsetStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _positionOffset;

        public OffsetStream(Stream inner, long positionOffset)
        {
            _inner = inner;
            _positionOffset = positionOffset;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _positionOffset + _inner.Length;

        public override long Position
        {
            get => _positionOffset + _inner.Position;
            set => _inner.Position = value - _positionOffset;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
        {
            var innerOffset = origin == SeekOrigin.Begin ? offset - _positionOffset : offset;
            return _positionOffset + _inner.Seek(innerOffset, origin);
        }

        public override void SetLength(long value) => _inner.SetLength(value - _positionOffset);

        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    }
}
