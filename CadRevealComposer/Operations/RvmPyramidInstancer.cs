namespace CadRevealComposer.Operations
{
    using Primitives;
    using RvmSharp.Primitives;
    using System.Collections.Generic;
    using System.Numerics;

    public static class RvmPyramidInstancer
    {
        public static Dictionary<ProtoMeshFromPyramid, (RvmPyramid template, Matrix4x4 transform)> Process(ProtoMeshFromPyramid[] protoPyramids)
        {
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
                    protoPyramidToTemplateAndTransform.Add(protoPyramid, (template, transform));
                    matchFound = true;
                    break;
                }

                if (matchFound)
                    continue;

                var newTemplate = rvmPyramid with { Matrix = Matrix4x4.Identity };
                templateLibrary.Add(newTemplate);
                protoPyramidToTemplateAndTransform.Add(protoPyramid, (newTemplate, rvmPyramid.Matrix));
            }

            return protoPyramidToTemplateAndTransform;
        }
    }
}