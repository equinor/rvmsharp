﻿namespace CadRevealComposer.Tests
{
    using CadRevealComposer.Primitives.Instancing;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.IO;
    using Utils;

    [TestFixture]
    public class FacetGroupMatchTests
    {
        [Test]
        public void MatchTwoBentPipes()
        {
            var pipe1 = DataLoader.LoadTestJson<RvmFacetGroup>("43907.json");
            var pipe2 = DataLoader.LoadTestJson<RvmFacetGroup>("43908.json");
            var pipesEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out var transform);
            Assert.That(pipesEqual);
        }

        [Test]
        public void MatchRotatedHinges()
        {
            var hinges1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(DataLoader.TestSamplesDirectory, "m1.json")));
            var hinges2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(DataLoader.TestSamplesDirectory, "m2.json")));
            var hingesEqual = RvmFacetGroupMatcher.Match(hinges1, hinges2, out var transform);
            Assert.IsFalse(hingesEqual);
        }

        [Test]
        public void MatchUnequalPanelsWithOffset()
        {
            var panel1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(DataLoader.TestSamplesDirectory, "0.json")));
            var panel2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(DataLoader.TestSamplesDirectory, "2.json")));
            var panelsEqual = RvmFacetGroupMatcher.Match(panel1, panel2, out var transform);
            Assert.IsFalse(panelsEqual);
        }

        [Test]
        public void MatchEqualPanelsWithDifferentPolygonOrder()
        {
            var pipe1 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(DataLoader.TestSamplesDirectory, "5.json")));
            var pipe2 = JsonConvert.DeserializeObject<RvmFacetGroup>(
                File.ReadAllText(Path.Combine(DataLoader.TestSamplesDirectory, "6.json")));
            var facetGroupsEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out var transform);
            Assert.That(facetGroupsEqual);
        }
    }
}