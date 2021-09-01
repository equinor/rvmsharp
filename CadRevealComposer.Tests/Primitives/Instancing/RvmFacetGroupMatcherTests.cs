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
            var isMatch = RvmFacetGroupMatcher.TryCalculateTransform(
                Vector3.UnitX,
                Vector3.UnitY,
                Vector3.UnitZ,
                Vector3.One, 
                Vector3.UnitX,
                Vector3.UnitY,
                Vector3.UnitZ,
                Vector3.One,
                out var transform);
            
            Assert.IsTrue(isMatch, "Could not match.");
            Assert.AreEqual(Matrix4x4.Identity, transform.Value);
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
            var r = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var meshA = new RvmFacetGroup(0, Matrix4x4.Identity, new RvmBoundingBox(Vector3.One, Vector3.One),
                    new[]
                    {
                        new RvmFacetGroup.RvmPolygon(new[]
                        {
                            new RvmFacetGroup.RvmContour(new[]
                            {
                                (new Vector3(1, 1, 1), Vector3.One),
                                (new Vector3(0, 2, 1), Vector3.One),
                                (new Vector3(3, 2.5f, 1.5f), Vector3.One)
                            }),
                            new RvmFacetGroup.RvmContour(new[]
                            {
                                (new Vector3(1, 1, 1), Vector3.One),
                                (new Vector3(0, 2, 1), Vector3.One),
                                (new Vector3(3, 3, 3), Vector3.One)
                            })
                        })
                    });

                var eulers = RandomVector(r, 0, MathF.PI);
                var scale = RandomVector(r, 0.1f, 5.1f);
                var position = new Vector3(0, 0, 0);
                var q = Quaternion.CreateFromYawPitchRoll(eulers.X, eulers.Y, eulers.Z);

                var Mr = Matrix4x4.CreateFromQuaternion(q);
                var Ms = Matrix4x4.CreateScale(scale);
                var Mt = Matrix4x4.CreateTranslation(position);
                var Ma = Ms * Mr * Mt;

                var meshB = TransformFacetGroup(meshA, Ma);

                var isMatch = RvmFacetGroupMatcher.Match(meshA, meshB, out var transform);


                Matrix4x4.Decompose(Ma, out var s1, out var r1, out var t1);
                Matrix4x4.Decompose(transform.Value, out var s2, out var r2, out var t2);
                var ds = s1 - s2;
                var dr = r1 - r2;
                var dt = t1 - t2;

                Assert.IsTrue(isMatch, "Could not match.");
            }
        }

        [Test]
        public void FailingTest()
        {
            var r = new Random(12345);
            for (int i = 0; i < 1000; i++)
            {
                var meshA = new RvmFacetGroup(0, Matrix4x4.Identity, new RvmBoundingBox(Vector3.One, Vector3.One),
                    new[]
                    {
                        new RvmFacetGroup.RvmPolygon(new[]
                        {
                            new RvmFacetGroup.RvmContour(new[]
                            {
                                (new Vector3(1, 1, 1), Vector3.One),
                                (new Vector3(0, 2, 1), Vector3.One),
                                (new Vector3(3, 2.5f, 1.5f), Vector3.One),
                            }),
                            new RvmFacetGroup.RvmContour(new[]
                            {
                                (new Vector3(1, 1, 1), Vector3.One),
                                (new Vector3(0, 2, 1), Vector3.One),
                                (new Vector3(3, 3, 3), Vector3.One),
                            }),
                        })
                    });

                var eulers = new Vector3(3.044467f, 2.8217556f, 1.6506897f);
                var scale = new Vector3(2.1362286f, 4.620028f, 3.2072587f);

                var position = new Vector3(0, 0, 0);
                var q = Quaternion.CreateFromYawPitchRoll(eulers.X, eulers.Y, eulers.Z);

                var Mr = Matrix4x4.CreateFromQuaternion(q);
                var Ms = Matrix4x4.CreateScale(scale);
                var Mt = Matrix4x4.CreateTranslation(position);
                var Ma = Ms * Mr * Mt;

                var meshB = TransformFacetGroup(meshA, Ma);

                var isMatch = RvmFacetGroupMatcher.Match(meshA, meshB, out var transform);

                Matrix4x4.Decompose(Ma, out var s1, out var r1, out var t1);
                Matrix4x4.Decompose(transform.Value, out var s2, out var r2, out var t2);
                var ds = s1 - s2;
                var dr = r1 - r2;
                var dt = t1 - t2;

                Assert.IsTrue(isMatch, "Could not match.");
            }
        }

        private static Vector3 RandomVector(Random r, float minComponentValue, float maxComponentValue)
        {
            Func<float,float,float> rf = (min, max) =>  (float)r.NextDouble() * (max - min) + min;
            return new Vector3(rf(minComponentValue, maxComponentValue), rf(minComponentValue, maxComponentValue), rf(minComponentValue, maxComponentValue));
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
