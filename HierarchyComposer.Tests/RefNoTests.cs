namespace HierarchyComposer.Tests
{
    using Model;
    using NUnit.Framework;
    using System;

    public class RefNoTests
    {
        [Test]
        [TestCase("X")]
        [TestCase("=ABC/123")]
        [TestCase("=123/123.4")] // Must be ints
        [TestCase("=123/")]
        [TestCase("=-123/321")] // Must not be negative
        [TestCase("=123/123/123")] // Two Numbers
        public void Parse_WhenGivenInvalidValues_ThrowsArgumentException(string invalidInput)
        {
            Assert.Throws<ArgumentException>(() => _ = RefNo.Parse(invalidInput));
        }

        [Test]
        public void Parse_WhenGivenNullValues_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _ = RefNo.Parse(null!));
        }

        [Test]
        [TestCase("=123/1337", 123, 1337)]
        [TestCase("=1/0", 1, 0)]
        [TestCase("=8129/0", 8129, 0)]
        public void Parse_WhenGivenValidValues_ReturnsRefNoWithExpectedDbAndSeq(string validInput, int expectedDb,
            int expectedSequence)
        {
            var result = RefNo.Parse(validInput);
            Assert.That(result.DbNo, Is.EqualTo(expectedDb));
            Assert.That(result.SequenceNo, Is.EqualTo(expectedSequence));
        }
    }
}