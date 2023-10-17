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
    public void GeneralRingTessellatorTest_WithFullThickness()
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

        var tessellatedGeneralRing = GeneralRingTessellator.Tessellate(ring);
        var vertices = tessellatedGeneralRing.Mesh.Vertices;
        var indices = tessellatedGeneralRing.Mesh.Indices;

        Assert.AreEqual(indices.Length, (vertices.Length - 2) * 3);
    }

    [Test]
    public void GeneralRingTessellatorTest_WithNotFullThicknes()
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

        var tessellatedGeneralRing = GeneralRingTessellator.Tessellate(ring);
        var vertices = tessellatedGeneralRing.Mesh.Vertices;
        var indices = tessellatedGeneralRing.Mesh.Indices;

        Assert.AreEqual(indices.Length, (vertices.Length - 2) * 3);
    }
}
