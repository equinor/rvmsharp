namespace CadRevealComposer.Tests.Primitives.Instancing
{
    using CadRevealComposer.Primitives.Instancing;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System;
    using System.Linq;
    using System.Numerics;

    // TODO: test for vectors in same and opposite direction

    public class RvmFacetGroupMatcherTests
    {
        [Test]
        public void CheckTransformVertex()
        {
            Assert.IsFalse(RvmFacetGroupMatcher.MatchVertexApproximately(Vector3.Zero, Vector3.One, Matrix4x4.Identity));
            Assert.IsFalse(RvmFacetGroupMatcher.MatchVertexApproximately(Vector3.One, Vector3.One, Matrix4x4.CreateScale(2)));

            Assert.IsTrue(RvmFacetGroupMatcher.MatchVertexApproximately(Vector3.One, Vector3.One, Matrix4x4.Identity));
        }

        [Test]
        public void GetTransform()
        {
            //var isMatch = RvmFacetGroupMatcher.TryCalculateTransform(
            //    Vector3.UnitX,
            //    Vector3.UnitY,
            //    Vector3.UnitZ,
            //    Vector3.UnitX,
            //    Vector3.UnitY,
            //    Vector3.UnitZ,
            //    out var transform);
            //
            //Assert.IsTrue(isMatch, "Could not match.");
            //Assert.AreEqual(Matrix4x4.Identity, transform.Value);
        }

        [Test]
        public void MatchWithSelf()
        {
            var facetGroup = CreateRvmFacetGroup(Vector3.One, Vector3.One, Vector3.One);
            var isMatch = RvmFacetGroupMatcher.Match(facetGroup, facetGroup, out var transform);

            Assert.IsTrue(isMatch, "Could not match with self.");
            Assert.IsTrue(transform.Value.IsIdentity, "Matching with self should return identity transform.");
        }

        [Test]
        public void MatchScale()
        {
            var a = CreateRvmFacetGroup(
                Vector3.Transform(Vector3.One, Matrix4x4.CreateRotationX(MathF.PI / 2f)),
                Vector3.Transform(Vector3.One, Matrix4x4.CreateRotationY(MathF.PI / 2f)),
                Vector3.Transform(Vector3.One, Matrix4x4.CreateRotationZ(MathF.PI / 2f)));
            var b = CreateRvmFacetGroup(
                Vector3.Transform(Vector3.One, Matrix4x4.CreateRotationX(MathF.PI / 2f) * 2),
                Vector3.Transform(Vector3.One, Matrix4x4.CreateRotationY(MathF.PI / 2f) * 2),
                Vector3.Transform(Vector3.One, Matrix4x4.CreateRotationZ(MathF.PI / 2f) * 2));
            var isMatch = RvmFacetGroupMatcher.Match(a, b, out _);

            Assert.IsTrue(isMatch, "Could not match.");
        }

        public RvmFacetGroup TransformFacetGroup(RvmFacetGroup group, Matrix4x4 matrix)
        {
            return group with
            {
                Polygons = group.Polygons.Select(a => a with
                {
                    Contours = a.Contours.Select(c => c with
                    {
                        Vertices = c.Vertices.Select(v => (Vector3.Transform(v.Vertex, matrix), Vector3.TransformNormal(v.Normal, matrix))).ToArray()
                    }).ToArray()
                }
                ).ToArray()
            };
        }

        [Test]
        public void MatchRotation()
        {
            var meshA = new RvmFacetGroup(0, Matrix4x4.Identity, new RvmBoundingBox(Vector3.One, Vector3.One),
                new[]
                {
                    new RvmFacetGroup.RvmPolygon(new []
                    {
                        new RvmFacetGroup.RvmContour(new []
                        {
                            (new Vector3(1, 1, 1), Vector3.One),
                            (new Vector3(0, 2, 1), Vector3.One),
                            (new Vector3(3, 2.5f, 1.5f), Vector3.One),
                        }),
                        new RvmFacetGroup.RvmContour(new []
                        {
                            (new Vector3(1, 1, 1), Vector3.One),
                            (new Vector3(0, 2, 1), Vector3.One),
                            (new Vector3(3, 3, 3), Vector3.One),
                        }),
                    })
                });

            var q = Quaternion.CreateFromYawPitchRoll(14, 123, 43);
            var Mr = Matrix4x4.CreateFromQuaternion(q);
            var Ms = Matrix4x4.CreateScale(new Vector3(1, 4, 5));
            var Mt = Matrix4x4.CreateTranslation(new Vector3(2, 3, 4));
            var Ma = Ms * Mr * Mt;

            var meshB = TransformFacetGroup(meshA, Ma);

            var isMatch = RvmFacetGroupMatcher.Match(meshA, meshB, out var transform);

            Assert.IsTrue(isMatch, "Could not match.");
        }

        private static RvmFacetGroup CreateRvmFacetGroup(params Vector3[] vertices)
        {
            var vectorMin = vertices.Aggregate(Vector3.Min);
            var vectorMax = vertices.Aggregate(Vector3.Max);

            return new RvmFacetGroup(
                0,
                Matrix4x4.Identity,
                new RvmBoundingBox(vectorMin, vectorMax),
                new[]
                {
                    new RvmFacetGroup.RvmPolygon(new []
                    {
                        new RvmFacetGroup.RvmContour(vertices.Select(x => (x, Vector3.Zero)).ToArray())
                    })
                });
        }
    }
}
