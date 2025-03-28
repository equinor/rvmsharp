namespace CadRevealComposer.Tests.Operations;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Operations;
using NUnit.Framework.Legacy;
using Primitives;

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
            new Box(Matrix4x4.Identity, int.MaxValue, Color.Red, new BoundingBox(-Vector3.One, Vector3.One)),
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.That(estimatedDrawCalls, Is.EqualTo(1));
        Assert.That(estimatedTriangleCount, Is.EqualTo(12 * 3));
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
                new BoundingBox(-Vector3.One, Vector3.One)
            ),
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.That(estimatedDrawCalls, Is.EqualTo(3)); // 2x circle and 1x cone segment
        Assert.That(estimatedTriangleCount, Is.EqualTo((4 * 2) + (1 * 4))); // 4 (2 tris) circles and 1 (4 tris) cone segments
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
                new BoundingBox(-Vector3.One, Vector3.One)
            ),
        };
        (long estimatedTriangleCount, int estimatedDrawCalls) = DrawCallEstimator.Estimate(geometry);
        Assert.That(estimatedDrawCalls, Is.EqualTo(4)); // circle, cone, ring segment, torus
        Assert.That(estimatedTriangleCount, Is.EqualTo(132));
    }
}
