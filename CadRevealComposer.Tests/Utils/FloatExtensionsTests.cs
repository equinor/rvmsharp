namespace CadRevealComposer.Tests.Utils;

using CadRevealComposer.Utils;
using Commons.Utils;

[TestFixture]
public class FloatExtensionsTests
{
    [Test]
    [TestCase(1, 1, 0.0005f, ExpectedResult = true)]
    [TestCase(0, 1, 0.0005f, ExpectedResult = false)]
    [TestCase(0.999f, 1, 0.001f, ExpectedResult = true)]
    [TestCase(0.9999f, 1, 0.00001f, ExpectedResult = false)]
    public bool Float_ApproximatelyEquals_ExplicitTolerance(float a, float b, float tolerance) =>
        a.ApproximatelyEquals(b, tolerance);

    [Test]
    [TestCase(1, 1, ExpectedResult = true)]
    [TestCase(0, 1, ExpectedResult = false)]
    [TestCase(0.999f, 1, ExpectedResult = false)]
    [TestCase(0.9999f, 1, ExpectedResult = false)]
    [TestCase(0.999999f, 1, ExpectedResult = true)]
    public bool Float_ApproximatelyEquals_DefaultTolerance(float a, float b) => a.ApproximatelyEquals(b);

    [Test]
    [TestCase(1, 1, ExpectedResult = true)]
    [TestCase(0, 1, ExpectedResult = false)]
    [TestCase(0.999f, 1, ExpectedResult = false)]
    [TestCase(0.9999f, 1, ExpectedResult = false)]
    [TestCase(0.999999f, 1, ExpectedResult = true)]
    [TestCase(0.999999f, 1, ExpectedResult = true)]
    public bool Float_SignificantDecimalsEquals_DefaultTolerance(float a, float b) =>
        a.SignificantDecimalsEquals(b, decimals: 5);

    /// <summary>
    /// This test visualizes some of the challenges with the significant decimals rounding
    /// </summary>
    [Test]
    [TestCase(0.51f, 0.50f, ExpectedResult = false)] // Nearly equal numbers are placed in different "buckets"
    [TestCase(0, 0.5f, ExpectedResult = true)] // Not even close numbers are bucketed together
    [TestCase(-0.5f, 0.5f, ExpectedResult = true)] // Not even close numbers are bucketed together (Rounding to closest even number)
    public bool Float_SignificantDecimalsEquals_Rounding(float a, float b) =>
        a.SignificantDecimalsEquals(b, decimals: 0);
}
