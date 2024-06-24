namespace CadRevealComposer.Tests.Utils.MeshTools;

using System.Numerics;
using CadRevealComposer.Utils.MeshOptimization;
using Tessellation;

public class DisjoinMeshesTests
{
    [Test]
    public void DisjoinMeshes_WhenGivenMeshWithTwoDistinctParts_SplitsIntoTwoMeshesWithExpectedCoordinates()
    {
        // No overlapping vertexes
        var bb1 = new BoundingBox(Vector3.One, Vector3.One * 2);
        var bb2 = new BoundingBox(Vector3.Zero, Vector3.One * 0.5f);

        var mesh1 = GenerateMeshFromBoundingBox(bb1);
        var mesh2 = GenerateMeshFromBoundingBox(bb2);
        var joinedMeshes = JoinMeshes(new[] { mesh1, mesh2 });
        var result = DisjointMeshTools.SplitDisjointPieces(joinedMeshes);
        Assert.That(result, Has.Exactly(2).Items);
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Vertices, Is.EquivalentTo(mesh1.Vertices));
            Assert.That(result[1].Vertices, Is.EquivalentTo(mesh2.Vertices));
        });
    }

    [Test]
    public void DisjoinMeshes_WhenGivenMeshWithOverlappingVertexes_DoesNotSplit()
    {
        // Overlaps by sharing the Vector3.One vertex.
        var bb1 = new BoundingBox(Vector3.One, Vector3.One * 2);
        var bb2 = new BoundingBox(Vector3.Zero, Vector3.One);

        var mesh1 = GenerateMeshFromBoundingBox(bb1);
        var mesh2 = GenerateMeshFromBoundingBox(bb2);
        var joinedMeshes = JoinMeshes(new[] { mesh1, mesh2 });
        var result = DisjointMeshTools.SplitDisjointPieces(joinedMeshes);
        Assert.That(result, Has.Exactly(1).Items);
    }

    private static Mesh JoinMeshes(Mesh[] meshes)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();
        float error = meshes.Max(x => x.Error);

        foreach (var mesh in meshes)
        {
            var vertexOffset = vertices.Count;
            vertices.AddRange(mesh.Vertices);
            indices.AddRange(mesh.Indices.Select(x => x + (uint)vertexOffset));
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), error);
    }

    private static Mesh GenerateMeshFromBoundingBox(BoundingBox bb)
    {
        var min = bb.Min;
        var max = bb.Max;
        var vertexes = new[]
        {
            max,
            min,
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z)
        };
        // Triangles for a cube
        // csharpier-ignore - prettier manual formatting
        var triangleIndexes = new uint[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, // front and back
            1, 3, 0, 2, 5, 4, 7, 6, // top and bottom
            0, 4, 1, 5, 2, 6, 3, 7 // left and right
        };

        return new Mesh(vertexes.ToArray(), triangleIndexes, 0);
    }
}
