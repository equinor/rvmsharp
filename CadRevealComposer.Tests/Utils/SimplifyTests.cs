namespace CadRevealComposer.Tests.Utils;

using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Numerics;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Utils;
using Primitives;
using Tessellation;

public static class SimplifyTests
{
    public enum MeshType
    {
        MeshBox,
        MeshSphere,
    }

    private static Mesh GenBoxMesh()
    {
        var vertices = new List<Vector3>();
        for (float x = -1.0f; x <= 1.01f; x += 0.05f)
        {
            for (float y = -1.0f; y <= 1.01f; y += 0.05f)
            {
                vertices.Add(new Vector3(x, y, 1.0f));
                vertices.Add(new Vector3(x, y, -1.0f));

                vertices.Add(new Vector3(x, 1.0f, y));
                vertices.Add(new Vector3(x, -1.0f, y));

                vertices.Add(new Vector3(1.0f, x, y));
                vertices.Add(new Vector3(-1.0f, x, y));
            }
        }
        var indices = vertices.Select((v, i) => (uint)i);
        return new Mesh(vertices.ToArray(), indices.ToArray(), 0.01f);
    }

    private static Mesh GenSphereMesh(Vector3 center)
    {
        const float R = 2.0f;
        var vertices = new List<Vector3>();
        for (float theta = 0.0f; theta <= (float)(2.0 * Math.PI); theta += 0.05f)
        {
            for (float phi = 0.0f; phi <= (float)Math.PI; phi += 0.05f)
            {
                vertices.Add(
                    new Vector3(
                        center.X + R * float.Cos(theta) * float.Sin(phi),
                        center.Y + R * float.Sin(theta) * float.Sin(phi),
                        center.Z + R * float.Cos(phi)
                    )
                );
            }
        }
        var indices = vertices.Select((v, i) => (uint)i);
        return new Mesh(vertices.ToArray(), indices.ToArray(), 0.01f);
    }

    private static Mesh GenTestMesh(MeshType meshType)
    {
        switch (meshType)
        {
            case MeshType.MeshBox:
                return GenBoxMesh();
            case MeshType.MeshSphere:
                return GenSphereMesh(new Vector3(0.0f, 0.0f, 0.0f));
        }

        return null;
    }

    [Test]
    [TestCase(MeshType.MeshSphere)]
    [TestCase(MeshType.MeshBox)]
    public static void CheckConvertToConvexHull_GivenManifoldMesh_VerifyThatConvexHullConversionWasInvoked(
        MeshType meshType
    )
    {
        // Create mesh data to input into the convex hull algorithm
        Mesh testMesh = GenTestMesh(meshType);

        // Perform convex hull conversion
        Mesh convertedMeshLowTolerance = CadRevealComposer.Utils.Simplify.ConvertToConvexHull(testMesh, 0.1f);
        Mesh convertedMeshHighTolerance = CadRevealComposer.Utils.Simplify.ConvertToConvexHull(testMesh, 1.0f);

        // Check that the output is not null
        Assert.That(convertedMeshLowTolerance, Is.Not.Null);

        Assert.Multiple(() =>
        {
            // Check that the number of vertices reduced (also implicitly testing that input and output are not the same)
            Assert.That(convertedMeshLowTolerance.Vertices, Has.Length.LessThan(testMesh.Vertices.Length));

            // Check that the tolerance parameter had an effect
            Assert.That(
                convertedMeshLowTolerance.Vertices,
                Has.Length.GreaterThan(convertedMeshHighTolerance.Vertices.Length)
            );
        });
    }

    [Test]
    public static void CheckConvertToConvexHull_GivenNonManifoldMesh_VerifyThatConvexHullConversionDoNoChange()
    {
        // Create an empty mesh to provoke a failure of the convex hull conversion
        var emptyTestMesh = new Mesh([], Array.Empty<uint>(), 0.01f);

        // Perform convex hull conversion
        Mesh convertedMesh = CadRevealComposer.Utils.Simplify.ConvertToConvexHull(emptyTestMesh, 0.1f);

        // Check that the output is not null
        Assert.That(convertedMesh, Is.Not.Null);

        // Check that the conversion failed, and therefore that the output mesh is the same as the one we put in
        Assert.That(convertedMesh, Is.SameAs(emptyTestMesh));
    }
}
