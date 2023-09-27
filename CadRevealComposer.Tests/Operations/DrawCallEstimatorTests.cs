namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations;
using Primitives;
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
            new Box(Matrix4x4.Identity, int.MaxValue, Color.Red, new BoundingBox(-Vector3.One, Vector3.One)),
            new Box(Matrix4x4.Identity, int.MaxValue, Color.Red, new BoundingBox(-Vector3.One, Vector3.One)),
            new Box(Matrix4x4.Identity, int.MaxValue, Color.Red, new BoundingBox(-Vector3.One, Vector3.One))
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
            new Cone(
                0f,
                0f,
                Vector3.One,
                Vector3.One,
                Vector3.One,
                0f,
                0f,
                int.MaxValue,
                Color.Red,
                new BoundingBox(-Vector3.One, Vector3.One)
            ),
            new GeneralCylinder(
                0f,
                0f,
                Vector3.One,
                Vector3.One,
                Vector3.One,
                Vector4.One,
                Vector4.One,
                0f,
                int.MaxValue,
                Color.Red,
                new BoundingBox(-Vector3.One, Vector3.One),
                Quaternion.Identity
            )
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.AreEqual(3, estimatedDrawCalls); // 2x circle and 1x cone segment
        Assert.AreEqual((4 * 2) + (1 * 4), estimatedTriangleCount); // 4 (2 tris) circles and 1 (4 tris) cone segments
    }

    [Test]
    public void SolidClosedGeneralConeTorusAndClosedCylinder()
    {
        var geometry = new APrimitive[]
        {
            new Cone(
                0f,
                0f,
                Vector3.One,
                Vector3.One,
                Vector3.One,
                0f,
                0f,
                int.MaxValue,
                Color.Red,
                new BoundingBox(-Vector3.One, Vector3.One)
            ),
            new TorusSegment(
                0f,
                Matrix4x4.Identity,
                0f,
                0f,
                int.MaxValue,
                Color.Red,
                new BoundingBox(-Vector3.One, Vector3.One)
            ),
            new GeneralCylinder(
                0f,
                0f,
                Vector3.One,
                Vector3.One,
                Vector3.One,
                Vector4.One,
                Vector4.One,
                0f,
                int.MaxValue,
                Color.Red,
                new BoundingBox(-Vector3.One, Vector3.One),
                Quaternion.Identity
            )
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.AreEqual(4, estimatedDrawCalls); // circle, cone, ring segment, torus
        Assert.AreEqual(132, estimatedTriangleCount);
    }
}
