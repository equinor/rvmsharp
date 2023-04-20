namespace CadRevealRvmProvider.Tests.Primitives.Instancing;

using Operations;
using RvmSharp.Primitives;
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
        var hinges1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("m1.json");
        var hinges2 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("m2.json");
        var hingesEqual = RvmFacetGroupMatcher.Match(hinges1, hinges2, out Matrix4x4 _);
        Assert.IsFalse(hingesEqual);
    }

    [Test]
    public void MatchUnequalPanelsWithOffset()
    {
        var panel1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("0.json");
        var panel2 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("2.json");
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
        var pipe1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("5.json");
        var pipe2 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("6.json");
        var facetGroupsEqual = RvmFacetGroupMatcher.Match(pipe1, pipe2, out Matrix4x4 _);
        Assert.That(facetGroupsEqual);
    }

    [Test]
    public void MatchAll()
    {
        var pipe1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("43907.json");
        var pipe2 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("43908.json");
        var panel1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("0.json");

        var results = RvmFacetGroupMatcher.MatchAll(new[] { pipe1, pipe2, panel1 }, _ => true, 100);

        var templates = results.OfType<RvmFacetGroupMatcher.TemplateResult>().Select(r => r.FacetGroup).ToArray();
        var instanced = results.OfType<RvmFacetGroupMatcher.InstancedResult>().Select(r => r.FacetGroup).ToArray();
        var notInstanced = results
            .OfType<RvmFacetGroupMatcher.NotInstancedResult>()
            .Select(r => r.FacetGroup)
            .ToArray();

        Assert.That(templates, Does.Contain(pipe1));
        Assert.That(instanced, Does.Contain(pipe1));
        Assert.That(notInstanced, Does.Not.Contain(pipe1));

        Assert.That(templates, Does.Not.Contain(pipe2));
        Assert.That(instanced, Does.Contain(pipe2));
        Assert.That(notInstanced, Does.Not.Contain(pipe2));

        Assert.That(templates, Does.Not.Contain(panel1));
        Assert.That(instanced, Does.Not.Contain(panel1));
        Assert.That(notInstanced, Does.Contain(panel1));
    }

    [Test]
    public void MatchAllWithFilter()
    {
        var equalPipes = Enumerable
            .Range(0, 10)
            .Select(_ => TestSampleLoader.LoadTestJson<RvmFacetGroup>("43907.json"));

        var equalHinges = Enumerable.Range(0, 5).Select(_ => TestSampleLoader.LoadTestJson<RvmFacetGroup>("m1.json"));

        var facetGroups = equalPipes.Concat(equalHinges).ToArray();
        var results = RvmFacetGroupMatcher.MatchAll(facetGroups, group => group.Length >= 10, 100);

        var templates = results.OfType<RvmFacetGroupMatcher.TemplateResult>().Select(r => r.FacetGroup).ToArray();
        var instanced = results.OfType<RvmFacetGroupMatcher.InstancedResult>().Select(r => r.FacetGroup).ToArray();
        var notInstanced = results
            .OfType<RvmFacetGroupMatcher.NotInstancedResult>()
            .Select(r => r.FacetGroup)
            .ToArray();

        Assert.AreEqual(1, templates.Length);
        Assert.AreEqual(10, instanced.Length);
        Assert.AreEqual(5, notInstanced.Length);
    }

    [Test]
    public void MatchAllWithTemplateLimit()
    {
        var pipe1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("43907.json");
        var pipe2 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("43908.json");
        var panel1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("0.json");

        var results = RvmFacetGroupMatcher.MatchAll(new[] { pipe1, pipe1, pipe2, pipe2, panel1, panel1 }, _ => true, 1);

        var instancedResults = results.OfType<RvmFacetGroupMatcher.TemplateResult>();
        Assert.That(instancedResults.Count() <= 1);

        var resultsNoLimit = RvmFacetGroupMatcher.MatchAll(
            new[] { pipe1, pipe1, pipe2, pipe2, panel1, panel1 },
            _ => true,
            100
        );

        var instancedResultsNoLimit = resultsNoLimit.OfType<RvmFacetGroupMatcher.TemplateResult>();
        Assert.That(instancedResultsNoLimit.Count() > 1);
    }
}
