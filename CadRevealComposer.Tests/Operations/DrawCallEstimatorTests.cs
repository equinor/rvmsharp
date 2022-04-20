namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using NUnit.Framework;
using RvmSharp.Primitives;
using System.Drawing;
using System.Numerics;

[TestFixture]
public class DrawCallEstimatorTests
{
    [Test]
    public void ThreeBoxesCost()
    {
        var geometry = new APrimitive[]
        {
            new Box(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.UnitZ, 1, 1, 1, 0),
            new Box(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.UnitZ, 1, 1, 1, 0),
            new Box(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.UnitZ, 1, 1, 1, 0),
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.AreEqual(1, estimatedDrawCalls);
        Assert.AreEqual(12 * 3, estimatedTriangleCount);
    }

    [Test]
    public void ConeAndCylinder()
    {
        var geometry = new APrimitive[]
        {
            new ClosedCone(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.UnitZ, 10, 5, 5),
            new ClosedCylinder(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.UnitY, 10, 5),
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.AreEqual(2, estimatedDrawCalls); // circle and cone segment
        Assert.AreEqual(4 * 2 + 2 * 4, estimatedTriangleCount); // 4 (2 tris) circles and 2 (4 tris) cone segments
    }

    [Test]
    public void SolidClosedGeneralConeTorusAndClosedCylinder()
    {
        var geometry = new APrimitive[]
        {
            new SolidClosedGeneralCone(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.One, 13, 3, 5, 0.3f, 1.5f, 0.1f, 0.0f, 0.0f, 0.1f, 0.2f),
            new Torus(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.UnitY, 10, 5),
            new ClosedCylinder(
                new CommonPrimitiveProperties(1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 1.0f, new RvmBoundingBox(-Vector3.One, Vector3.One), Color.Blue, (Vector3.UnitZ, 0), null!),
                Vector3.UnitY, 10, 5)
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.AreEqual(5, estimatedDrawCalls); // circle, cone, ring segment, sloped cone, torus
        Assert.AreEqual((2 * 2 + 4 * 2) + (120) + (2 * 2 + 4), estimatedTriangleCount);
    }
}