namespace RvmSharp.Tests.Operations;

using NUnit.Framework;
using RvmSharp.Operations;
using System.Numerics;

[TestFixture]
public class Matrix4x4HelpersTests
{
    [Test]
    public void CalculateTransformMatrix_ReturnsExpectedMatrix()
    {
        var pos = new Vector3(1f, 2, 3);
        var angle = 1;
        var rot = Quaternion.CreateFromAxisAngle(new Vector3(0.0f, -0.70710677f, 0.70710677f), angle);
        var scale = new Vector3(4, 5, 6);
        var definitiveMatrix =
            Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(pos);

        var helperMatrix = Matrix4x4Helpers.CalculateTransformMatrix(pos, rot, scale);

        Matrix4x4.Decompose(helperMatrix, out var outScale, out var outRot, out var outPos);

        Assert.That(helperMatrix, Is.EqualTo(definitiveMatrix));
        Assert.That(outScale, Is.EqualTo(scale));
        const float tolerance = 0.0000001f;
        Assert.That(outRot.X, Is.EqualTo(rot.X).Within(tolerance));
        Assert.That(outRot.Y, Is.EqualTo(rot.Y).Within(tolerance));
        Assert.That(outRot.Z, Is.EqualTo(rot.Z).Within(tolerance));
        Assert.That(outRot.W, Is.EqualTo(rot.W).Within(tolerance));
        Assert.That(outPos, Is.EqualTo(pos));
    }
}
