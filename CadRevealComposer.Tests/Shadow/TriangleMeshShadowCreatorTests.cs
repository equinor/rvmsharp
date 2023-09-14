namespace CadRevealComposer.Tests.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Shadow;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

[TestFixture]
public class TriangleMeshShadowCreatorTests
{
    [Test]
    public void CreateShadow_SmallerThanThreshold()
    {
        var side = TriangleMeshShadowCreator.SizeTreshold / 2f;
        var min = Vector3.Zero;
        var max = new Vector3(side);
        var bb = new BoundingBox(min, max);

        Vector3[] vertices = { new Vector3(0f, 0f, 0f), new Vector3(side), new Vector3(0, 0, side) };

        uint[] indices = { 0, 1, 2 };

        var mesh = new Mesh(vertices, indices, 0.1f);

        var triangleMesh = new TriangleMesh(mesh, 0, Color.Red, bb);
        var result = triangleMesh.CreateShadow();

        Assert.IsTrue(result is Box);

        Assert.AreEqual(triangleMesh.TreeIndex, result.TreeIndex);
        Assert.AreEqual(triangleMesh.Color, result.Color);
        Assert.AreEqual(triangleMesh.AxisAlignedBoundingBox, result.AxisAlignedBoundingBox);

        var box = (Box)result;

        if (!box.InstanceMatrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + box.InstanceMatrix);
        }

        var expectedScale = new Vector3(side);

        Assert.AreEqual(expectedScale, scale);
        Assert.AreEqual(Quaternion.Identity, rotation);
        Assert.AreEqual(new Vector3(side / 2), position);
    }

    [Test]
    public void CreateShadow_LargerThanThreshold()
    {
        var side = TriangleMeshShadowCreator.SizeTreshold * 2f;
        var min = Vector3.Zero;
        var max = new Vector3(side);
        var bb = new BoundingBox(min, max);

        Vector3[] vertices = { new Vector3(0f, 0f, 0f), new Vector3(side), new Vector3(0, 0, side) };

        uint[] indices = { 0, 1, 2 };

        var mesh = new Mesh(vertices, indices, 0.1f);

        var triangleMesh = new TriangleMesh(mesh, 0, Color.Red, bb);
        var result = triangleMesh.CreateShadow();

        Assert.IsTrue(result is TriangleMesh);
        // Cannot compare vertices, because a Simplify might have been run on the mesh
    }
}
