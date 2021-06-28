namespace CadRevealComposer.Utils
{
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    public class PyramidConversionUtils
    {
        private static void TessellatePyramids(IReadOnlyCollection<RvmNode> rvmNodes)
        {
            var pyramidsWithMeshes = rvmNodes
                .AsParallel()
                .Select(x =>
                {
                    const float tolerance = 0.01f; // Tolerance is ignored at the time of writing.
                    var pyramidMeshes = x.Children.OfType<RvmPyramid>().Select(pyramid =>
                        (RvmPyramid: pyramid, Mesh: TessellatorBridge.Tessellate(pyramid, tolerance)));

                    return pyramidMeshes;
                });


            bool almostEqual(float f1, float f2)
            {
                return MathF.Abs(f1 - f2) < 0.01;
            }

            var uniquePyramids = new Dictionary<RvmPyramid, int>();
            var processedPyramids = new List<RvmPyramid>();
            foreach (var rvmPyramid in rvmNodes.SelectMany(x => x.Children.OfType<RvmPyramid>())
                .Select(CreatePyramidWithUnitSizeInAllDimension))
            {
                var foundEqual = false;
                foreach (var kvp in uniquePyramids)
                {
                    var uniquePyramid = kvp.Key;

                    if ( /* almostEqual(rvmPyramid.Height, uniquePyramid.Height) */
                        // We can scale for height, assuming the top is in same position
                        (almostEqual(rvmPyramid.BottomX,
                             uniquePyramid.BottomX) // BottomX should be equal for all, since they are scaled equal
                         && almostEqual(rvmPyramid.BottomY, uniquePyramid.BottomY))
                        && almostEqual(rvmPyramid.OffsetX, uniquePyramid.OffsetX)
                        && almostEqual(rvmPyramid.OffsetY, uniquePyramid.OffsetY)
                        && almostEqual(rvmPyramid.TopX, uniquePyramid.TopX)
                        && almostEqual(rvmPyramid.TopY, uniquePyramid.TopY)
                    )
                    {
                        uniquePyramids[uniquePyramid] = uniquePyramids[uniquePyramid] + 1;
                        foundEqual = true;
                        break;
                    }
                }

                if (!foundEqual)
                    uniquePyramids.Add(rvmPyramid, 0);


                processedPyramids.Add(rvmPyramid);
            }

            Console.WriteLine("Pyramids");
        }

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
                throw new Exception($"Unexpected non-finite scaling. Was {scales}");

            var scaledPyramid = input with
            {
                Matrix = Matrix4x4.Multiply(input.Matrix, Matrix4x4.CreateScale(inverseScale)),
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