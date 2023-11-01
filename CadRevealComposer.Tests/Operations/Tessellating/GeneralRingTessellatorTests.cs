namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using System;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class GeneralRingTessellatorTests
{
    [Test]
    public void TessellateGeneralRing_WithFullThickness_ReturnsCorrectNumberOfVerticesAndIndices()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var ring = new GeneralRing(
            0,
            MathF.PI * 2,
            Matrix4x4.Identity,
            Vector3.UnitY,
            1,
            1,
            Color.Red,
            dummyBoundingBox
        );

        var tessellatedGeneralRing = (TriangleMesh)GeneralRingTessellator.Tessellate(ring);
        var vertices = tessellatedGeneralRing.Mesh.Vertices;
        var indices = tessellatedGeneralRing.Mesh.Indices;

        Assert.AreEqual(indices.Length, (vertices.Length - 2) * 3);
    }

    [Test]
    public void TessellateGeneralRing_WithoutFullThickness_ReturnsCorrectNumberOfVerticesAndIndices()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var ring = new GeneralRing(
            0,
            MathF.PI * 2,
            Matrix4x4.Identity,
            Vector3.UnitY,
            0.5f,
            1,
            Color.Red,
            dummyBoundingBox
        );

        var tessellatedGeneralRing = (TriangleMesh)GeneralRingTessellator.Tessellate(ring);
        var vertices = tessellatedGeneralRing.Mesh.Vertices;
        var indices = tessellatedGeneralRing.Mesh.Indices;

        Assert.AreEqual(indices.Length, (vertices.Length - 2) * 3);
    }

    [Test]
    public void TessellateGeneralRing_WithFullThickness_ReturnsIndicesWithCorrectWindingOrder()
    {
        // This test is based on https://math.stackexchange.com/questions/932800/what-formula-will-tell-if-three-vertices-in-3d-space-are-ordered-clockwise-or-co

        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var ring = new GeneralRing(
            0,
            MathF.PI * 2,
            Matrix4x4.Identity,
            Vector3.UnitY,
            1,
            1,
            Color.Red,
            dummyBoundingBox
        );

        var tessellatedGeneralRing = (TriangleMesh)GeneralRingTessellator.Tessellate(ring);
        var vertices = tessellatedGeneralRing.Mesh.Vertices;
        var indices = tessellatedGeneralRing.Mesh.Indices;

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

    [Test]
    public void TessellateGeneralRing_WithoutFullThickness_ReturnsIndicesWithCorrectWindingOrder()
    {
        // This test is based on https://math.stackexchange.com/questions/932800/what-formula-will-tell-if-three-vertices-in-3d-space-are-ordered-clockwise-or-co

        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var ring = new GeneralRing(
            0,
            MathF.PI * 2,
            Matrix4x4.Identity,
            Vector3.UnitY,
            0.5f,
            1,
            Color.Red,
            dummyBoundingBox
        );

        var tessellatedGeneralRing = (TriangleMesh)GeneralRingTessellator.Tessellate(ring);
        var vertices = tessellatedGeneralRing.Mesh.Vertices;
        var indices = tessellatedGeneralRing.Mesh.Indices;

        for (uint index = 0; index < indices.Length; index += 3)
        {
            uint i1 = indices[index];
            uint i2 = indices[index + 1];
            uint i3 = indices[index + 2];

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            float a = v1.X;
            float b = v1.Y;
            float c = v1.Z;
            float d = v2.X;
            float e = v2.Y;
            float f = v2.Z;
            float g = v3.X;
            float h = v3.Y;
            float i = v3.Z;

            float determinant = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);

            Assert.GreaterOrEqual(determinant, 0.0f);
        }
    }
}
