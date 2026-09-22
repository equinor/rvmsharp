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
        using var stream = new MemoryStream();

        void WriteUint(uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            stream.Write(bytes);
        }

        void WriteFloat(float value) => WriteUint(BitConverter.SingleToUInt32Bits(value));

        void WriteChunk(string name)
        {
            foreach (var character in name)
                WriteUint(character);
            WriteUint(0);
            WriteUint(0);
        }

        WriteChunk("HEAD");
        WriteUint(1);
        for (var field = 0; field < 4; field++)
            WriteUint(0);
        WriteChunk("MODL");
        WriteUint(1);
        WriteUint(0);
        WriteUint(0);
        WriteChunk("CNTB");
        WriteUint(1);
        for (var field = 0; field < 5; field++)
            WriteUint(0);
        WriteChunk("PRIM");
        WriteUint(1);
        WriteUint((uint)RvmPrimitiveKind.FacetGroup);
        foreach (var component in new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 })
            WriteFloat(component);
        for (var component = 0; component < 6; component++)
            WriteFloat(0);

        int[][] polygonContourSizes =
        [
            [3, 0],
            [4],
            [3],
            [],
            [3],
            [0],
        ];
        WriteUint((uint)polygonContourSizes.Length);
        for (var polygonIndex = 0; polygonIndex < polygonContourSizes.Length; polygonIndex++)
        {
            var contourSizes = polygonContourSizes[polygonIndex];
            WriteUint((uint)contourSizes.Length);
            foreach (var vertexCount in contourSizes)
            {
                WriteUint((uint)vertexCount);
                for (var vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
                {
                    WriteFloat(polygonIndex * 10);
                    WriteFloat(vertexIndex);
                    WriteFloat(0);
                    WriteFloat(0);
                    WriteFloat(0);
                    WriteFloat(1);
                }
            }
        }

        WriteChunk("CNTE");
        WriteUint(1);
        WriteChunk("END:");
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
        Assert.That(
            facetGroup.CalculateBoundingBoxFromVertexPositions(),
            Is.EqualTo(new RvmBoundingBox(Vector3.Zero, new Vector3(40, 3, 0)))
        );
        Assert.That(stream.Position, Is.EqualTo(stream.Length));
    }
}
