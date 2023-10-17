namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class CircleTessellatorTests
{
    [Test]
    public void CircleTessellatorTest()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
        var circle = new Circle(Matrix4x4.Identity, Vector3.UnitY, 1, Color.Red, dummyBoundingBox);

        var tessellatedCircle = CircleTessellator.Tessellate(circle);

        var vertices = tessellatedCircle.Mesh.Vertices;
        var indices = tessellatedCircle.Mesh.Indices;

        Assert.AreEqual(indices.Length, (vertices.Length - 1) * 3);
    }
}
