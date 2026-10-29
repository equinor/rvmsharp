namespace CadRevealRvmProvider.Tests.Primitives.Instancing;

using System.Numerics;
using CadRevealComposer.Utils;
using Operations;
using RvmSharp.Operations;
using RvmSharp.Primitives;
using Utils;

[TestFixture]
public class RvmFacetGroupMatcherTests
{
    [Test]
    public void TransformVertexDataNormalizesNormalsAndPreservesZeroNormals()
    {
        var sourceNormal = new Vector3(1, 1, 0);
        var vertices = new[] { (Vector3.One, sourceNormal), (Vector3.Zero, Vector3.Zero) };
        var contours = new[] { new RvmFacetGroup.RvmContour(vertices) };
        var group = new RvmFacetGroup(
            1,
            Matrix4x4.Identity,
            new RvmBoundingBox(Vector3.Zero, Vector3.One),
            [new RvmFacetGroup.RvmPolygon(contours)]
        );

        var transformed = group.TransformVertexData(Matrix4x4.CreateScale(2, 3, 4));
        var result = transformed.Polygons[0].Contours[0].Vertices;

        var expectedNormal = Vector3.Normalize(new Vector3(1f / 2, 1f / 3, 0));
        Assert.That(Vector3.Distance(result[0].Normal, expectedNormal), Is.LessThan(0.00001f));
        Assert.That(result[0].Normal.Length(), Is.EqualTo(1).Within(0.00001f));
        Assert.That(result[0].Vertex, Is.EqualTo(new Vector3(2, 3, 4)));
        Assert.That(result[1].Normal, Is.EqualTo(Vector3.Zero));
        Assert.That(group.Polygons[0].Contours[0].Vertices[0].Normal, Is.EqualTo(sourceNormal));
    }

    [Test]
    public void GetTransform()
    {
        var isMatch = AlgebraUtils.GetTransform(
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ,
            Vector3.One,
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ,
            Vector3.One,
            out var transform
        );

        Assert.That(isMatch, Is.True, "Could not match.");
        Assert.That(Matrix4x4.Identity, Is.EqualTo(transform));
    }

    [Test]
    public void MatchRotation()
    {
        var r = new Random(0);
        for (int i = 0; i < 1000; i++)
        {
            var meshA = TestSampleLoader.LoadTestJson<RvmFacetGroup>("simple_group.json");

            var eulers = RandomVector(r, 0, MathF.PI);
            var scale = RandomVector(r, 0.1f, 5.1f);
            var q = Quaternion.CreateFromYawPitchRoll(eulers.X, eulers.Y, eulers.Z);

            var mr = Matrix4x4.CreateFromQuaternion(q);
            var ms = Matrix4x4.CreateScale(scale);
            var mt = Matrix4x4.CreateTranslation(Vector3.Zero);
            var ma = ms * mr * mt;

            var meshB = meshA.TransformVertexData(ma);

            var isMatch = RvmFacetGroupMatcher.Match(meshA, meshB, out Matrix4x4 _);
            Assert.That(isMatch, Is.True, "Could not match.");
        }
    }

    [Test]
    public void PreparedTransformAllowsRigidMatchesWithSingularScaleSolver()
    {
        var first = Vector3.Zero;
        var second = new Vector3(1, 1, 0);
        var third = new Vector3(1, -1, 0);
        var fourth = Vector3.UnitZ;
        var source = new AlgebraUtils.TransformSource(first, second, third, fourth);
        var translation = new Vector3(10, 20, 30);

        // This valid uniform scaling is rejected because the squared-coordinate scale solver is singular.
        // Update the failure expectation when that limitation is fixed; rigid matching must still succeed.
        Assert.That(source.TryGetTransform(first, second * 2, third * 2, fourth * 2, out _), Is.False);
        Assert.That(
            source.TryGetTransform(
                first + translation,
                second + translation,
                third + translation,
                fourth + translation,
                out var transform
            ),
            Is.True
        );
        foreach (var vertex in new[] { first, second, third, fourth })
            Assert.That(
                Vector3.Distance(Vector3.Transform(vertex, transform), vertex + translation),
                Is.LessThan(0.001f)
            );
    }

    [Test]
    public void RotationPrecisionTest()
    {
        var meshA = TestSampleLoader.LoadTestJson<RvmFacetGroup>("simple_group.json");

        // these parameters fail on low precision in dot product on from-to rotation
        var eulers = new Vector3(3.044467f, 2.8217556f, 1.6506897f);
        var scale = new Vector3(2.1362286f, 4.620028f, 3.2072587f);

        var position = new Vector3(0, 0, 0);
        var q = Quaternion.CreateFromYawPitchRoll(eulers.X, eulers.Y, eulers.Z);

        var mr = Matrix4x4.CreateFromQuaternion(q);
        var ms = Matrix4x4.CreateScale(scale);
        var mt = Matrix4x4.CreateTranslation(position);
        var ma = ms * mr * mt;

        var meshB = meshA.TransformVertexData(ma);

        var isMatch = RvmFacetGroupMatcher.Match(meshA, meshB, out Matrix4x4 _);
        Assert.That(isMatch, Is.True, "Could not match.");
    }

    private static Vector3 RandomVector(Random r, float minComponentValue, float maxComponentValue)
    {
        float Rf() => r.NextSingle() * (maxComponentValue - minComponentValue) + minComponentValue;
        return new Vector3(Rf(), Rf(), Rf());
    }
}
