namespace CadRevealComposer.Operations;

using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class RvmPyramidInstancer
{
    public abstract record Result(ProtoMeshFromPyramid Pyramid);
    public record NotInstancedResult(ProtoMeshFromPyramid Pyramid) : Result(Pyramid);
    public record InstancedResult(ProtoMeshFromPyramid Pyramid, RvmPyramid Template, Matrix4x4 Transform) : Result(Pyramid);
    public record TemplateResult(ProtoMeshFromPyramid Pyramid, RvmPyramid Template, Matrix4x4 Transform) : InstancedResult(Pyramid, Template, Transform);

    private record TemplateInfo(ProtoMeshFromPyramid Pyramid, RvmPyramid Template, Matrix4x4 Transform)
    {
        public List<(ProtoMeshFromPyramid, Matrix4x4)>? Matches { get; private set; }

        public void Add(ProtoMeshFromPyramid pyramid, Matrix4x4 transform)
        {
            Matches ??= new List<(ProtoMeshFromPyramid, Matrix4x4)>();
            Matches.Add((pyramid, transform));
        }
    };

    public static Result[] Process(ProtoMeshFromPyramid[] protoPyramids, Func<ProtoMeshFromPyramid[], bool> shouldInstance)
    {
        var templateLibrary = new List<TemplateInfo>();

        //foreach (var protoPyramid in protoPyramids)
        for (int i = 0; i < protoPyramids.Length; i++)
        {
            var rvmPyramid = protoPyramids[i].Pyramid;
            var matchFound = false;
            //foreach (var template in templateLibrary)
            for(int iLib=0; iLib < templateLibrary.Count;iLib++)
            {
                var template = templateLibrary[iLib];
                var rvmPyramidTemplate = template.Template;
                var isMatch = RvmPyramidMatcher.Match(rvmPyramidTemplate, rvmPyramid, out var transform);
                if (!isMatch)
                {
                    continue;
                }

                var newTransform = transform * rvmPyramid.Matrix;
                template.Add(protoPyramids[i], newTransform);
                matchFound = true;
                break;
            }

            if (matchFound)
            {
                continue;
            }

            var newTemplate = rvmPyramid with { Matrix = Matrix4x4.Identity };
            templateLibrary.Add(new TemplateInfo(protoPyramids[i], newTemplate, rvmPyramid.Matrix));
        }

        var result = new List<Result>(protoPyramids.Length);
        //foreach (var template in templateLibrary)
        for(int i=0;i<templateLibrary.Count;i++)
        {
            var template = templateLibrary[i];
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