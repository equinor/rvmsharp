namespace CadRevealComposer.Tests.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Shadow;
using CadRevealComposer.Utils;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class CylinderShadowCreatorTests
{
    [Test]
    public void ConvertToBox()
    {
        var matrix = Matrix4x4.Identity;

        var centerA = new Vector3(0, 0, 0);
        var centerB = new Vector3(0, 0, 2);

        var min = new Vector3(-1, -1, 0);
        var max = new Vector3(1, 1, 2);
        var bb = new BoundingBox(min, max);

        var radius = 2f;
        var height = Vector3.Distance(centerA, centerB);

        var cylinder = new GeneralCylinder(
            matrix,
            0f,
            2 * MathF.PI,
            centerA,
            centerB,
            Vector3.UnitX,
            Vector4.UnitZ,
            Vector4.UnitZ,
            radius,
            0,
            Color.Red,
            bb
        );

        var result = cylinder.CreateShadow();

        Assert.IsTrue(result is Box);

        Assert.AreEqual(cylinder.TreeIndex, result.TreeIndex);
        Assert.AreEqual(cylinder.Color, result.Color);
        Assert.AreEqual(cylinder.AxisAlignedBoundingBox, result.AxisAlignedBoundingBox);

        var box = (Box)result;

        if (!box.InstanceMatrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + box.InstanceMatrix);
        }

        var expectedScale = new Vector3(radius * 2, radius * 2, height);

        Assert.AreEqual(expectedScale, scale);
        Assert.AreEqual(Quaternion.Identity, rotation);
        Assert.AreEqual(Vector3.Zero, position);
    }
}
