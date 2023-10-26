namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using System;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class TorusSegmentTessellatorTests
{
    [Test]
    public void TessellateTorusSegment_ReturnsCorrectNumberOfVerticesAndIndices()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var torus = new TorusSegment(MathF.PI * 2, Matrix4x4.Identity, 1, 0.25f, 1, Color.Red, dummyBoundingBox);
        var torusTessellate = TorusSegmentTessellator.Tessellate(torus);

        var vertices = torusTessellate.Mesh.Vertices;
        var indices = torusTessellate.Mesh.Indices;

        Assert.Greater(vertices.Length, 0);
        Assert.Greater(indices.Length, 0);
    }

    [Test]
    public void VerticesConformToTorusEquation()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var torus = new TorusSegment(MathF.PI * 2, Matrix4x4.Identity, 1, 0.25f, 1, Color.Red, dummyBoundingBox);
        var tessellatedTorus = TorusSegmentTessellator.Tessellate(torus);

        var vertices = tessellatedTorus.Mesh.Vertices;

        var a = torus.TubeRadius;
        var c = torus.Radius;

        foreach (var vertex in vertices)
        {
            var x = vertex.X;
            var y = vertex.Y;
            var z = vertex.Z;

            Assert.That(MathF.Pow(c - MathF.Sqrt(x * x + y * y), 2) + z * z, Is.EqualTo(a * a).Within(0.001f));
        }
    }

    [Test]
    public void TessellateTorusSegment_ReturnsIndicesWithCorrectWindingOrder()
    {
        // This test is based on https://math.stackexchange.com/questions/932800/what-formula-will-tell-if-three-vertices-in-3d-space-are-ordered-clockwise-or-co

        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var torus = new TorusSegment(MathF.PI * 2, Matrix4x4.Identity, 1, 0.25f, 1, Color.Red, dummyBoundingBox);
        var torusTessellate = TorusSegmentTessellator.Tessellate(torus);

        var vertices = torusTessellate.Mesh.Vertices;
        var indices = torusTessellate.Mesh.Indices;

        // Can calculate the determinant with this formula from:
        // https://www.geeksforgeeks.org/determinant-of-a-matrix/
        //   | a d g |
        //   | b e h |
        //   | c f i |
        // determinant = a(ei - fh) - b(di - gf) + c(dh - eg)


        var poloidalSegments = indices[1]; // NOTE: This is dependent on tessellation order
        var toroidalSegments = indices.Length / (3 * poloidalSegments * 2);
        var turnIncrement = 2 * MathF.PI / toroidalSegments;

        var vertex1 = vertices[indices[0]];
        var vertex2 = vertices[indices[1]];

        var midPoint = (vertex1 + vertex2) / 2f;

        var pDirectionVector = Vector3.Normalize(midPoint with { Z = 0 });
        var p = pDirectionVector * torus.Radius;

        for (int j = 0; j < toroidalSegments; j++)
        {
            var turnAngle = j * turnIncrement;
            var normal = Vector3.UnitZ;

            var q = Quaternion.CreateFromAxisAngle(normal, turnAngle);
            var toroidalCenter = Vector3.Transform(p, q);

            for (int k = 0; k < poloidalSegments * 2; k++)
            {
                // toroidal segment * poloidalsegments * indices in a triangle * triangles in a poloidal segment + poloidalsegment * indices in a triangle
                var firstVertexInTriangleIndex = j * poloidalSegments * 3 * 2 + k * 3;

                uint i1 = indices[firstVertexInTriangleIndex];
                uint i2 = indices[firstVertexInTriangleIndex + 1];
                uint i3 = indices[firstVertexInTriangleIndex + 2];

                Vector3 v1 = vertices[i1] - toroidalCenter;
                Vector3 v2 = vertices[i2] - toroidalCenter;
                Vector3 v3 = vertices[i3] - toroidalCenter;

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
}
