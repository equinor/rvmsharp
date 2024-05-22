namespace RvmSharp.Tests.Tessellator;

using NUnit.Framework;
using RvmSharp.Primitives;
using System.Numerics;
using Tessellation;

[TestFixture]
public class MeshTests
{
    static readonly RvmBoundingBox BoundingBoxUnused = new RvmBoundingBox(Vector3.Zero, Vector3.Zero);

    private static RvmMesh GenerateNewStandardMeshForTest()
    {
        // Arbitrary mesh
        int toleranceUnusedForPyramid = -1;
        return TessellatorBridge.Tessellate(
            new RvmPyramid(2, Matrix4x4.Identity, BoundingBoxUnused, 1, 2, 3, 2, 0, 1, 1),
            toleranceUnusedForPyramid
        );
    }

    [Test]
    public void Mesh_Equals_ByContent()
    {
        var initialMesh = GenerateNewStandardMeshForTest();

#pragma warning disable NUnit2009 // Sanity-check test for self equality
        Assert.That(initialMesh, Is.EqualTo(initialMesh));
        Assert.That(initialMesh.GetHashCode(), Is.EqualTo(initialMesh.GetHashCode()));
#pragma warning restore NUnit2009

        var newIdenticalMesh = GenerateNewStandardMeshForTest();
        // Check assertions expected in Equals
        Assert.That(newIdenticalMesh.Vertices, Is.EqualTo(initialMesh.Vertices));
        Assert.That(newIdenticalMesh.Normals, Is.EqualTo(initialMesh.Normals));
        Assert.That(newIdenticalMesh.Triangles, Is.EqualTo(initialMesh.Triangles));
        Assert.That(newIdenticalMesh.Error, Is.EqualTo(initialMesh.Error));

        // Check that Equals works.
        Assert.That(newIdenticalMesh, Is.EqualTo(initialMesh));
        Assert.That(newIdenticalMesh.GetHashCode(), Is.EqualTo(initialMesh.GetHashCode()));

        // Modify the (previously identical) mesh
        newIdenticalMesh.Apply(Matrix4x4.CreateScale(new Vector3(1, 2, 3)));
        Assert.That(initialMesh, Is.Not.EqualTo(newIdenticalMesh));
        Assert.That(initialMesh.GetHashCode(), Is.Not.EqualTo(newIdenticalMesh.GetHashCode()));
    }

    [Test]
    public void MeshApply_ScalesNormals_AccordingToReference()
    {
        // This tests that the normals are scaled (and rotated) according to the mesh, when its scaled non-uniform.
        // See https://web.archive.org/web/20210628111622/https://paroj.github.io/gltut/Illumination/Tut09%20Normal%20Transformation.html


        var unitPyramid = new RvmPyramid(2, Matrix4x4.Identity, BoundingBoxUnused, 1, 1, 0, 0, 0, 0, 1);

        var widePyramidWidth = 10;
        var widePyramid = unitPyramid with { BottomX = widePyramidWidth };

        const float unusedTolerance = -1;
        var unitPyramidMesh = TessellatorBridge.Tessellate(unitPyramid, unusedTolerance);
        var widePyramidMesh = TessellatorBridge.Tessellate(widePyramid, unusedTolerance);

        var scaledUnitPyramid = unitPyramid with
        {
            Matrix = Matrix4x4.Multiply(unitPyramid.Matrix, Matrix4x4.CreateScale(widePyramidWidth, 1, 1))
        };
        var scaledUnitPyramidMesh = TessellatorBridge.Tessellate(scaledUnitPyramid, unusedTolerance);

        Assert.That(unitPyramidMesh, Is.Not.EqualTo(widePyramidMesh));
        Assert.That(unitPyramidMesh, Is.Not.EqualTo(scaledUnitPyramidMesh));
        Assert.That(scaledUnitPyramidMesh, Is.EqualTo(widePyramidMesh));
    }
}
