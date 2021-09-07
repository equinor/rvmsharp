namespace CadRevealComposer.Tests.Primitives.Instancing
{
    using CadRevealComposer.Primitives.Instancing;
    using NUnit.Framework;
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
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

                var meshB = meshA.TransformVertexData(Ma);

                var isMatch = RvmFacetGroupMatcher.Match(meshA, meshB, out Matrix4x4 _);
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

            var meshB = meshA.TransformVertexData(Ma);

            var isMatch = RvmFacetGroupMatcher.Match(meshA, meshB, out Matrix4x4 _);
            Assert.IsTrue(isMatch, "Could not match.");
        }

        private static Vector3 RandomVector(Random r, float minComponentValue, float maxComponentValue)
        {
            float Rf() => (float)r.NextDouble() * (maxComponentValue - minComponentValue) + minComponentValue;
            return new Vector3(Rf(), Rf(), Rf());
        }
    }
}
