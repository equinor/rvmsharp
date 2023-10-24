namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using SharpGLTF.Memory;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class BoxTessellatorTests
{
    [Test]
    public void TessellateBoxTest()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var box = new Box(Matrix4x4.Identity, 1, Color.Red, dummyBoundingBox);

        var tessellatedBox = BoxTessellator.Tessellate(box);

        var vertices = tessellatedBox.Mesh.Vertices;
        var indices = tessellatedBox.Mesh.Indices;

        Assert.AreEqual(vertices.Length, 8);
        Assert.AreEqual(indices.Length, 36);
    }

    [Test]
    public void WindingOrderTest()
    {
        // This test is based on https://math.stackexchange.com/questions/932800/what-formula-will-tell-if-three-vertices-in-3d-space-are-ordered-clockwise-or-co

        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var box = new Box(Matrix4x4.Identity, 1, Color.Red, dummyBoundingBox);

        var tessellatedBox = BoxTessellator.Tessellate(box);

        var vertices = tessellatedBox.Mesh.Vertices;
        var indices = tessellatedBox.Mesh.Indices;

        // Can calculate the determinant with this formula from:
        // https://www.geeksforgeeks.org/determinant-of-a-matrix/
        //   | a d g |
        //   | b e h |
        //   | c f i |
        // determinant = a(ei - fh) - b(di - gf) + c(dh - eg)

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
