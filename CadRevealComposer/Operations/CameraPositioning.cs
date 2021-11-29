namespace CadRevealComposer.Operations
{
    using Primitives;
    using System;
    using System.Linq;
    using System.Numerics;

    public static class CameraPositioning
    {
        public static (Vector3 cameraPosition, Vector3 cameraDirection) CalculateInitialCamera(APrimitive[] geometries)
        {
            // Camera looks towards platform center, with tilt down.
            // The camera is positioned such that the longest side X or Y is in view.

            const float cameraFieldOfViewDeg = 45f;
            const float additionalCameraDistanceFactor = 1.1f;
            const float cameraHorizonAngleDeg = 30f;

            var (boundingBoxMin, boundingBoxMax) = GetPlatformBoundingBox(geometries);
            var platformSides = boundingBoxMax - boundingBoxMin;
            var platformCenter = boundingBoxMin + (platformSides / 2);

            Vector3 dir; float cameraDistance;
            var xLongerThanY = platformSides.X > platformSides.Y;
            if (xLongerThanY)
            {
                var platformYzPlaneMin = new Vector3(boundingBoxMin.X + (platformSides.X / 2), boundingBoxMin.Y, boundingBoxMin.Z);
                var distanceCenterToYzPlaneMin = (platformCenter - platformYzPlaneMin).Length();
                cameraDistance = additionalCameraDistanceFactor * PythagorasTan(distanceCenterToYzPlaneMin, DegToRad(cameraFieldOfViewDeg) / 2);
                dir = Vector3.Normalize(new Vector3(0, -MathF.Cos(DegToRad(cameraHorizonAngleDeg)), MathF.Sin(DegToRad(cameraHorizonAngleDeg))));
            }
            else
            {
                var platformXzPlaneMin = new Vector3(boundingBoxMin.X, boundingBoxMin.Y + (platformSides.Y / 2), boundingBoxMin.Z);
                var distanceCenterToYzPlaneMin = (platformCenter - platformXzPlaneMin).Length();
                cameraDistance = additionalCameraDistanceFactor * PythagorasTan(distanceCenterToYzPlaneMin, DegToRad(cameraFieldOfViewDeg) / 2);
                dir = Vector3.Normalize(new Vector3(-MathF.Cos(DegToRad(cameraHorizonAngleDeg)), 0, MathF.Sin(DegToRad(cameraHorizonAngleDeg))));
            }

            var position = platformCenter + dir * cameraDistance;
            var direction = Vector3.Negate(dir);

            return (position, direction);
        }

        private static float PythagorasTan(float oppositeLeg, float angleRad) => oppositeLeg / MathF.Tan(angleRad);
        private static float DegToRad(float degree) => MathF.PI / 180f * degree;

        private static (Vector3 PlatformBoundingBoxMin, Vector3 PlatformBoundingBoxMax) GetPlatformBoundingBox(APrimitive[] geometries)
        {
            // get bounding box for platform using approximation (99th percentile)
            var percentile = 0.01;
            var platformMinX = geometries.Select(node => node.AxisAlignedBoundingBox.Min.X).OrderBy(x => x).Skip((int)(percentile * geometries.Length)).First();
            var platformMinY = geometries.Select(node => node.AxisAlignedBoundingBox.Min.Y).OrderBy(x => x).Skip((int)(percentile * geometries.Length)).First();
            var platformMinZ = geometries.Select(node => node.AxisAlignedBoundingBox.Min.Z).OrderBy(x => x).Skip((int)(percentile * geometries.Length)).First();
            var platformMaxX = geometries.Select(node => node.AxisAlignedBoundingBox.Max.X).OrderByDescending(x => x).Skip((int)(percentile * geometries.Length)).First();
            var platformMaxY = geometries.Select(node => node.AxisAlignedBoundingBox.Max.Y).OrderByDescending(x => x).Skip((int)(percentile * geometries.Length)).First();
            var platformMaxZ = geometries.Select(node => node.AxisAlignedBoundingBox.Max.Z).OrderByDescending(x => x).Skip((int)(percentile * geometries.Length)).First();

            var bbMin = new Vector3(platformMinX, platformMinY, platformMinZ);
            var bbMax = new Vector3(platformMaxX, platformMaxY, platformMaxZ);
            return (bbMin, bbMax);
        }
    }
}