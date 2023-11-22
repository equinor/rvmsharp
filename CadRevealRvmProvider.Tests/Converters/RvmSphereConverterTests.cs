namespace CadRevealRvmProvider.Tests.Converters;

using CadRevealComposer.Primitives;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

public class RvmSphereConverterTests
{
    const int _treeIndex = 1337;
    private RvmSphere _rvmSphere = null!;

    [SetUp]
    public void Setup()
    {
        _rvmSphere = new RvmSphere(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Radius: 1
        );
    }

    [Test]
    public void RvmSphereConverter_ReturnsEllipsoidSegment()
    {
        var geometries = _rvmSphere.ConvertToRevealPrimitive(_treeIndex, Color.Red, "HA").ToArray();

        Assert.That(geometries[0], Is.TypeOf<EllipsoidSegment>());
        Assert.That(geometries.Length, Is.EqualTo(1));
    }
}
