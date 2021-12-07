namespace RvmSharp.Tests.Operations
{
    using NUnit.Framework;
    using RvmSharp.Operations;

    [TestFixture]
    public class FloatExtensionsTests
    {
        [Test]
        [TestCase(float.NaN, ExpectedResult = false)]
        [TestCase(float.NegativeInfinity, ExpectedResult = false)]
        [TestCase(float.PositiveInfinity, ExpectedResult = false)]
        [TestCase(float.Epsilon, ExpectedResult = true)]
        [TestCase(0, ExpectedResult = true)]
        [TestCase(float.MaxValue, ExpectedResult = true)]
        [TestCase(float.MinValue, ExpectedResult = true)]
        public bool IsFiniteTests(float input) => input.IsFinite();
    }
}