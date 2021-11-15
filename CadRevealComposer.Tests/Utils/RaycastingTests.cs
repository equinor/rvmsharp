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
            var ray = new Raycasting.Ray(rayOrigin, rayDirection);

            var triangle = new Raycasting.Triangle(
                new Vector3(-5, -5, 2),
                new Vector3(5, 0, 2),
                new Vector3(0, 5, 2)
            );

            var result = Raycasting.Raycast(ray, triangle, out var intersectionPoint);

            Console.WriteLine($"Intersection: {result}");
            if (result)
                Console.WriteLine($"Intersection point: {intersectionPoint.ToString("G4")}");

        }
    }
}