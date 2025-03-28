namespace Commons.Tests.Utils;

using Commons.Utils;

[TestFixture]
public class StringExtensionsTests
{
    [TestCase("test", StringComparison.OrdinalIgnoreCase, ExpectedResult = true)]
    [TestCase("Test", StringComparison.OrdinalIgnoreCase, ExpectedResult = true)]
    [TestCase("TEST", StringComparison.OrdinalIgnoreCase, ExpectedResult = true)]
    [TestCase("EST", StringComparison.OrdinalIgnoreCase, ExpectedResult = true)]
    [TestCase("is a test", StringComparison.OrdinalIgnoreCase, ExpectedResult = true)]
    [TestCase("is test", StringComparison.OrdinalIgnoreCase, ExpectedResult = false)]
    [TestCase("fest", StringComparison.OrdinalIgnoreCase, ExpectedResult = false)]
    [TestCase("This", StringComparison.Ordinal, ExpectedResult = true)]
    [TestCase("THIS", StringComparison.Ordinal, ExpectedResult = false)]
    public bool ContainsAny_GivenSingleKeyword_ThenCheckResult(string keyword, StringComparison comparisonType)
    {
        // Arrange
        const string testString = "This is a test string";

        // Act
        // Assert
        return testString.ContainsAny([keyword], comparisonType);
    }

    [TestCase("This is a test string", "this", "abc", "def", ExpectedResult = true)]
    [TestCase("This is a test string", "THIS", "abc", "def", ExpectedResult = true)]
    [TestCase("This is a test string", "this", "abc", "string", ExpectedResult = true)]
    [TestCase("This is a test string", "THIS", "abc", "STRING", ExpectedResult = true)]
    [TestCase("This is a string", "this", "abc", "string", ExpectedResult = true)]
    [TestCase("This is a string", "THIS", "abc", "STRING", ExpectedResult = true)]
    [TestCase("This is a test string", "this s", "abc", "stringy", ExpectedResult = false)]
    [TestCase("This is a test string", "THIS s", "abc", "STRINGY", ExpectedResult = false)]
    public bool ContainsAny_GivenThreeKeywords_ThenCheckResult(
        string testString,
        string keyword1,
        string keyword2,
        string keyword3
    )
    {
        // Arrange
        // Act
        // Assert
        return testString.ContainsAny([keyword1, keyword2, keyword3], StringComparison.OrdinalIgnoreCase);
    }
}
