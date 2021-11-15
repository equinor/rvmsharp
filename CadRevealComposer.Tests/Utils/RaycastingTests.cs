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
        public void SimpleRayCast()
        {
            var rayOrigin = new Vector3(0, 0, 0);
            var rayDirection = new Vector3(0, 0, 1);
            var triangle = new Raycasting.Triangle(
                new Vector3(-5, -5, 2),
                new Vector3(5, 0, 2),
                new Vector3(0, 5, 2)
            );

            var ray = new Raycasting.Ray(rayOrigin, rayDirection);
            var hitResult = Raycasting.Raycast(ray, triangle, out var intersectionPoint);
            LogResult(hitResult, intersectionPoint);

            Assert.That(hitResult);
        }

        [Test]
        public void SimpleRayCast2()
        {
            var rayOrigin = new Vector3(10, 0, 0);
            var rayDirection = new Vector3(0, 0, 1);
            var triangle = new Raycasting.Triangle(
                new Vector3(-5, -5, 2),
                new Vector3(5, 0, 2),
                new Vector3(0, 5, 2)
            );

            var ray = new Raycasting.Ray(rayOrigin, rayDirection);
            var hitResult = Raycasting.Raycast(ray, triangle, out var intersectionPoint);
            LogResult(hitResult, intersectionPoint);

            Assert.That(!hitResult);
        }

        private static void LogResult(bool isHit, Vector3 hitPosition)
        {
            Console.Write(isHit ? "Hit! " : "Miss! ");
            if (isHit)
                Console.WriteLine($"Hit position: {hitPosition.ToString("G4")}");
        }
    }
}