namespace CadRevealComposer.AlgebraExtensions
{
    using System;
    using System.Numerics;
    using Utils;

    public readonly record struct Ray(Vector3 Origin, Vector3 Direction)
    {
        public bool Trace(in Triangle triangle, out Vector3 intersectionPoint, out bool isFrontFace)
        {
            const float tolerance = 0.000_01f;

            intersectionPoint = Vector3.Zero;
            isFrontFace = false;

            var v1v2 = triangle.V2 - triangle.V1;
            var v1v3 = triangle.V3 - triangle.V1;
            var v2v3 = triangle.V3 - triangle.V2;
            var v3v1 = triangle.V1 - triangle.V3;
            var planeNormal = Vector3.Cross(v1v2, v1v3);
            if (planeNormal.LengthSquared() < 0.000_000_0001f) // NOTE: arbitrary value, works fine for now
            {
                return false; // Triangle is too small for raycast
            }
            planeNormal = Vector3.Normalize(planeNormal);

            // Plane formula
            // a * x + b * y + c * z = d
            // where plane normal n = [a b c]T
            // n * [x y z]T = d
            // Ray(t) = Ro + t * Rd
            // t = (d - n * Ro)/(n * Rd)
            if (Vector3.Dot(planeNormal, Direction).ApproximatelyEquals(0))
            {
                return false;
            }

            var t = (Vector3.Dot(planeNormal, triangle.V1) - Vector3.Dot(planeNormal, Origin)) / Vector3.Dot(planeNormal, Direction);
            if (t < 0)
            {
                return false;
            }

            intersectionPoint = Origin + Direction * t;
            isFrontFace = planeNormal.AngleTo(Direction) > Math.PI / 2;

            // Triangle test
            var v1pi = intersectionPoint - triangle.V1;
            var v2pi = intersectionPoint - triangle.V2;
            var v3pi = intersectionPoint - triangle.V3;

            // Check if intersection point is on any corner
            if (v1pi.EqualsWithinTolerance(Vector3.Zero, tolerance) ||
                v2pi.EqualsWithinTolerance(Vector3.Zero, tolerance) ||
                v3pi.EqualsWithinTolerance(Vector3.Zero, tolerance))
            {
                return true;
            }

            var v1v2Crossv1pi = Vector3.Cross(v1v2, v1pi);
            var v2v3Crossv2pi = Vector3.Cross(v2v3, v2pi);
            var v3v1Crossv3pi = Vector3.Cross(v3v1, v3pi);

            // Check if intersection point is on any of the sides

            // TODO: ApproximatelyEquals with 0
            // TODO: ApproximatelyEquals with 0
            // TODO: ApproximatelyEquals with 0

            // TODO: rename ApproximatelyEquals
            // TODO: rename ApproximatelyEquals
            // TODO: rename ApproximatelyEquals

            if (v1v2Crossv1pi.EqualsWithinTolerance(Vector3.Zero, tolerance))
            {
                if ((triangle.V2 - intersectionPoint).AngleTo(v1v2).ApproximatelyEquals(0) &&
                    v1pi.AngleTo(v1v2).ApproximatelyEquals(0))
                {
                    return true;
                }
            }
            if (v2v3Crossv2pi.EqualsWithinTolerance(Vector3.Zero, tolerance))
            {
                if ((triangle.V3 - intersectionPoint).AngleTo(v2v3).ApproximatelyEquals(0) &&
                    v2pi.AngleTo(v2v3).ApproximatelyEquals(0))
                {
                    return true;
                }
            }
            if (v3v1Crossv3pi.EqualsWithinTolerance(Vector3.Zero, tolerance))
            {
                if ((triangle.V1 - intersectionPoint).AngleTo(v3v1).ApproximatelyEquals(0) &&
                    v3pi.AngleTo(v3v1).ApproximatelyEquals(0))
                {
                    return true;
                }
            }

            var aboveV1V2 = Vector3.Dot(Vector3.Normalize(v1v2Crossv1pi), planeNormal) >= 0.0;
            var aboveV2V3 = Vector3.Dot(Vector3.Normalize(v2v3Crossv2pi), planeNormal) >= 0.0;
            var aboveV3V1 = Vector3.Dot(Vector3.Normalize(v3v1Crossv3pi), planeNormal) >= 0.0;

            return aboveV1V2 && aboveV2V3 && aboveV3V1;
        }
    }
}