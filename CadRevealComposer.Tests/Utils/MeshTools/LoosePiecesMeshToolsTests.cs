namespace CadRevealComposer.Tests.Utils.MeshTools;

using System.Numerics;
using CadRevealComposer.Utils.MeshOptimization;
using Tessellation;

public class LoosePiecesMeshToolsTests
{
    [Test]
    public void SplitMeshByLoosePieces_WhenGivenMeshWithTwoDistinctParts_SplitsIntoTwoMeshesWithExpectedCoordinates()
    {
        // No overlapping vertexes
        var bb1 = new BoundingBox(Vector3.One, Vector3.One * 2);
        var bb2 = new BoundingBox(Vector3.Zero, Vector3.One * 0.5f);

        var mesh1 = GenerateMeshFromBoundingBox(bb1);
        var mesh2 = GenerateMeshFromBoundingBox(bb2);
        var joinedMeshes = JoinMeshes(new[] { mesh1, mesh2 });
        var result = LoosePiecesMeshTools.SplitMeshByLoosePieces(joinedMeshes);
        Assert.That(result, Has.Exactly(2).Items);
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Vertices, Is.EquivalentTo(mesh1.Vertices));
            Assert.That(result[1].Vertices, Is.EquivalentTo(mesh2.Vertices));
        });
    }

    [Test]
    public void SplitMeshByLoosePieces_WhenGivenMeshWithOverlappingVertexes_DoesNotSplit()
    {
        // Overlaps by sharing the Vector3.One vertex.
        var bb1 = new BoundingBox(Vector3.One, Vector3.One * 2);
        var bb2 = new BoundingBox(Vector3.Zero, Vector3.One);

        var mesh1 = GenerateMeshFromBoundingBox(bb1);
        var mesh2 = GenerateMeshFromBoundingBox(bb2);
        var joinedMeshes = JoinMeshes(new[] { mesh1, mesh2 });
        var result = LoosePiecesMeshTools.SplitMeshByLoosePieces(joinedMeshes);
        Assert.That(result, Has.Exactly(1).Items);
        Assert.That(result.First(), Is.EqualTo(joinedMeshes)); // Assuming the input is returned as is.
    }

    [Test]
    public void SplitMeshByLoosePieces_WhenGivenMeshWithOnlyOnePiece_ReturnsOriginalInputMesh()
    {
        // Overlaps by sharing the Vector3.One vertex.
        var bb1 = new BoundingBox(Vector3.One, Vector3.One * 2);

        var mesh1 = GenerateMeshFromBoundingBox(bb1);
        var result = LoosePiecesMeshTools.SplitMeshByLoosePieces(mesh1);
        Assert.That(result, Has.Exactly(1).Items);
        Assert.That(result.First(), Is.SameAs(mesh1)); // Assuming the input is returned as is.
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

        // create vertexes and triangles for a cube using min and max vectors
        var vertexes = new List<Vector3>
        {
            new(min.X, min.Y, min.Z), // 0
            new(max.X, min.Y, min.Z), // 1
            new(max.X, max.Y, min.Z), // 2
            new(min.X, max.Y, min.Z), // 3
            new(min.X, min.Y, max.Z), // 4
            new(max.X, min.Y, max.Z), // 5
            new(max.X, max.Y, max.Z), // 6
            new(min.X, max.Y, max.Z) // 7
        };
        // csharpier-ignore -- prettier manual formatting
        var triangleIndexes = new uint[]
        {
            0, 1, 2, 0, 2, 3, // front
            1, 5, 6, 1, 6, 2, // right
            5, 4, 7, 5, 7, 6, // back
            4, 0, 3, 4, 3, 7, // left
            3, 2, 6, 3, 6, 7, // top
            4, 5, 1, 4, 1, 0  // bottom
        };

        return new Mesh(vertexes.ToArray(), triangleIndexes, 0);
    }
}
