namespace CadRevealComposer.Tests.Utils
{
    using CadRevealComposer.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class FloatExtensionsTests
    {
        [Test]
        [TestCase(1, 1, 0.0005f, ExpectedResult = true)]
        [TestCase(0, 1, 0.0005f, ExpectedResult = false)]
        [TestCase(0.999f, 1, 0.001f, ExpectedResult = true)]
        [TestCase(0.9999f, 1, 0.00001f, ExpectedResult = false)]
        public bool Float_NearlyEquals_ExplicitTolerance(float a, float b, float tolerance) => a.NearlyEquals(b, tolerance);

        [Test]
        [TestCase(1, 1, ExpectedResult = true)]
        [TestCase(0, 1, ExpectedResult = false)]
        [TestCase(0.999f, 1, ExpectedResult = false)]
        [TestCase(0.9999f, 1, ExpectedResult = false)]
        [TestCase(0.999999f, 1, ExpectedResult = true)]
        public bool Float_NearlyEquals_DefaultTolerance(float a, float b) => a.NearlyEquals(b);
    }
}