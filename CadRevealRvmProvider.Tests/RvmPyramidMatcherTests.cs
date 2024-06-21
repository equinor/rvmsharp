namespace CadRevealRvmProvider.Tests;

using System.Numerics;
using CadRevealComposer.Utils;
using Operations;
using RvmSharp.Primitives;

[TestFixture]
public class RvmPyramidMatcherTests
{
    [Test]
    [TestCase(1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 1, ExpectedResult = true)]
    [TestCase(1, 1, 1, 1, 0, 0, 1, 2, 2, 2, 2, 0, 0, 1, ExpectedResult = true)]
    [TestCase(1, 1, 1, 1, 0, 0, 1, 2, 2, 3, 2, 0, 0, 1, ExpectedResult = false)]
    [TestCase(1, 1, 1, 2, 0, 0, 1, 2, 2, 4, 2, 0, 0, 1, ExpectedResult = true)]
    public bool MatchPyramids(
        float aBottomX,
        float aBottomY,
        float aTopX,
        float aTopY,
        float aOffsetX,
        float aOffsetY,
        float aHeight,
        float bBottomX,
        float bBottomY,
        float bTopX,
        float bTopY,
        float bOffsetX,
        float bOffsetY,
        float bHeight
    )
    {
        var pyramidA = new RvmPyramid(
            1,
            Matrix4x4.Identity,
            new RvmBoundingBox(-Vector3.One, Vector3.One),
            aBottomX,
            aBottomY,
            aTopX,
            aTopY,
            aOffsetX,
            aOffsetY,
            aHeight
        );
        var pyramidB = new RvmPyramid(
            1,
            Matrix4x4.Identity,
            new RvmBoundingBox(-Vector3.One, Vector3.One),
            bBottomX,
            bBottomY,
            bTopX,
            bTopY,
            bOffsetX,
            bOffsetY,
            bHeight
        );

        var result = RvmPyramidMatcher.Match(pyramidA, pyramidB, out var transform);
        if (!result)
        {
            return result;
        }

        if (transform.DecomposeAndNormalize(out var scale, out var rotation, out var translation))
        {
            (float rollX, float pitchY, float yawZ) = rotation.ToEulerAngles();
            Console.WriteLine("Transform:");
            Console.WriteLine("scale: " + scale.ToString("0.00"));
            Console.WriteLine($"rotation: (x: {rollX:##.00} y: {pitchY:##.00} z: {yawZ:##.00})");
            Console.WriteLine("translation: " + translation.ToString("0.00"));
        }
        else
        {
            Console.WriteLine("Failed to decompose matrix!");
        }

        return result;
    }
}
