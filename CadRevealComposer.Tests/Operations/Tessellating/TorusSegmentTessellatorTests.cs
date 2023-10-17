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
    public void TorusSegmentTessellatorTest()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var torus = new TorusSegment(MathF.PI * 2, Matrix4x4.Identity, 1, 0.25f, 1, Color.Red, dummyBoundingBox);
        var torusTessellate = TorusSegmentTessellator.Tessellate(torus);

        var vertices = torusTessellate.Mesh.Vertices;
        var indices = torusTessellate.Mesh.Indices;

        Assert.Greater(vertices.Length, 0);
        Assert.Greater(indices.Length, 0);
    }
}
