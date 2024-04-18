namespace CadRevealComposer.Tests.AlgebraExtensions;

using CadRevealComposer.AlgebraExtensions;
using CadRevealComposer.Utils;
using System.Numerics;

[TestFixture]
public class RayTests
{
    [Test]
    public void RaycastInZHitBackFace()
    {
        var rayOrigin = new Vector3(0, 0, 0);
        var rayDirection = new Vector3(0, 0, 1);
        var triangle = new Triangle(new Vector3(-5, -5, 2), new Vector3(5, 0, 2), new Vector3(0, 5, 2));

        var ray = new Ray(rayOrigin, rayDirection);
        var hitResult = ray.Trace(triangle, out var intersectionPoint, out var isFrontFace);
        LogResult(hitResult, intersectionPoint, isFrontFace);

        Assert.That(intersectionPoint.EqualsWithinTolerance(new Vector3(0, 0, 2), 0.00001f));
        Assert.That(hitResult);
        Assert.That(isFrontFace, Is.False);
    }

    [Test]
    public void RaycastInZHitFrontFace()
    {
        var rayOrigin = new Vector3(0, 0, 0);
        var rayDirection = new Vector3(0, 0, 1);
        var triangle = new Triangle(new Vector3(0, 5, 2), new Vector3(5, 0, 2), new Vector3(-5, -5, 2));

        var ray = new Ray(rayOrigin, rayDirection);
        var hitResult = ray.Trace(triangle, out var intersectionPoint, out var isFrontFace);
        LogResult(hitResult, intersectionPoint, isFrontFace);

        Assert.That(intersectionPoint.EqualsWithinTolerance(new Vector3(0, 0, 2), 0.00001f));
        Assert.That(hitResult);
        Assert.That(isFrontFace);
    }

    [Test]
    public void RaycastInZMiss()
    {
        var rayOrigin = new Vector3(10, 0, 0);
        var rayDirection = new Vector3(0, 0, 1);
        var triangle = new Triangle(new Vector3(-5, -5, 2), new Vector3(5, 0, 2), new Vector3(0, 5, 2));

        var ray = new Ray(rayOrigin, rayDirection);
        var hitResult = ray.Trace(triangle, out var intersectionPoint, out var isFrontFace);
        LogResult(hitResult, intersectionPoint, isFrontFace);

        Assert.That(!hitResult);
    }

    [Test]
    public void RaycastInXFrontFace()
    {
        var rayOrigin = new Vector3(-10, 0, 0);
        var rayDirection = new Vector3(1, 0, 0);
        var triangle = new Triangle(new Vector3(0, -5, -2), new Vector3(0, 0, 2), new Vector3(0, 5, -2));

        var ray = new Ray(rayOrigin, rayDirection);
        var hitResult = ray.Trace(triangle, out var intersectionPoint, out var isFrontFace);
        LogResult(hitResult, intersectionPoint, isFrontFace);

        Assert.That(intersectionPoint.EqualsWithinTolerance(new Vector3(0, 0, 0), 0.00001f));
        Assert.That(hitResult);
        Assert.That(isFrontFace);
    }

    private static void LogResult(bool isHit, Vector3 hitPosition, bool isFrontFace)
    {
        Console.Write(isHit ? "Hit! " : "Miss! ");
        if (isHit)
            Console.Write($"Hit position: {hitPosition.ToString("G4")} " + (isFrontFace ? "front face" : "back face"));
        Console.WriteLine();
    }
}
