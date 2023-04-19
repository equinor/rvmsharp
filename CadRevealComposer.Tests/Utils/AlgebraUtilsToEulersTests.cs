namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;
using System.Numerics;

[TestFixture]
public class AlgebraUtilsToEulersTests
{
    [Test]
    [TestCase(0.5f, 0.5f, 0.5f, 0.5f)]
    [TestCase(0.5f, 0.5f, 0.5f, -0.5f)]
    [TestCase(0.5f, 0.5f, -0.5f, 0.5f)]
    [TestCase(0.5f, 0.5f, -0.5f, -0.5f)]
    [TestCase(0.5f, -0.5f, 0.5f, 0.5f)]
    [TestCase(0.5f, -0.5f, 0.5f, -0.5f)]
    [TestCase(0.5f, -0.5f, -0.5f, 0.5f)]
    [TestCase(0.5f, -0.5f, -0.5f, -0.5f)]
    [TestCase(-0.5f, 0.5f, 0.5f, 0.5f)]
    [TestCase(-0.5f, 0.5f, 0.5f, -0.5f)]
    [TestCase(-0.5f, 0.5f, -0.5f, 0.5f)]
    [TestCase(-0.5f, 0.5f, -0.5f, -0.5f)]
    [TestCase(-0.5f, -0.5f, 0.5f, 0.5f)]
    [TestCase(-0.5f, -0.5f, 0.5f, -0.5f)]
    [TestCase(-0.5f, -0.5f, -0.5f, 0.5f)]
    [TestCase(-0.5f, -0.5f, -0.5f, -0.5f)]
    [TestCase(0f, -0.7071068f, 0f, 0.7071068f)]
    [TestCase(0.7071068f, 0f, -0.7071068f, 0f)]
    [TestCase(0.6123848f, 0.353532f, -0.6123848f, 0.353531957f)]
    [TestCase(-0.49999994f, 0.50000006f, -0.49999994f, -0.49999994f)]
    [TestCase(-0.47758815f, 0.52144945f, 0.47758815f, 0.52144945f)]
    [TestCase(0.6957228f, 0.12681098f, -0.6956885f, 0.12612033f)]
    [TestCase(-0.26970974f, 0.6510748f, 0.27152732f, 0.6554625f)]
    [TestCase(0.4993437f, 0.5006555f, 0.50065535f, -0.49934372f)]
    [TestCase(0.6611338f, -0.28299555f, 0.6289412f, -0.2508029f)]
    public void SimpleTest(float x, float y, float z, float w)
    {
        var q = Quaternion.Normalize(new Quaternion(x, y, z, w));
        var eulerAngles = q.ToEulerAngles();
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, eulerAngles.rollX);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, eulerAngles.pitchY);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, eulerAngles.yawZ);
        var qc = qz * qy * qx;

        var xa = Vector3.Transform(Vector3.UnitX, q);
        var ya = Vector3.Transform(Vector3.UnitY, q);
        var za = Vector3.Transform(Vector3.UnitZ, q);
        var oa = Vector3.Transform(Vector3.One, q);

        var xb = Vector3.Transform(Vector3.UnitX, qc);
        var yb = Vector3.Transform(Vector3.UnitY, qc);
        var zb = Vector3.Transform(Vector3.UnitZ, qc);
        var ob = Vector3.Transform(Vector3.One, qc);

        Console.WriteLine(xa.ToString("0.0000") + " = " + xb.ToString("0.0000"));
        Console.WriteLine(ya.ToString("0.0000") + " = " + yb.ToString("0.0000"));
        Console.WriteLine(za.ToString("0.0000") + " = " + zb.ToString("0.0000"));
        Console.WriteLine(oa.ToString("0.0000") + " = " + ob.ToString("0.0000"));
        Assert.That(xa.EqualsWithinTolerance(xb, 0.0001f));
        Assert.That(ya.EqualsWithinTolerance(yb, 0.0001f));
        Assert.That(za.EqualsWithinTolerance(zb, 0.0001f));
        Assert.That(oa.EqualsWithinTolerance(ob, 0.0001f));
    }

    [Test]
    [TestCase(0, 0, 0)]
    [TestCase(90, 0, 0)]
    [TestCase(0, 90, 0)]
    [TestCase(0, 0, 90)]
    [TestCase(90, 90, 90)]
    public void SimpleTest2(float yawDeg, float pitchDeg, float rollDeg)
    {
        var yawR = yawDeg * MathF.PI / 180;
        var pitchR = pitchDeg * MathF.PI / 180;
        var rollR = rollDeg * MathF.PI / 180;

        var q = Quaternion.CreateFromYawPitchRoll(yawR, pitchR, rollR);

        var eulerAngles = q.ToEulerAngles(); // FIXME: improve precision
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, eulerAngles.rollX);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, eulerAngles.pitchY);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, eulerAngles.yawZ);
        var qc = qz * qy * qx;

        var xa = Vector3.Transform(Vector3.UnitX, q);
        var ya = Vector3.Transform(Vector3.UnitY, q);
        var za = Vector3.Transform(Vector3.UnitZ, q);

        var xb = Vector3.Transform(Vector3.UnitX, qc);
        var yb = Vector3.Transform(Vector3.UnitY, qc);
        var zb = Vector3.Transform(Vector3.UnitZ, qc);

        Assert.That(xa.EqualsWithinTolerance(xb, 0.00001f));
        Assert.That(ya.EqualsWithinTolerance(yb, 0.00001f));
        Assert.That(za.EqualsWithinTolerance(zb, 0.00001f));
    }
}
