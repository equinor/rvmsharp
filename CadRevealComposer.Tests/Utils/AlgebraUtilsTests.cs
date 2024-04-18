namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;

[TestFixture]
public class AlgebraUtilsTests
{
    [DefaultFloatingPointTolerance(0.01d)]
    [TestCase(MathF.PI * 2 + 2, ExpectedResult = 2.0f)]
    [TestCase(MathF.PI, ExpectedResult = -MathF.PI)]
    [TestCase(MathF.PI * 2, ExpectedResult = 0.0f)]
    [TestCase(float.NegativeInfinity, ExpectedResult = float.NaN)]
    [TestCase(float.NaN, ExpectedResult = float.NaN)]
    public float NormalizeRadiansTest(float r) => AlgebraUtils.NormalizeRadians(r);
}
