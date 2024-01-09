namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class RvmPyramidConverterTests
{
    private const int _treeIndex = 1337;
    private static RvmPyramid _rvmPyramid = null!;

    [SetUp]
    public void Setup()
    {
        _rvmPyramid = new RvmPyramid(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            BottomX: 1,
            BottomY: 2,
            TopX: 3,
            TopY: 4,
            OffsetX: 5,
            OffsetY: 6,
            Height: 7
        );
    }

    [Test]
    public void RvmPyramidConverter_WhenTopAndBottomIsEqualAndNoOffset_IsBox()
    {
        var pyramid = _rvmPyramid with { BottomX = 1, BottomY = 2, TopX = 1, TopY = 2, OffsetX = 0, OffsetY = 0, };

        var logObject = new FailedPrimitivesLogObject();
        var geometries = pyramid.ConvertToRevealPrimitive(_treeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Box>());
        Assert.That(geometries.Length, Is.EqualTo(1));
    }

    [Test]
    public void RvmPyramidConverter_WhenNotBoxShaped_ReturnsProtoMeshFromPyramid()
    {
        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmPyramid.ConvertToRevealPrimitive(_treeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<ProtoMeshFromRvmPyramid>());
        Assert.That(geometries.Length, Is.EqualTo(1));
    }
}
