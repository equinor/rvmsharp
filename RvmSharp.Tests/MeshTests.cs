namespace RvmSharp.Tests;

using NUnit.Framework;
using System.Numerics;
using Tessellation;

[TestFixture]
public class MeshTests
{
    [Test]
    public void ApplySingleColorTest()
    {
        Vector3[] vertices = { new(3, 4, 5) };
        Vector3[] normals = { new(6, 7, 8) };
        int[] triangles = { 0, 1, 2};

        Vector3[] expectedColors = { new(0.07098039f, 0.8396079f, 0.52980393f) };

        var mesh = new Mesh(vertices, normals, triangles, 0);
        
        mesh.ApplySingleColor(1_234_567);
        
        Assert.That(mesh.VertexColors.Length, Is.EqualTo(mesh.Vertices.Length));
        Assert.That(mesh.VertexColors, Is.EqualTo(expectedColors));
    }
}