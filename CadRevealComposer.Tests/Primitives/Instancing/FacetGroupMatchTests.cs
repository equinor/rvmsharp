namespace CadRevealComposer.Tests.Primitives.Instancing
{
    using Newtonsoft.Json;
    using NUnit.Framework;
    using Operations;
    using RvmSharp.Primitives;
    using System.IO;
    using System.Numerics;
    using Utils;

    [TestFixture]
    public class FacetGroupMatchTests
    {
        [Test]
        public void MatchItself()
        {
            var pipe1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("43907.json");
            var pipesEqual = RvmFacetGroupMatcher.Match(pipe1, pipe1, out Matrix4x4 _);
            Assert.That(pipesEqual);
        }

        [Test]
        public void MatchTwoBentPipes()
        {
            var pipe1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("43907.json");
            var pipe2 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("43908.json");
            var pipesEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out Matrix4x4 _);
            Assert.That(pipesEqual);
        }

        [Test]
        public void MatchRotatedHinges()
        {
            var hinges1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSampleLoader.TestSamplesDirectory, "m1.json")));
            var hinges2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSampleLoader.TestSamplesDirectory, "m2.json")));
            var hingesEqual = RvmFacetGroupMatcher.Match(hinges1, hinges2, out Matrix4x4 _);
            Assert.IsFalse(hingesEqual);
        }

        [Test]
        public void MatchUnequalPanelsWithOffset()
        {
            var panel1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSampleLoader.TestSamplesDirectory, "0.json")));
            var panel2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSampleLoader.TestSamplesDirectory, "2.json")));
            var panelsEqual = RvmFacetGroupMatcher.Match(panel1, panel2, out Matrix4x4 _);
            Assert.IsFalse(panelsEqual);
        }

        /// <summary>
        /// This test will match mixed polygon meshes. Currently it is disabled since the code
        /// that can handle this case is not implemented yet
        /// </summary>
        [Test]
        [Explicit]
        public void MatchEqualPanelsWithDifferentPolygonOrder()
        {
            var pipe1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSampleLoader.TestSamplesDirectory, "5.json")));
            var pipe2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(TestSampleLoader.TestSamplesDirectory, "6.json")));
            var facetGroupsEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out Matrix4x4 _);
            Assert.That(facetGroupsEqual);
        }
    }
}