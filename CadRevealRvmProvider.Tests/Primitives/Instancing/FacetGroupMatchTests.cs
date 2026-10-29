namespace CadRevealRvmProvider.Tests.Primitives.Instancing;

using System.Numerics;
using Operations;
using RvmSharp.Primitives;
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
        Assert.That(hingesEqual, Is.False);
    }

    [Test]
    public void MatchUnequalPanelsWithOffset()
    {
        var panel1 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("0.json");
        var panel2 = TestSampleLoader.LoadTestJson<RvmFacetGroup>("2.json");
        var panelsEqual = RvmFacetGroupMatcher.Match(panel1, panel2, out Matrix4x4 _);
        Assert.That(panelsEqual, Is.False);
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

        Assert.That(templates, Has.Exactly(1).Items);
        Assert.That(instanced, Has.Exactly(10).Items);
        Assert.That(notInstanced, Has.Exactly(5).Items);
    }

    [Test]
    public void MatchAllReusesPositionsAndKeepsPreparedDataWithPromotedTemplates()
    {
        int[] families = [0, 1, 1, 1, 0, 0];
        var groups = families
            .Select(
                (family, index) =>
                {
                    var unused = new Vector3(float.NaN);
                    var vertices = new[]
                    {
                        (unused, Vector3.Zero),
                        (Vector3.Zero, Vector3.UnitZ),
                        (Vector3.UnitX, Vector3.UnitZ),
                        (Vector3.UnitY, Vector3.UnitZ),
                        (Vector3.UnitZ, Vector3.UnitZ),
                        (family == 0 ? Vector3.One : new Vector3(0.5f), Vector3.UnitZ),
                        (unused, Vector3.Zero),
                    };
                    var contours = new[]
                    {
                        new RvmFacetGroup.RvmContour(new ArraySegment<(Vector3, Vector3)>(vertices, 0, 1)),
                        new RvmFacetGroup.RvmContour(new ArraySegment<(Vector3, Vector3)>(vertices, 1, 5)),
                    };
                    return new RvmFacetGroup(
                        1,
                        index < 2
                            ? Matrix4x4.Identity
                            : Matrix4x4.CreateScale(1 + index * 0.1f, 1 + index * 0.2f, 1 + index * 0.3f)
                                * Matrix4x4.CreateRotationZ(index * 0.2f)
                                * Matrix4x4.CreateTranslation(index * 10, -index * 5, index * 2),
                        // Conservative bounds make template encounter order deterministic despite parallel grouping.
                        new RvmBoundingBox(Vector3.Zero, new Vector3(10 - index)),
                        [new RvmFacetGroup.RvmPolygon(new ArraySegment<RvmFacetGroup.RvmContour>(contours, 1, 1))]
                    );
                }
            )
            .ToArray();

        var results = RvmFacetGroupMatcher.MatchAll(groups, _ => true, 100);
        var instances = results.OfType<RvmFacetGroupMatcher.InstancedResult>().ToArray();
        Assert.That(instances, Has.Length.EqualTo(groups.Length));
        Assert.That(results.OfType<RvmFacetGroupMatcher.TemplateResult>().Count(), Is.EqualTo(2));
        foreach (var instance in instances)
        {
            var original = instance.FacetGroup.Polygons[0].Contours[0].Vertices;
            var template = instance.Template.Polygons[0].Contours[0].Vertices;
            for (var vertexIndex = 0; vertexIndex < original.Count; vertexIndex++)
            {
                var expected = Vector3.Transform(original[vertexIndex].Vertex, instance.FacetGroup.Matrix);
                var actual = Vector3.Transform(template[vertexIndex].Vertex, instance.Transform);
                Assert.That(Vector3.Distance(actual, expected), Is.LessThan(0.001f));
                Assert.That(original[vertexIndex].Normal, Is.EqualTo(Vector3.UnitZ));
            }
        }
    }

    [Test]
    // Documents the solver's non-coplanar anchor requirement, not a desired restriction.
    // Update this expectation when planar matching is intentionally supported.
    public void MatchAllKeepsPlanarGroupsUninstanced()
    {
        var vertices = new[]
        {
            (Vector3.Zero, Vector3.UnitZ),
            (Vector3.UnitX, Vector3.UnitZ),
            (Vector3.UnitY, Vector3.UnitZ),
            (Vector3.UnitX + Vector3.UnitY, Vector3.UnitZ),
        };
        var group = new RvmFacetGroup(
            1,
            Matrix4x4.Identity,
            new RvmBoundingBox(Vector3.Zero, Vector3.One),
            [new RvmFacetGroup.RvmPolygon(new[] { new RvmFacetGroup.RvmContour(vertices) })]
        );

        var results = RvmFacetGroupMatcher.MatchAll([group, group, group], _ => true, 100);
        Assert.That(results, Has.Length.EqualTo(3));
        Assert.That(results, Has.All.TypeOf<RvmFacetGroupMatcher.NotInstancedResult>());
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
