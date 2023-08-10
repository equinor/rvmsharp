namespace CadRevealComposer.Tests.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Shadow;
using CadRevealComposer.Utils;
using System;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class TorusSegmentShadowCreatorTests
{
    [Test]
    public void ConvertBox()
    {
        var matrix = Matrix4x4.Identity;

        var min = new Vector3(-1, -1, 0);
        var max = new Vector3(1, 1, 2);
        var bb = new BoundingBox(min, max);

        var outerRadius = 4f;
        var tubeRadius = 1f;

        var torusSegment = new TorusSegment(MathF.PI * 2, matrix, outerRadius, tubeRadius, 0, Color.Red, bb);

        var result = torusSegment.CreateShadow();

        Assert.IsTrue(result is Box);

        Assert.AreEqual(torusSegment.TreeIndex, result.TreeIndex);
        Assert.AreEqual(torusSegment.Color, result.Color);
        Assert.AreEqual(torusSegment.AxisAlignedBoundingBox, result.AxisAlignedBoundingBox);

        var box = (Box)result;

        if (!box.InstanceMatrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + box.InstanceMatrix);
        }

        var expectedScale = new Vector3(outerRadius * 2, outerRadius * 2, tubeRadius * 2);

        Assert.AreEqual(expectedScale, scale);
        Assert.AreEqual(Quaternion.Identity, rotation);
        Assert.AreEqual(Vector3.Zero, position);
    }
}
