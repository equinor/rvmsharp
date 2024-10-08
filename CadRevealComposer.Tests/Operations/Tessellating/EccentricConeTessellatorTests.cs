﻿namespace CadRevealComposer.Tests.Operations.Tessellating;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Operations.Tessellating;
using NUnit.Framework.Legacy;
using Primitives;

[TestFixture]
public class EccentricConeTessellatorTests
{
    [Test]
    public void TessellateEccentricCone_ReturnsCorrectNumberOfVerticesAndIndices()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var cone = new EccentricCone(Vector3.Zero, Vector3.UnitY, Vector3.UnitY, 1, 1, 1, Color.Red, dummyBoundingBox);

        var tessellatedCone = EccentricConeTessellator.Tessellate(cone)!;

        var vertices = tessellatedCone.Mesh.Vertices;
        var indices = tessellatedCone.Mesh.Indices;

        Assert.That(vertices.Length * 3, Is.EqualTo(indices.Length));
    }

    [Test]
    public void VerticesConformToConeEquation()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var cone = new Cone(
            0,
            MathF.PI * 2,
            Vector3.Zero,
            Vector3.UnitY,
            Vector3.UnitX,
            1,
            1,
            1,
            Color.Red,
            dummyBoundingBox
        );

        var tessellatedCone = ConeTessellator.Tessellate(cone)!;

        var vertices = tessellatedCone.Mesh.Vertices;

        foreach (var vertex in vertices)
        {
            Assert.That(
                cone.RadiusA * cone.RadiusA,
                Is.EqualTo(vertex.X * vertex.X + vertex.Z * vertex.Z).Within(0.0001f)
            );
        }
    }

    [Test]
    public void TessellateEccentricCone_ReturnsIndicesWithCorrectWindingOrder()
    {
        // This test is based on https://math.stackexchange.com/questions/932800/what-formula-will-tell-if-three-vertices-in-3d-space-are-ordered-clockwise-or-co

        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var cone = new EccentricCone(Vector3.Zero, Vector3.UnitY, -Vector3.UnitY, 1, 1, 1, Color.Red, dummyBoundingBox);

        var tessellatedCone = EccentricConeTessellator.Tessellate(cone)!;

        var vertices = tessellatedCone.Mesh.Vertices;
        var indices = tessellatedCone.Mesh.Indices;

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
