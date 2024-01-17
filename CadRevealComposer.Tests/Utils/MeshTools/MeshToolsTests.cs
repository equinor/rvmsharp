namespace CadRevealComposer.Tests.Utils.MeshTools;

using CadRevealComposer.Utils.MeshOptimization;
using System.Numerics;
using Tessellation;

[TestFixture]
public class MeshToolsTests
{
    private static readonly int[] Expected = [0, 1, 0, 1, 0, 0];

    [Test]
    public void Test_DeduplicateVertices()
    {
        // Sample data is not a valid mesh, but tests logic
        var mesh = new Mesh([Vector3.One, Vector3.Zero, Vector3.One], [0, 1, 2, 1, 0, 2], 0);
        var newMesh = MeshTools.DeduplicateVertices(mesh);
        Assert.That(newMesh.Vertices, Is.EquivalentTo(new[] { Vector3.One, Vector3.Zero }));

        // Expect Vertex 0 and 2 to be equal, and that indices have been changed to only be 0
        Assert.That(newMesh.Indices, Is.EquivalentTo(Expected));
    }

    [Test]
    public void EnsureDeduplicateVertices_DoesNothing_WhenNoVerticesAreDuplicates()
    {
        // Sample data is not a valid mesh, but tests logic
        var inputMesh = new Mesh([Vector3.One, Vector3.Zero, -Vector3.One], [0, 1, 2, 1, 0, 2], 0);
        var newMesh = MeshTools.DeduplicateVertices(inputMesh);
        Assert.That(newMesh, Is.Not.SameAs(inputMesh), "Expected not to be reference equal");
        Assert.That(newMesh, Is.EqualTo(inputMesh), "Expected to be deep equal");
    }
}
