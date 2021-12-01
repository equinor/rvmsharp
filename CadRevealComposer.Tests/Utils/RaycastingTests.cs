namespace CadRevealComposer.Tests.Utils
{
    using AlgebraExtensions;
    using NUnit.Framework;
    using System;
    using System.Numerics;

    [TestFixture]
    public class RaycastingTests
    {
        [Test]
        public void RaycastInZHitFrontFace()
        {
            var rayOrigin = new Vector3(0, 0, 0);
            var rayDirection = new Vector3(0, 0, 1);
            var triangle = new Triangle(
                new Vector3(-5, -5, 2),
                new Vector3(5, 0, 2),
                new Vector3(0, 5, 2)
            );

            var ray = new Ray(rayOrigin, rayDirection);
            var hitResult = ray.Trace(triangle, out var intersectionPoint, out var isFrontFace);
            LogResult(hitResult, intersectionPoint, isFrontFace);

            Assert.That(hitResult);
            Assert.True(isFrontFace);
        }

        [Test]
        public void RaycastInZHitBackFace()
        {
            var rayOrigin = new Vector3(0, 0, 0);
            var rayDirection = new Vector3(0, 0, 1);
            var triangle = new Triangle(
                new Vector3(0, 5, 2),
                new Vector3(5, 0, 2),
                new Vector3(-5, -5, 2)
            );

            var ray = new Ray(rayOrigin, rayDirection);
            var hitResult = ray.Trace(triangle, out var intersectionPoint, out var isFrontFace);
            LogResult(hitResult, intersectionPoint, isFrontFace);

            Assert.That(hitResult);
            Assert.False(isFrontFace);
        }

        [Test]
        public void RaycastInZMiss()
        {
            var rayOrigin = new Vector3(10, 0, 0);
            var rayDirection = new Vector3(0, 0, 1);
            var triangle = new Triangle(
                new Vector3(-5, -5, 2),
                new Vector3(5, 0, 2),
                new Vector3(0, 5, 2)
            );

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
            var triangle = new Triangle(
                new Vector3(0, -5, -2),
                new Vector3(0, 0, 2),
                new Vector3(0, 5, -2)
            );

            var ray = new Ray(rayOrigin, rayDirection);
            var hitResult = ray.Trace(triangle, out var intersectionPoint, out var isFrontFace);
            LogResult(hitResult, intersectionPoint, isFrontFace);

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
}