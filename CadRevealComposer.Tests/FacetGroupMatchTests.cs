namespace CadRevealComposer.Tests
{
    using CadRevealComposer.Primitives.Instancing;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using RvmSharp.Exporters;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System.IO;

    [TestFixture]
    public class FacetGroupMatchTests
    {
        private static readonly string TestSamplesDirectory = Path.GetFullPath(Path.Join(TestContext.CurrentContext.TestDirectory, "TestSamples"));

        private static T LoadTestJson<T>(string filename)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(TestSamplesDirectory, filename)));
        }

        [Test]
        public void TestPipes()
        {
            var pipe1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "43907.json")));
            var pipe2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "43907.json")));
            var facetGroupsEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out var transform);
            Assert.That(facetGroupsEqual);
        }

        [Test]
        public void TestPipes2()
        {
            var pipe1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "m1.json")));
            var pipe2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "m2.json")));
            var facetGroupsEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out var transform);
            Assert.IsFalse(facetGroupsEqual);
        }

        [Test]
        public void TestPipes3()
        {
            var pipe1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "0.json")));
            var pipe2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "2.json")));
            var facetGroupsEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out var transform);
            Assert.IsFalse(facetGroupsEqual);
        }

        [Test]
        public void TestPipes4()
        {
            var pipe1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "5.json")));
            var pipe2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSamplesDirectory, "6.json")));
            var facetGroupsEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out var transform);
            Assert.That(facetGroupsEqual);
        }

        [Explicit]
        [Test]
        public void WriteObj()
        {
            var f1 = LoadTestJson<RvmFacetGroup>("m1.json");
            var f2 = LoadTestJson<RvmFacetGroup>("m2.json");
            var objExporter = new ObjExporter("D:\\m.obj");
            objExporter.StartGroup("m1");
            objExporter.WriteMesh(TessellatorBridge.Tessellate(f1, 5.0f));
            objExporter.StartGroup("m2");
            objExporter.WriteMesh(TessellatorBridge.Tessellate(f2, 5.0f));
        }

        [Explicit]
        [Test]
        public void WriteObj2()
        {
            var f1 = LoadTestJson<RvmFacetGroup>("5.json");
            var f2 = LoadTestJson<RvmFacetGroup>("6.json");
            using var objExporter = new ObjExporter("D:\\m.obj");

            for (var i = 0; i < f1.Polygons.Length; i++)
            {
                var p = f1.Polygons[i];
                var m1 = f1 with { Polygons = new[] { p } };
                objExporter.StartGroup($"m5_p{i}");
                objExporter.WriteMesh(TessellatorBridge.Tessellate(m1, 5.0f));
            }
            for (var i = 0; i < f2.Polygons.Length; i++)
            {
                var p = f2.Polygons[i];
                var m1 = f2 with { Polygons = new[] { p } };
                objExporter.StartGroup($"m6_p{i}");
                objExporter.WriteMesh(TessellatorBridge.Tessellate(m1, 5.0f));
            }
        }

    }
}