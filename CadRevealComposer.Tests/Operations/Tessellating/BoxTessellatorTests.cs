namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class BoxTessellatorTests
{
    [Test]
    public void TessellateBox_ReturnsCorrectNumberOfVerticesAndIndices()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var box = new Box(Matrix4x4.Identity, 1, Color.Red, dummyBoundingBox);

        var tessellatedBox = BoxTessellator.Tessellate(box);

        var vertices = tessellatedBox!.Mesh.Vertices;
        var indices = tessellatedBox!.Mesh.Indices;

        Assert.AreEqual(vertices.Length, 8);
        Assert.AreEqual(indices.Length, 36);
    }

    [Test]
    public void TessellateBox_ReturnsIndicesWithCorrectWindingOrder()
    {
        // This test is based on https://math.stackexchange.com/questions/932800/what-formula-will-tell-if-three-vertices-in-3d-space-are-ordered-clockwise-or-co

        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var box = new Box(Matrix4x4.Identity, 1, Color.Red, dummyBoundingBox);

        var tessellatedBox = BoxTessellator.Tessellate(box);

        var vertices = tessellatedBox!.Mesh.Vertices;
        var indices = tessellatedBox!.Mesh.Indices;

        for (uint index = 0; index < indices.Length; index += 3)
        {
            uint i1 = indices[index];
            uint i2 = indices[index + 1];
            uint i3 = indices[index + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            var determinant = TessellatorTestUtils.CalculateDeterminant(v1, v2, v3);

            Assert.GreaterOrEqual(determinant, 0.0f);
        }
    }
}
