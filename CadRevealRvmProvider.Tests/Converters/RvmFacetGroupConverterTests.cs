﻿namespace CadRevealRvmProvider.Tests.Converters;

using System.Drawing;
using System.Numerics;
using CadRevealRvmProvider.Converters;
using RvmSharp.Primitives;

public class RvmFacetGroupConverterTests
{
    const int TreeIndex = 1337;
    private RvmFacetGroup _rvmFacetGroup = null!;

    [SetUp]
    public void Setup()
    {
        _rvmFacetGroup = new RvmFacetGroup(
            Version: 2,
            Matrix: Matrix4x4.Identity,
            BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
            Polygons: null!
        );
    }

    [Test]
    public void RvmFacetGroupConverter_ReturnsProtoMeshFromFacetGroup()
    {
        var logObject = new FailedPrimitivesLogObject();
        var geometries = _rvmFacetGroup.ConvertToRevealPrimitive(TreeIndex, Color.Red, logObject).ToArray();

        Assert.That(geometries[0], Is.TypeOf<ProtoMeshFromFacetGroup>());
        Assert.That(geometries.Length, Is.EqualTo(1));
    }
}
