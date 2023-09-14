namespace CadRevealComposer.Tests.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Shadow;
using CadRevealComposer.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

[TestFixture]
public class EllipsoidSegmentShadowCreatorTests
{
    [Test]
    public void ConvertToBox()
    {
        var matrix = Matrix4x4.Identity;

        var center = new Vector3(0, 0, 0);

        var min = new Vector3(-1, -1, 0);
        var max = new Vector3(1, 1, 2);
        var bb = new BoundingBox(min, max);

        var horizontalRadius = 2f;
        var verticalRadius = 3f;
        var height = 4f;

        var ellipsoidSegment = new EllipsoidSegment(
            matrix,
            horizontalRadius,
            verticalRadius,
            height,
            center,
            Vector3.UnitX,
            0,
            Color.Red,
            bb
        );

        var result = ellipsoidSegment.CreateShadow();

        Assert.IsTrue(result is Box);

        Assert.AreEqual(ellipsoidSegment.TreeIndex, result.TreeIndex);
        Assert.AreEqual(ellipsoidSegment.Color, result.Color);
        Assert.AreEqual(ellipsoidSegment.AxisAlignedBoundingBox, result.AxisAlignedBoundingBox);

        var box = (Box)result;

        if (!box.InstanceMatrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + box.InstanceMatrix);
        }

        var expectedScale = new Vector3(horizontalRadius * 2, horizontalRadius * 2, height);

        Assert.AreEqual(expectedScale, scale);
        Assert.AreEqual(Quaternion.Identity, rotation);
        Assert.AreEqual(Vector3.Zero, position);
    }
}
