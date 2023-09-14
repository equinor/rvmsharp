namespace CadRevealComposer.Tests.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Shadow;
using CadRevealComposer.Utils;
using System;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class EccentricConeShadowCreatorTests
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

        var radiusA = 2f;
        var radiusB = 3f;
        var height = Vector3.Distance(centerA, centerB);

        var eccentricCone = new EccentricCone(
            matrix,
            centerA,
            centerB,
            Vector3.UnitX,
            radiusA,
            radiusB,
            0,
            Color.Red,
            bb
        );

        var result = eccentricCone.CreateShadow();

        Assert.IsTrue(result is Box);

        Assert.AreEqual(eccentricCone.TreeIndex, result.TreeIndex);
        Assert.AreEqual(eccentricCone.Color, result.Color);
        Assert.AreEqual(eccentricCone.AxisAlignedBoundingBox, result.AxisAlignedBoundingBox);

        var box = (Box)result;

        if (!box.InstanceMatrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + box.InstanceMatrix);
        }

        var largestRadius = MathF.Max(radiusA, radiusB);
        var expectedScale = new Vector3(largestRadius * 2, largestRadius * 2, height);

        Assert.AreEqual(expectedScale, scale);
        Assert.AreEqual(Quaternion.Identity, rotation);
        Assert.AreEqual(Vector3.Zero, position);
    }
}
