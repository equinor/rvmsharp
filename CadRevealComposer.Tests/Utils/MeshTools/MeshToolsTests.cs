namespace CadRevealComposer.Tests.Utils.MeshTools;

using CadRevealFbxProvider.BatchUtils;
using System.Numerics;
using Tessellation;

[TestFixture]
public class MeshToolsTests
{
    [Test]
    public void Test_DeduplicateVertices()
    {
        // Sample data is not a valid mesh, but tests logic
        var mesh = new Mesh(new[] { Vector3.One, Vector3.Zero, Vector3.One }, new uint[] { 0, 1, 2, 1, 0, 2 }, 0);
        var newMesh = MeshTools.DeduplicateVertices(mesh);
        Assert.That(newMesh.Vertices, Is.EquivalentTo(new[] { Vector3.One, Vector3.Zero }));

        // Expect Vertex 0 and 2 to be equal, and that indices have been changed to only be 0
        Assert.That(newMesh.Indices, Is.EquivalentTo(new[] { 0, 1, 0, 1, 0, 0 }));
    }

    [Test]
    public void EnsureDeduplicateVertices_DoesNothing_WhenNoVertsAreDuplicates()
    {
        // Sample data is not a valid mesh, but tests logic
        var inputMesh = new Mesh(new[] { Vector3.One, Vector3.Zero, -Vector3.One }, new uint[] { 0, 1, 2, 1, 0, 2 }, 0);
        var newMesh = MeshTools.DeduplicateVertices(inputMesh);
        Assert.That(newMesh, Is.Not.SameAs(inputMesh), "Expected not to be reference equal");
        Assert.That(newMesh, Is.EqualTo(inputMesh), "Expected to be deep equal");
    }
}
