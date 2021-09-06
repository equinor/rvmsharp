namespace CadRevealComposer.Tests.Utils
{
    using NUnit.Framework;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System.IO;

    /// <summary>
    /// Use methods provided here to manually extract and inspect data
    /// </summary>
    [TestFixture]
    public class FacetGroupHelpers
    {
        private const string ExportPath = "D:\\tmp";
        private static readonly string[] FacetGroupJsons = new[]
        {
            "m1.json",
            "m2.json",
            "5.json",
            "6.json"
        };

        [Explicit]
        [Test]
        public void ExportAsObj()
        {
            Directory.CreateDirectory(ExportPath);
            foreach (var facetGroupJson in FacetGroupJsons)
            {
                var facetGroup = DataLoader.LoadTestJson<RvmFacetGroup>(facetGroupJson);
                var name = facetGroupJson.Replace(".json", "");
                using var objExporter = new ObjExporter(Path.Combine(ExportPath, $"{name}.obj"));
                objExporter.StartGroup(name);
                objExporter.WriteMesh(TessellatorBridge.Tessellate(facetGroup, 5.0f));
            }
        }

        [Explicit]
        [Test]
        public void ExportPolygonsAsObj()
        {
            Directory.CreateDirectory(ExportPath);
            foreach (var facetGroupJson in FacetGroupJsons)
            {
                var facetGroup = DataLoader.LoadTestJson<RvmFacetGroup>(facetGroupJson);
                var name = facetGroupJson.Replace(".json", "");
                using var objExporter = new ObjExporter(Path.Combine(ExportPath, $"{name}_polys.obj"));
                objExporter.StartGroup(name);

                for (var i = 0; i < facetGroup.Polygons.Length; i++)
                {
                    var p = facetGroup.Polygons[i];
                    var m1 = facetGroup with { Polygons = new[] { p } };
                    objExporter.StartGroup($"{name}_p{i}");
                    objExporter.WriteMesh(TessellatorBridge.Tessellate(m1, 5.0f));
                }
            }
        }
    }
}