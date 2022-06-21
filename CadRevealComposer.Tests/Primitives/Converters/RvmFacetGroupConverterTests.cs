namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System.Drawing;
using System.Linq;
using System.Numerics;

public class RvmFacetGroupConverterTests
{
    const int _treeIndex = 1337;
    private RvmFacetGroup _rvmFacetGroup;

    [SetUp]
    public void Setup()
    {
        _rvmFacetGroup = new RvmFacetGroup(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Polygons: null
        );
    }

    [Test]
    public void RvmFacetGroupConverter_ReturnsProtoMeshFromFacetGroup()
    {
        var geometries = _rvmFacetGroup.ConvertToRevealPrimitive(_treeIndex, Color.Red).ToArray();

        Assert.That(geometries[0], Is.TypeOf<ProtoMeshFromFacetGroup>());
        Assert.That(geometries.Length, Is.EqualTo(1));
    }
}
