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

    [Test]
    public void MatrixContainsInfiniteValue_Test()
    {
        //Arrange
        Matrix4x4 matrix = new Matrix4x4();
        matrix.M11 = 1f;
        matrix.M12 = 2f;
        matrix.M13 = 3f;
        matrix.M14 = 4f;
        matrix.M21 = 5f;
        matrix.M22 = 6f;
        matrix.M23 = 7f;
        matrix.M24 = 8f;
        matrix.M31 = 9f;
        matrix.M32 = 10f;
        matrix.M33 = 11f;
        matrix.M34 = 12f;
        matrix.M41 = 13f;
        matrix.M42 = 14f;
        matrix.M43 = 15f;
        matrix.M44 = 16f;

        //Act
        bool result = Matrix4x4Helpers.MatrixContainsInfiniteValue(matrix);

        //Assert
        Assert.IsFalse(result);
    }
}
