namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
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
}
