namespace CadRevealComposer.Operations;

using Primitives;
using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

public static class CameraPositioning
{
    public record CameraPosition([property: JsonPropertyName("cameraPosition")]
        SerializableVector3 Position,
        [property: JsonPropertyName("cameraTarget")]
        SerializableVector3 Target,
        [property: JsonPropertyName("cameraDirection")]
        SerializableVector3 Direction);

    public static CameraPosition CalculateInitialCamera(APrimitive[] geometries)
    {
        static float PythagorasTan(float oppositeLeg, float angleRad) => oppositeLeg / MathF.Tan(angleRad);
        static float DegToRad(float degree) => MathF.PI / 180f * degree;

        // Camera looks towards platform center, with tilt down.
        // The camera is positioned such that the longest side X or Y is in view.

        const float cameraVerticalFieldOfViewDeg = 45f;
        const float additionalCameraDistanceFactor = 1.1f;
        const float cameraVerticalHorizonAngleDeg = 30f;

        var (boundingBoxMin, boundingBoxMax) = GetPlatformBoundingBox(geometries);
        var platformSides = boundingBoxMax - boundingBoxMin;
        var platformCenter = boundingBoxMin + (platformSides / 2);

        Vector3 dir;
        float cameraDistance;
        var xLongerThanY = platformSides.X > platformSides.Y;
        if (xLongerThanY)
        {
            var platformYzPlaneMin =
                new Vector3(boundingBoxMin.X + (platformSides.X / 2), boundingBoxMin.Y, boundingBoxMin.Z);
            var distanceCenterToYzPlaneMin = (platformCenter - platformYzPlaneMin).Length();
            cameraDistance = additionalCameraDistanceFactor *
                             PythagorasTan(distanceCenterToYzPlaneMin, DegToRad(cameraVerticalFieldOfViewDeg) / 2);
            dir = Vector3.Normalize(new Vector3(0, -MathF.Cos(DegToRad(cameraVerticalHorizonAngleDeg)),
                MathF.Sin(DegToRad(cameraVerticalHorizonAngleDeg))));
        }
        else
        {
            var platformXzPlaneMin =
                new Vector3(boundingBoxMin.X, boundingBoxMin.Y + (platformSides.Y / 2), boundingBoxMin.Z);
            var distanceCenterToYzPlaneMin = (platformCenter - platformXzPlaneMin).Length();
            cameraDistance = additionalCameraDistanceFactor *
                             PythagorasTan(distanceCenterToYzPlaneMin, DegToRad(cameraVerticalFieldOfViewDeg) / 2);
            dir = Vector3.Normalize(new Vector3(-MathF.Cos(DegToRad(cameraVerticalHorizonAngleDeg)), 0,
                MathF.Sin(DegToRad(cameraVerticalHorizonAngleDeg))));
        }

        var position = platformCenter + dir * cameraDistance;
        var direction = Vector3.Negate(dir);

        return new CameraPosition(SerializableVector3.FromVector3(position),
            SerializableVector3.FromVector3(platformCenter),
            SerializableVector3.FromVector3(direction));
    }

    private static (Vector3 PlatformBoundingBoxMin, Vector3 PlatformBoundingBoxMax) GetPlatformBoundingBox(
        APrimitive[] geometries)
    {
        // TODO: does not handle empty geometries correctly
        if (geometries.Length == 0)
        {
            throw new Exception(
                "The input data had 0 elements, we cannot position the camera. Does the 3D scene have any valid meshes?");
        }

        // get bounding box for platform using approximation (99th percentile)
        const double percentile = 0.01;
        var platformMinX = geometries.Select(node => node.AxisAlignedBoundingBox.Min.X).OrderBy(x => x)
            .Skip((int)(percentile * geometries.Length)).First();
        var platformMinY = geometries.Select(node => node.AxisAlignedBoundingBox.Min.Y).OrderBy(x => x)
            .Skip((int)(percentile * geometries.Length)).First();
        var platformMinZ = geometries.Select(node => node.AxisAlignedBoundingBox.Min.Z).OrderBy(x => x)
            .Skip((int)(percentile * geometries.Length)).First();
        var platformMaxX = geometries.Select(node => node.AxisAlignedBoundingBox.Max.X).OrderByDescending(x => x)
            .Skip((int)(percentile * geometries.Length)).First();
        var platformMaxY = geometries.Select(node => node.AxisAlignedBoundingBox.Max.Y).OrderByDescending(x => x)
            .Skip((int)(percentile * geometries.Length)).First();
        var platformMaxZ = geometries.Select(node => node.AxisAlignedBoundingBox.Max.Z).OrderByDescending(x => x)
            .Skip((int)(percentile * geometries.Length)).First();

        var bbMin = new Vector3(platformMinX, platformMinY, platformMinZ);
        var bbMax = new Vector3(platformMaxX, platformMaxY, platformMaxZ);
        return (bbMin, bbMax);
    }
}