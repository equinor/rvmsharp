namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using NUnit.Framework.Legacy;
using Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class CircleTessellatorTests
{
    [Test]
    public void TessellateCircle_ReturnsCorrectNumberOfVerticesAndIndices()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var circle = new Circle(Matrix4x4.Identity, Vector3.UnitY, 1, Color.Red, dummyBoundingBox);

        var tessellatedCircle = CircleTessellator.Tessellate(circle)!;

        var vertices = tessellatedCircle.Mesh.Vertices;
        var indices = tessellatedCircle.Mesh.Indices;

        Assert.That((vertices.Length - 1) * 3, Is.EqualTo(indices.Length));
    }

    [Test]
    public void TessellateCircle_ReturnsIndicesWithCorrectWindingOrder()
    {
        // This test is based on https://math.stackexchange.com/questions/932800/what-formula-will-tell-if-three-vertices-in-3d-space-are-ordered-clockwise-or-co

        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var circle = new Circle(Matrix4x4.Identity, Vector3.UnitY, 1, Color.Red, dummyBoundingBox);

        var tessellatedCircle = CircleTessellator.Tessellate(circle)!;

        var vertices = tessellatedCircle.Mesh.Vertices;
        var indices = tessellatedCircle.Mesh.Indices;

        for (uint index = 0; index < indices.Length; index += 3)
        {
            uint i1 = indices[index];
            uint i2 = indices[index + 1];
            uint i3 = indices[index + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            var determinant = TessellatorTestUtils.CalculateDeterminant(v1, v2, v3);

            Assert.That(determinant, Is.GreaterThanOrEqualTo(0.0f));
        }
    }
}
