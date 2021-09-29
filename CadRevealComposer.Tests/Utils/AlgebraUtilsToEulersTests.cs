namespace CadRevealComposer.Tests.Utils
{
    using CadRevealComposer.Utils;
    using NUnit.Framework;
    using System;
    using System.Numerics;

    [TestFixture]
    public class AlgebraUtilsToEulersTests
    {
        [Test]
        public void SimpleTest()
        {
            var q = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f);
            var x = q.ToEulerAngles();
            var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x.rollX);
            var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, x.pitchY);
            var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, x.yawZ);
            var qc = qz * qy * qx;

            var xa = Vector3.Transform(Vector3.UnitX, q);
            var ya = Vector3.Transform(Vector3.UnitY, q);
            var za = Vector3.Transform(Vector3.UnitZ, q);

            var xb = Vector3.Transform(Vector3.UnitX, qc);
            var yb = Vector3.Transform(Vector3.UnitY, qc);
            var zb = Vector3.Transform(Vector3.UnitZ, qc);

            Assert.That(xa.ApproximatelyEquals(xb));
            Assert.That(ya.ApproximatelyEquals(yb));
            Assert.That(za.ApproximatelyEquals(zb));
        }

        [Test]
        [TestCase(0, 0, 0)]
        [TestCase(90, 0, 0)]
        [TestCase(0, 90, 0)]
        [TestCase(0, 0, 90)]
        [TestCase(90, 90, 90)]
        public void SimpleTest(float yawDeg, float pitchDeg, float rollDeg)
        {
            var yawR = yawDeg * MathF.PI / 180;
            var pitchR = pitchDeg * MathF.PI / 180;
            var rollR = rollDeg * MathF.PI / 180;
            var q = Quaternion.CreateFromYawPitchRoll(yawR, pitchR, rollR);
            var x = q.ToEulerAngles(); // FIXME: improve precision
            var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x.rollX);
            var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, x.pitchY);
            var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, x.yawZ);
            var qc = qz * qy * qx;

            var xa = Vector3.Transform(Vector3.UnitX, q);
            var ya = Vector3.Transform(Vector3.UnitY, q);
            var za = Vector3.Transform(Vector3.UnitZ, q);

            var xb = Vector3.Transform(Vector3.UnitX, qc);
            var yb = Vector3.Transform(Vector3.UnitY, qc);
            var zb = Vector3.Transform(Vector3.UnitZ, qc);

            Assert.That(xa.ApproximatelyEquals(xb, 0.001f));
            Assert.That(ya.ApproximatelyEquals(yb, 0.001f));
            Assert.That(za.ApproximatelyEquals(zb, 0.001f));
        }

    }
}