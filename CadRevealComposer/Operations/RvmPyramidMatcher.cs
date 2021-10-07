﻿namespace CadRevealComposer.Operations
{
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Numerics;
    using Utils;

    public static class RvmPyramidMatcher
    {
        private enum PyramidVariation
        {
            Original,
            RotatedZ90,
            RotatedZ180,
            RotatedZ270,
            RotatedX180,
            RotatedX180Z90,
            RotatedX180Z180,
            RotatedX180Z270
        }

        public static bool Match(RvmPyramid pyramidA, RvmPyramid pyramidB, out Matrix4x4 transform)
        {
            transform = Matrix4x4.Identity;
            foreach (var variation in Enum.GetValues<PyramidVariation>())
            {
                var rotatedPyramidB = RotatePyramid(pyramidB, variation);
                if (!ExtractPossibleScale(pyramidA, rotatedPyramidB, out var scale))
                    continue;

                transform = Matrix4x4Helpers.CalculateTransformMatrix(Vector3.Zero, GetRotation(variation), scale);
                return true;
            }

            return false;
        }

        private static Quaternion GetRotation(PyramidVariation pyramidVariation)
        {
            switch (pyramidVariation)
            {
                case PyramidVariation.Original:
                    return Quaternion.Identity;
                case PyramidVariation.RotatedZ90:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2);
                case PyramidVariation.RotatedZ180:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI);
                case PyramidVariation.RotatedZ270:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 3 * MathF.PI / 2);
                case PyramidVariation.RotatedX180:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
                case PyramidVariation.RotatedX180Z90:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
                case PyramidVariation.RotatedX180Z180:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
                case PyramidVariation.RotatedX180Z270:
                    return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 3 * MathF.PI / 2) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
                default:
                    throw new ArgumentOutOfRangeException(nameof(pyramidVariation), pyramidVariation, null);
            }
        }

        private static bool ExtractPossibleScale(RvmPyramid a, RvmPyramid b, out Vector3 aToBScale)
        {
            const float threshold = 0.001f;

            var possibleX = a.BottomX == 0 ? 1 : b.BottomX / a.BottomX;
            var possibleY = a.BottomY == 0 ? 1 : b.BottomY / a.BottomY;
            var possibleZ = a.Height == 0 ? 1 : b.Height / a.Height;
            aToBScale = new Vector3(possibleX, possibleY, possibleZ);

            var scaledA = ScalePyramid(a, aToBScale);

            return (scaledA.BottomX).ApproximatelyEquals(b.BottomX, threshold) &&
                   (scaledA.BottomY).ApproximatelyEquals(b.BottomY, threshold) &&
                   (scaledA.TopX).ApproximatelyEquals(b.TopX, threshold) &&
                   (scaledA.TopY).ApproximatelyEquals(b.TopY, threshold) &&
                   (scaledA.OffsetX).ApproximatelyEquals(b.OffsetX, threshold) &&
                   (scaledA.OffsetY).ApproximatelyEquals(b.OffsetY, threshold) &&
                   (scaledA.Height).ApproximatelyEquals(b.Height, threshold);
        }

        private static RvmPyramid ScalePyramid(RvmPyramid pyramid, Vector3 scale)
        {
            return pyramid with
            {
                BoundingBoxLocal = new RvmBoundingBox(pyramid.BoundingBoxLocal.Min * scale, pyramid.BoundingBoxLocal.Max * scale),
                BottomX = pyramid.BottomX * scale.X,
                BottomY = pyramid.BottomY * scale.Y,
                TopX = pyramid.TopX * scale.X,
                TopY = pyramid.TopY * scale.Y,
                OffsetX = pyramid.OffsetX * scale.X,
                OffsetY = pyramid.OffsetY * scale.Y,
                Height = pyramid.Height * scale.Z
            };
        }

        private static RvmPyramid RotatePyramid(RvmPyramid pyramid,
            PyramidVariation variation)
        {
            switch (variation)
            {
                case PyramidVariation.Original:
                    return pyramid;
                case PyramidVariation.RotatedZ90:
                    return pyramid with {
                        BottomX = pyramid.BottomY,
                        BottomY = pyramid.BottomX,
                        TopX = pyramid.TopY,
                        TopY = pyramid.TopX,
                        OffsetX = -pyramid.OffsetY,
                        OffsetY = pyramid.OffsetX,
                    };
                case PyramidVariation.RotatedZ180:
                    return pyramid with {
                        OffsetX = -pyramid.OffsetX,
                        OffsetY = -pyramid.OffsetY,
                    };
                case PyramidVariation.RotatedZ270:
                    return pyramid with {
                        BottomX = pyramid.BottomY,
                        BottomY = pyramid.BottomX,
                        TopX = pyramid.TopY,
                        TopY = pyramid.TopX,
                        OffsetX = pyramid.OffsetY,
                        OffsetY = -pyramid.OffsetX,
                    };
                case PyramidVariation.RotatedX180:
                    return pyramid with {
                        BottomX = pyramid.TopX,
                        BottomY = pyramid.TopY,
                        TopX = pyramid.BottomX,
                        TopY = pyramid.BottomY,
                        OffsetX = pyramid.OffsetX,
                        OffsetY = - pyramid.OffsetY,
                    };
                case PyramidVariation.RotatedX180Z90:
                    return pyramid with {
                        BottomX = pyramid.TopY,
                        BottomY = pyramid.TopX,
                        TopX = pyramid.BottomY,
                        TopY = pyramid.BottomX,
                        OffsetX = pyramid.OffsetY,
                        OffsetY = pyramid.OffsetX,
                    };
                case PyramidVariation.RotatedX180Z180:
                    return pyramid with {
                        BottomX = pyramid.TopX,
                        BottomY = pyramid.TopY,
                        TopX = pyramid.BottomX,
                        TopY = pyramid.BottomY,
                        OffsetX = - pyramid.OffsetX,
                        OffsetY = pyramid.OffsetY,
                    };
                case PyramidVariation.RotatedX180Z270:
                    return pyramid with {
                        BottomX = pyramid.TopY,
                        BottomY = pyramid.TopX,
                        TopX = pyramid.BottomY,
                        TopY = pyramid.BottomX,
                        OffsetX = - pyramid.OffsetY,
                        OffsetY = - pyramid.OffsetX,
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(variation), variation, null);
            }
        }


    }
}