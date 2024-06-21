namespace Commons.Tests.Utils;

using System.Numerics;
using Commons.Utils;

[TestFixture]
public class MatrixExtensions
{
    [Test]
    public void EnsureInvalidMatrixIsInvalid()
    {
        const float inf = float.PositiveInfinity;
        var matrix = new Matrix4x4(inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf, inf);
        Assert.That(matrix.IsDecomposable(), Is.False);
    }

    [Test]
    public void EnsureIdentityMatrixIsValid()
    {
        var matrix = Matrix4x4.Identity;
        Assert.That(matrix.IsDecomposable(), Is.True);
    }
}
