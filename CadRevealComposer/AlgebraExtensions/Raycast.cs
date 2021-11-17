namespace CadRevealComposer.AlgebraExtensions
{
    using System;
    using System.Numerics;
    using Utils;

    public static class Raycasting
    {
        public record Ray(Vector3 Origin, Vector3 Direction);

        public record Triangle(Vector3 V1, Vector3 V2, Vector3 V3);

        public record Bounds(Vector3 Min, Vector3 Max);

        public static bool Raycast(Ray ray, Triangle triangle, out Vector3 intersectionPoint, out bool isFrontFace)
        {
            intersectionPoint = Vector3.Zero;
            isFrontFace = false;
            var v1v2 = triangle.V2 - triangle.V1;
            var v1v3 = triangle.V3 - triangle.V1;
            var v2v3 = triangle.V3 - triangle.V2;
            var v3v1 = triangle.V1 - triangle.V3;
            var planeNormal = Vector3.Cross(v1v2, v1v3);
            if (planeNormal.LengthSquared() < 0.00001f) // TODO: arbitrary value
                return false; // Triangle is too small for raycast
            planeNormal = Vector3.Normalize(planeNormal);

            // Plane formula
            // a * x + b * y + c * z = d
            // where plane normal n = [a b c]T
            // n * [x y z]T = d
            // Ray(t) = Ro + t * Rd
            // t = (d - n * Ro)/(n * Rd)
            var d = Vector3.Dot(planeNormal, triangle.V1);
            if (Vector3.Dot(planeNormal, ray.Direction).ApproximatelyEquals(0))
                return false;
            var t =
                (d - Vector3.Dot(planeNormal, ray.Origin)) / Vector3.Dot(planeNormal, ray.Direction);
            if (t < 0)
                return false;

            intersectionPoint = ray.Origin + ray.Direction * t;
            isFrontFace = planeNormal.AngleTo(ray.Direction) < Math.PI;

            // Triangle test
            var v1pi = intersectionPoint - triangle.V1;
            var v2pi = intersectionPoint - triangle.V2;
            var v3pi = intersectionPoint - triangle.V3;
            var aboveV1V2 = (Vector3.Dot(Vector3.Normalize(Vector3.Cross(v1v2, v1pi)), planeNormal) >= 0.0);
            var aboveV2V3 = (Vector3.Dot(Vector3.Normalize(Vector3.Cross(v2v3, v2pi)), planeNormal) >= 0.0);
            var aboveV3V1 = (Vector3.Dot(Vector3.Normalize(Vector3.Cross(v3v1, v3pi)), planeNormal) >= 0.0);



            return aboveV1V2 && aboveV2V3 && aboveV3V1;
        }
    }
}