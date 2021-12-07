namespace CadRevealComposer.Operations
{
    using Primitives;
    using RvmSharp.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public static class RvmPyramidInstancer
    {
        public static Dictionary<ProtoMeshFromPyramid, (RvmPyramid template, Matrix4x4 transform)> Process(ProtoMeshFromPyramid[] protoPyramids)
        {
            var debugDuplicatePyramidsIgnoredCount = 0;

            var templateLibrary = new List<RvmPyramid>();
            var protoPyramidToTemplateAndTransform = new Dictionary<ProtoMeshFromPyramid, (RvmPyramid template, Matrix4x4 transform)>();

            foreach (var protoPyramid in protoPyramids)
            {
                var rvmPyramid = protoPyramid.SourcePyramid;
                var matchFound = false;
                foreach (var template in templateLibrary)
                {
                    if (!RvmPyramidMatcher.Match(template, rvmPyramid, out var transform))
                        continue;
                    transform =  transform * rvmPyramid.Matrix;

                    // We have models where multiple Primitives on the same "part" are completely identical.
                    // This checks if this exact primitive is already used as a key. If it is we just count and ignore it.
                    if (protoPyramidToTemplateAndTransform.ContainsKey(protoPyramid))
                    {
                        debugDuplicatePyramidsIgnoredCount++;
                        break;
                    }

                    protoPyramidToTemplateAndTransform.Add(protoPyramid, (template, transform));
                    matchFound = true;
                    break;
                }

                if (matchFound)
                    continue;

                // We have models where multiple Primitives on the same "part" are completely identical.
                // This checks if this exact primitive is already used as a key. If it is we just count and ignore it.
                if (protoPyramidToTemplateAndTransform.ContainsKey(protoPyramid))
                {
                    debugDuplicatePyramidsIgnoredCount++;
                    continue;
                }

                var newTemplate = rvmPyramid with { Matrix = Matrix4x4.Identity };
                templateLibrary.Add(newTemplate);

                protoPyramidToTemplateAndTransform.Add(protoPyramid, (newTemplate, rvmPyramid.Matrix));
            }

            Console.WriteLine($"Found and ignored {debugDuplicatePyramidsIgnoredCount} duplicate pyramids (including: position, mesh, parent, id, etc).");

            return protoPyramidToTemplateAndTransform;
        }
    }
}