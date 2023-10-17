namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using System;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class ConeTessellatorTests
{
    [Test]
    public void ConeTessellatorTest()
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

        var tessellatedCone = ConeTessellator.Tessellate(cone);

        var vertices = tessellatedCone.Mesh.Vertices;
        var indices = tessellatedCone.Mesh.Indices;

        Assert.AreEqual(indices.Length, vertices.Length * 3);
    }
}
