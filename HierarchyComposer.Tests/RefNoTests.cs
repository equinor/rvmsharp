namespace HierarchyComposer.Tests;

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
    [TestCase("PREFIX WITH SPACES=123/321")] // Prefix with spaces is not yet found in real data. Change this if needed.
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
    [TestCase("=123/1337", 123, 1337, null)]
    [TestCase("=1/0", 1, 0, null)]
    [TestCase("=8129/0", 8129, 0, null)]
    [TestCase("ILTUBOF=8129/0", 8129, 0, "ILTUBOF")]
    public void Parse_WhenGivenValidValues_ReturnsRefNoWithExpectedDbAndSeq(string validInput, int expectedDb,
        int expectedSequence, string expectedPrefix)
    {
        var result = RefNo.Parse(validInput);
        Assert.That(result.DbNo, Is.EqualTo(expectedDb));
        Assert.That(result.SequenceNo, Is.EqualTo(expectedSequence));
        Assert.That(result.Prefix, Is.EqualTo(expectedPrefix));
    }

    [Test]
    [TestCase("ILTUBOF", "ILTUBOF")]
    [TestCase("123", "123")]
    [TestCase(" ", null)]
    [TestCase("", null)]
    public void Constructor_WhenGivenValidValues_ReturnsExpectedRefNo(string actualPrefix, string expectedPrefix)
    {
        var refNo = new RefNo(expectedPrefix, 123,321);
        Assert.That(refNo.Prefix, Is.EqualTo(expectedPrefix));
    }

    [Test]
    [TestCase("PREFIX WITH SPACES")]
    [TestCase(" ILTUBOF")] // Expect trimmed input
    public void Constructor_WhenGivenInvalidValues_Throws(string invalidPrefix)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new RefNo(invalidPrefix, 123, 321);
        });
    }
}