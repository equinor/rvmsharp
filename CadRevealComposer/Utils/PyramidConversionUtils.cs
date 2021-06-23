namespace CadRevealComposer.Utils
{
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class PyramidConversionUtils
    {
        private static void TessellatePyramids(IReadOnlyCollection<RvmNode> rvmNodes)
        {
            var pyramidsWithMeshes = rvmNodes
                .AsParallel()
                .Select(x =>
                {
                    const float tolerance = 0.01f; // Tolerance is ignored at the time of writing.
                    var pyramidMeshes = x.Children.OfType<RvmPyramid>().Select(pyramid => (RvmPyramid: pyramid, Mesh: TessellatorBridge.Tessellate(pyramid, tolerance)));

                    return pyramidMeshes;
                });


            bool almostEqual(float f1, float f2)
            {
                return MathF.Abs(f1 - f2) < 0.01;
            }

            var uniquePyramids = new Dictionary<RvmPyramid, int>();
            var processedPyramids = new List<RvmPyramid>();
            foreach (var rvmPyramid in rvmNodes.SelectMany(x => x.Children.OfType<RvmPyramid>())
                .Select(CreatePyramidWithUnitSizeInXDimension))
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

        public static RvmPyramid CreatePyramidWithUnitSizeInXDimension(RvmPyramid input)
        {
            if (input.BottomX < float.Epsilon || input.BottomY < float.Epsilon)
            {
                Console.WriteLine(input);
            }

            var unitScaleModifier = 1 / input.BottomX;

            if (!float.IsFinite(unitScaleModifier * input.BottomX))
                return input;

            return input with
            {
                Height = input.Height * unitScaleModifier,
                BottomX = input.BottomX * unitScaleModifier,
                BottomY = input.BottomY * unitScaleModifier,
                OffsetX = input.OffsetX * unitScaleModifier,
                OffsetY = input.OffsetY * unitScaleModifier,
                TopX = input.TopX * unitScaleModifier,
                TopY = input.TopY * unitScaleModifier
            };
        }
    }
}