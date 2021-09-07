namespace CadRevealComposer.Tests.Primitives.Instancing
{
    using CadRevealComposer.Primitives.Instancing;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System;
    using System.Linq;
    using System.Numerics;
    using Utils;

    [TestFixture]
    public class RvmFacetGroupMatcherTests
    {
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
            Assert.AreEqual(Matrix4x4.Identity, transform);
        }

        private static RvmFacetGroup TransformFacetGroup(RvmFacetGroup group, Matrix4x4 matrix)
        {
            if (!Matrix4x4.Invert(matrix, out var matrixInvertedTransposed))
            {
                throw new ArgumentException("Matrix cannot be inverted");
            }
            return group with
            {
                Polygons = group.Polygons.Select(a => a with
                {
                    Contours = a.Contours.Select(c => c with
                    {
                        Vertices = c.Vertices.Select(v => (
                            Vector3.Transform(v.Vertex, matrix),
                            Vector3.Normalize(Vector3.TransformNormal(v.Normal, matrixInvertedTransposed)))).ToArray()
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
                var meshA = DataLoader.LoadTestJson<RvmFacetGroup>("simple_group.json");

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
        public void RotationPrecisionTest()
        {
            var meshA = DataLoader.LoadTestJson<RvmFacetGroup>("simple_group.json");

            // these parameters fail on low precision in dot product on from-to rotation
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

            Assert.IsTrue(isMatch, "Could not match.");
        }

        private static Vector3 RandomVector(Random r, float minComponentValue, float maxComponentValue)
        {
            Func<float,float,float> rf = (min, max) =>  (float)r.NextDouble() * (max - min) + min;
            return new Vector3(rf(minComponentValue, maxComponentValue), rf(minComponentValue, maxComponentValue), rf(minComponentValue, maxComponentValue));
        }
    }
}
