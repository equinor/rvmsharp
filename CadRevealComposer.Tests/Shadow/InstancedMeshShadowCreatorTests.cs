namespace CadRevealComposer.Tests.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Shadow;
using CadRevealComposer.Tessellation;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class InstancedMeshShadowCreatorTests
{
    [Test]
    public void ConvertToBox()
    {
        Vector3[] vertices = { new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), new Vector3(0, 0, 1f) };
        uint[] indices = { 0, 1, 2 };

        var mesh = new Mesh(vertices, indices, 0.1f);
        var matrix = Matrix4x4.Identity;
        var bb = new BoundingBox(Vector3.Zero, new Vector3(1f));

        var instancedMesh = new InstancedMesh(0, mesh, matrix, 0, Color.Red, bb);

        var result = instancedMesh.CreateShadow();

        Assert.IsTrue(result is InstancedMesh);

        Assert.AreEqual(instancedMesh.TreeIndex, result.TreeIndex);
        Assert.AreEqual(instancedMesh.Color, result.Color);
        Assert.AreEqual(instancedMesh.AxisAlignedBoundingBox, result.AxisAlignedBoundingBox);

        var newInstancedMesh = (InstancedMesh)result;

        var expectedVertices = new[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 1f, 1f),
            new Vector3(1f, 1f, 1f)
        };

        var expectedIndices = new[]
        {
            0,
            1,
            2,
            1,
            2,
            3,
            0,
            1,
            4,
            1,
            4,
            5,
            0,
            2,
            4,
            2,
            4,
            6,
            2,
            3,
            6,
            3,
            6,
            7,
            1,
            3,
            5,
            3,
            5,
            7,
            4,
            5,
            6,
            5,
            6,
            7
        };

        Assert.AreEqual(expectedVertices, newInstancedMesh.TemplateMesh.Vertices);
        Assert.AreEqual(expectedIndices, newInstancedMesh.TemplateMesh.Indices);
    }
}
