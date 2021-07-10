namespace CadRevealComposer.Primitives.Converters
{
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System.Collections.Generic;
    using System.Linq;
    using Utils;

    public class PyramidInstancingHelper
    {
        private readonly Dictionary<RvmPyramid, Mesh?> _instanceCandidateLookup;

        public PyramidInstancingHelper(IReadOnlyList<CadRevealNode> nodes, int matchesNeededToMarkForInstancing = 30)
        {
            // The idea here is to collect all pyramids by scaling them to a 1x1x1 representation, and applying the inverse scale to their matrix.
            // All pyramids that can be scaled easily on xyz can then use the same mesh.
            // I'm not 100% sure on this approach:
            //   It does not match if matches could be found by applying rotation, mirroring, inversion, etc
            //   It also does not handle when a dimension is Zero (as we cannot scale it to "1")

            var pyramids = nodes.SelectMany(x => x.RvmGeometries.OfType<RvmPyramid>());

            var unitPyramids = pyramids.Where(x => x.Height > 0 && x.BottomX > 0 && x.BottomY > 0).Select(PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension);


            Dictionary<RvmPyramid, List<RvmPyramid>> pyramidTemplateCandidates = new Dictionary<RvmPyramid, List<RvmPyramid>>();

            foreach (var pyramid in unitPyramids)
            {
                bool foundMatch = false;
                foreach (var keyValuePair in pyramidTemplateCandidates)
                {
                    if (PyramidConversionUtils.CanBeRepresentedByEqualMesh(keyValuePair.Key, pyramid))
                    {
                        pyramidTemplateCandidates[keyValuePair.Key].Add(pyramid);
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    pyramidTemplateCandidates[pyramid] = new List<RvmPyramid>() { pyramid };
                }
            }

            _instanceCandidateLookup = pyramidTemplateCandidates
                .Where(kvp => kvp.Value.Count > matchesNeededToMarkForInstancing)
                .SelectMany(kvp => kvp.Value.Distinct().Select(rvmPyramid => (kvp.Key, rvmPyramid)))
                .ToDictionary(x => x.rvmPyramid, x =>
                {
                    const float unused = -1;
                    return TessellatorBridge.Tessellate(x.Key, unused, unused);
                });
        }

        public Mesh? TryGetInstancedMesh(RvmPyramid pyramid)
        {
            var pyramidAsBox = PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(pyramid);
            if (_instanceCandidateLookup.TryGetValue(pyramidAsBox, out var sharedMesh))
            {
                return sharedMesh;
            }

            return null;
        }
    }
}