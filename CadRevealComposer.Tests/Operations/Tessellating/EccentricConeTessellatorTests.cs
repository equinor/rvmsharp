namespace CadRevealComposer.Tests.Operations.Tessellating;

using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class EccentricConeTessellatorTests
{
    [Test]
    public void EccentricConeTessellatorTest()
    {
        var dummyBoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);

        var cone = new EccentricCone(Vector3.Zero, Vector3.UnitY, Vector3.UnitY, 1, 1, 1, Color.Red, dummyBoundingBox);

        var tessellatedCone = EccentricConeTessellator.Tessellate(cone);

        var vertices = tessellatedCone.Mesh.Vertices;
        var indices = tessellatedCone.Mesh.Indices;

        Assert.AreEqual(indices.Length, vertices.Length * 3);
    }
}
