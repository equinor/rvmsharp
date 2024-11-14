namespace Commons.Tests.Utils;

using System.Numerics;
using Commons.Utils;

[TestFixture]
public class Matrix4X4ExtensionsTests
{
    [Test]
    public void IsDecomposable_EnsureInvalidMatrixIsInvalid()
    {
        const float inf = float.PositiveInfinity;
        var matrix = new Matrix4x4(inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf);
        Assert.That(matrix.IsDecomposable(), Is.False);
    }

    [Test]
    public void IsDecomposable_EnsureIdentityMatrixIsValid()
    {
        var matrix = Matrix4x4.Identity;
        Assert.That(matrix.IsDecomposable(), Is.True);
    }

    [Test]
    public void TryDecompose_WhenValidMatrix_ReturnsExpectedOutput()
    {
        var m =
            Matrix4x4.CreateFromYawPitchRoll(0, 0, 0) * Matrix4x4.CreateScale(1) * Matrix4x4.CreateTranslation(1, 2, 3);

        var result = m.TryDecompose();

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Value.scale, Is.EqualTo(Vector3.One));
            Assert.That(result.Value.rotation, Is.EqualTo(Quaternion.Identity));
            Assert.That(result.Value.translation, Is.EqualTo(new Vector3(1, 2, 3)));
        });
    }

    [Test]
    public void TryDecompose_WhenInvalidMatrix_ReturnsNull()
    {
        var m = Matrix4x4.CreateScale(float.NegativeInfinity);
        var result = m.TryDecompose();
        Assert.That(result, Is.Null);
    }
}
