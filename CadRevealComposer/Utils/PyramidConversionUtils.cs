namespace CadRevealComposer.Utils
{
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    public static class PyramidConversionUtils
    {
        public static RvmPyramid CreatePyramidWithUnitSizeInAllDimension(RvmPyramid input)
        {
            if (input.BottomX < float.Epsilon || input.BottomY < float.Epsilon)
            {
                Console.WriteLine(input);
            }

            var unitScaleXModifier = 1 / input.BottomX;
            var unitScaleYModifier = 1 / input.BottomY;
            var unitScaleZModifier = 1 / input.Height;

            var scales = new Vector3(unitScaleXModifier, unitScaleYModifier, unitScaleZModifier);
            var inverseScale = new Vector3(input.BottomX / 1f, input.BottomY / 1f, input.Height / 1f);

            if (!scales.AsEnumerable().All(RvmSharp.Operations.FloatExtensions.IsFinite))
            {
                // throw new Exception($"Unexpected non-finite scaling. Was {scales}");
                return input;
            }

            var transform = input.Matrix.TryDecomposeToTransform();
            if (transform == null)
                return input;

            (Vector3 scale, Quaternion rotation, Vector3 translation) = transform;

            var scaledPyramid = input with
            {
                Matrix = Matrix4x4Helpers.CalculateTransformMatrix(translation, rotation, scale * inverseScale),
                Height = input.Height * unitScaleZModifier,
                BottomX = input.BottomX * unitScaleXModifier,
                BottomY = input.BottomY * unitScaleYModifier,
                OffsetX = input.OffsetX * unitScaleXModifier,
                OffsetY = input.OffsetY * unitScaleYModifier,
                TopX = input.TopX * unitScaleXModifier,
                TopY = input.TopY * unitScaleYModifier
            };


            return scaledPyramid;
        }

        /// <summary>
        /// Check if two pyramids can be represented by an identical mesh. This assumes scaling to 1 in all directions.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool CanBeRepresentedByEqualMesh(RvmPyramid a, RvmPyramid b)
        {
            Debug.Assert(a.BottomX.ApproximatelyEquals(1));
            Debug.Assert(a.BottomY.ApproximatelyEquals(1));
            Debug.Assert(a.Height.ApproximatelyEquals(1));

            // TODO: Rotations and stuff

            return (a.BottomX.ApproximatelyEquals(b.BottomX)
                    && a.BottomY.ApproximatelyEquals(b.BottomY))
                   && a.OffsetX.ApproximatelyEquals(b.OffsetX)
                   && a.OffsetY.ApproximatelyEquals(b.OffsetY)
                   && a.TopX.ApproximatelyEquals(b.TopX)
                   && a.TopY.ApproximatelyEquals(b.TopY)
                   && a.Height.ApproximatelyEquals(b.Height);
        }
    }
}