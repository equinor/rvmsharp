namespace CadRevealRvmProvider.Operations;

using RvmSharp.Primitives;
using System.Numerics;

public static class RvmPyramidInstancer
{
    public abstract record Result(ProtoMeshFromRvmPyramid Pyramid);
    public record NotInstancedResult(ProtoMeshFromRvmPyramid Pyramid) : Result(Pyramid);
    public record InstancedResult(ProtoMeshFromRvmPyramid Pyramid, RvmPyramid Template, Matrix4x4 Transform) : Result(Pyramid);
    public record TemplateResult(ProtoMeshFromRvmPyramid Pyramid, RvmPyramid Template, Matrix4x4 Transform) : InstancedResult(Pyramid, Template, Transform);

    private record TemplateInfo(ProtoMeshFromRvmPyramid Pyramid, RvmPyramid Template, Matrix4x4 Transform)
    {
        public List<(ProtoMeshFromRvmPyramid, Matrix4x4)>? Matches { get; private set; }

        public void Add(ProtoMeshFromRvmPyramid pyramid, Matrix4x4 transform)
        {
            Matches ??= new List<(ProtoMeshFromRvmPyramid, Matrix4x4)>();
            Matches.Add((pyramid, transform));
        }
    };

    public static Result[] Process(ProtoMeshFromRvmPyramid[] protoPyramids, Func<ProtoMeshFromRvmPyramid[], bool> shouldInstance)
    {
        var templateLibrary = new List<TemplateInfo>();

        foreach (var protoPyramid in protoPyramids)
        {
            var rvmPyramid = protoPyramid.Pyramid;
            var matchFound = false;
            foreach (var template in templateLibrary)
            {
                var rvmPyramidTemplate = template.Template;
                var isMatch = RvmPyramidMatcher.Match(rvmPyramidTemplate, rvmPyramid, out var transform);
                if (!isMatch)
                {
                    continue;
                }

                var newTransform = transform * rvmPyramid.Matrix;
                template.Add(protoPyramid, newTransform);
                matchFound = true;
                break;
            }

            if (matchFound)
            {
                continue;
            }

            var newTemplate = rvmPyramid with { Matrix = Matrix4x4.Identity };
            templateLibrary.Add(new TemplateInfo(protoPyramid, newTemplate, rvmPyramid.Matrix));
        }

        var result = new List<Result>(protoPyramids.Length);
        foreach (var template in templateLibrary)
        {
            if (template.Matches is null)
            {
                result.Add(new NotInstancedResult(template.Pyramid));
                continue;
            }

            var pyramids = new[] { template.Pyramid }
                .Concat(template.Matches.Select(p => p.Item1))
                .ToArray();

            if (!shouldInstance(pyramids))
            {
                foreach (var pyramid in pyramids)
                {
                    result.Add(new NotInstancedResult(pyramid));
                }
                continue;
            }

            // instanced
            result.Add(new TemplateResult(template.Pyramid, template.Template, template.Transform));
            foreach (var templateMatch in template.Matches)
            {
                result.Add(new InstancedResult(templateMatch.Item1, template.Template, templateMatch.Item2));
            }
        }

        if (result.Count != protoPyramids.Length)
        {
            throw new Exception("Input and output count doesn't match up.");
        }

        var templateCount = result.OfType<TemplateResult>().Count();
        var instancedCount = result.OfType<InstancedResult>().Count();
        var fraction = instancedCount / (float)protoPyramids.Length;
        Console.WriteLine($"Pyramids found {templateCount:N0} unique representing {instancedCount:N0} instances from a total of {protoPyramids.Length:N0} ({fraction:P1}).");

        return result.ToArray();
    }
}