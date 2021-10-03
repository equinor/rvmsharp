﻿namespace CadRevealComposer.Tests.Utils
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using Operations;
    using RvmSharp.BatchUtils;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Use methods provided here to manually extract and inspect data
    /// </summary>
    [TestFixture]
    public class FacetGroupHelpers
    {
        private const float UnusedTolerance = -1.0f;
        private const string ExportPath = "D:\\tmp";
        private const string ExplicitReason = "Development helpers, not real tests.";

        private static readonly string[] FacetGroupJsons = new[]
        {
            "m1.json",
            "m2.json",
            "5.json",
            "6.json"
        };

        [Explicit(ExplicitReason)]
        [Test]
        public void ExportAsObj()
        {
            Directory.CreateDirectory(ExportPath);
            foreach (var facetGroupJson in FacetGroupJsons)
            {
                var facetGroup = TestSampleLoader.LoadTestJson<RvmFacetGroup>(facetGroupJson);
                var name = facetGroupJson.Replace(".json", "");
                using var objExporter = new ObjExporter(Path.Combine(ExportPath, $"{name}.obj"));
                objExporter.StartGroup(name);
                objExporter.WriteMesh(TessellatorBridge.Tessellate(facetGroup, 5.0f)!);
            }
        }

        [Explicit(ExplicitReason)]
        [Test]
        public void ExportPolygonsAsObj()
        {
            Directory.CreateDirectory(ExportPath);
            foreach (var facetGroupJson in FacetGroupJsons)
            {
                var facetGroup = TestSampleLoader.LoadTestJson<RvmFacetGroup>(facetGroupJson);
                var name = facetGroupJson.Replace(".json", "");
                using var objExporter = new ObjExporter(Path.Combine(ExportPath, $"{name}_polys.obj"));
                objExporter.StartGroup(name);

                for (var i = 0; i < facetGroup.Polygons.Length; i++)
                {
                    var p = facetGroup.Polygons[i];
                    var m1 = facetGroup with { Polygons = new[] { p } };
                    objExporter.StartGroup($"{name}_p{i}");
                    objExporter.WriteMesh(TessellatorBridge.Tessellate(m1, UnusedTolerance)!);
                }
            }
        }



        [Explicit(ExplicitReason)]
        [Test]
        public void ExportAllUnmatchedFacetGroupsAsObjs()
        {
            var workload = Workload.CollectWorkload(new[] { @"d:\Models\hda\rvm20210126" });
            var rvmStore = Workload.ReadRvmData(workload);
            var rvmNodes = rvmStore.RvmFiles.Select(f => f.Model).SelectMany(m => m.Children);
            var facetGroups = rvmNodes.SelectMany(GetAllFacetGroups).ToArray();

            var groupToTemplateWithTransform = RvmFacetGroupMatcher.MatchAll(facetGroups);
            var templateToMatchCount = groupToTemplateWithTransform
                .Select(kvp => (kvp.Value.template, kvp.Key))
                .GroupBy(p => p.template)
                .ToDictionary(p => p.Key, p => p.Count());
            var templateToFacetGroup = groupToTemplateWithTransform
                .Select(kvp => kvp.Value.template)
                .Distinct()
                .GroupBy(RvmFacetGroupMatcher.CalculateKey)
                .ToArray();

            var totalCount = 0;
            foreach (var templatesByKey in templateToFacetGroup)
            {
                var templates = templatesByKey.ToArray();
                var key = templatesByKey.Key;
                var templateCountForKey = templates.Length;
                if (templateCountForKey == 1) // skip all single key meshes, we cannot match it with anything
                    continue;
                var totalMatches = templates.Select(t => templateToMatchCount[t]).Sum();
                Console.WriteLine($"For group {key} templating count is remaining {templateCountForKey} of {totalMatches} ({(100.0 * (totalMatches - templateCountForKey) / totalMatches):F}%)");
                // foreach (var template in templatesByKey)
                // {
                //     var matchCount = templateToMatchCount[template];
                // }

                var i = 0;
                foreach (var t in templates)
                {
                    var m = TessellatorBridge.Tessellate(t, 5.0f);
                    var directory = $"{ExportPath}/{key}";
                    Directory.CreateDirectory(directory);
                    using var objExporter = new ObjExporter($"{directory}/{i}.obj");
                    objExporter.StartGroup(i.ToString());
                    objExporter.WriteMesh(m!);

                    File.WriteAllText($"{directory}/{i}.json", JsonConvert.SerializeObject(t));
                    i++;
                }

                totalCount += totalMatches;
            }

            Assert.AreEqual(groupToTemplateWithTransform.Count, totalCount);
        }

        private static IEnumerable<RvmFacetGroup> GetAllFacetGroups(RvmNode root)
        {
            foreach (var child in root.Children)
            {
                switch (child)
                {
                    case RvmNode node:
                        foreach (var facetGroup in GetAllFacetGroups(node))
                            yield return facetGroup;
                        break;
                    case RvmFacetGroup facetGroup:
                        yield return facetGroup;
                        break;
                }
            }
        }
    }
}