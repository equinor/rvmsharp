namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class RvmBoxConverterTests
{
    const int _treeIndex = 1337;
    private RvmBox _rvmBox = null!;

    [SetUp]
    public void Setup()
    {
        _rvmBox = new RvmBox(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            LengthX: 1,
            LengthY: 1,
            LengthZ: 1
        );
    }

    [Test]
    public void RvmBoxConverter_ReturnsBox()
    {
        var geometries = _rvmBox.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<Box>());
        Assert.That(geometries.Length, Is.EqualTo(1));
    }
}
