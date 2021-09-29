namespace CadRevealComposer.Tests.Utils
{
    using CadRevealComposer.Utils;
    using NUnit.Framework;
    using System.Numerics;

    [TestFixture]
    public class RotationTests
    {
        [Test]
        public void RotTest1()
        {
            var yaw = 13;
            var pitch = 43;
            var roll = 65;
            var q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
            var x = q.ToEulerAngles();
            var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitX, x.rollX);
            var q2 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, x.pitchY);
            var q3 = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, x.yawZ);
            var qt = q3 * q2 * q1;
            Assert.AreEqual(q, qt);

        }
    }
}