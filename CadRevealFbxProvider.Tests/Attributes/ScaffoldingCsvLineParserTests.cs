namespace CadRevealFbxProvider.Tests.Attributes;

using CadRevealFbxProvider.Attributes;
using Csv;

public class ScaffoldingCsvLineParserTests
{
    private class TestCsvLine : ICsvLine
    {
        public bool HasColumn(string name)
        {
            throw new NotImplementedException();
        }

        public bool LineHasColumn(string name)
        {
            throw new NotImplementedException();
        }

        public required string[] Headers { get; set; }
        public required string[] Values { get; set; }
        public string Raw { get; set; } = "NOT SPECIFIED IN TEST MOCK";
        public int Index { get; set; } = 1;
        public int ColumnCount => Headers.Length;
        public string this[int index] => Values[index];
        public string this[string header] => Values[Array.IndexOf(Headers, header)];
    }

    [Test]
    public void ExtractColumnValueFromRow_WithValidHeader_ReturnsExpectedValue()
    {
        var row = new TestCsvLine { Headers = new[] { "A", "B", "C" }, Values = new[] { "1", "2", "3" } };
        var result = ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row);
        Assert.That(result, Is.EqualTo("2"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_WithValidWeightHeader_ReturnsWeight()
    {
        var row = new TestCsvLine { Headers = new[] { "Weight kg", "Other" }, Values = new[] { "42", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.EqualTo("42"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_WithEmptyWeightHeader_ReturnsNull()
    {
        var row = new TestCsvLine { Headers = new[] { "Weight kg", "Other" }, Values = new[] { "", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_WithManufacturerFalse_ReturnsDescriptionWithManufacturer()
    {
        var row = new TestCsvLine { Headers = new[] { "HAKI Description", "Other" }, Values = new[] { "desc", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, false);
        Assert.That(result, Does.Contain("desc"));
    }

    [Test]
    public void ExtractKeyFromCsvRow_WithValidKeyHeader_ReturnsKey()
    {
        var row = new TestCsvLine { Headers = new[] { "Item code", "Other" }, Values = new[] { "KEY123", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractKeyFromCsvRow(row, 0, "Item code");
        Assert.That(result, Is.EqualTo("KEY123"));
    }

    [Test]
    public void ExtractKeyFromCsvRow_WithMissingKeyHeader_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "Item code", "Other" }, Values = new[] { "", "foo" } };
        Assert.That(
            () => ScaffoldingCsvLineParser.ExtractKeyFromCsvRow(row, 0, "Item code"),
            Throws.TypeOf<ScaffoldingAttributeParsingException>()
        );
    }

    [Test]
    public void IsNumericSapColumn_WithNumericHeader_ReturnsTrue()
    {
        var row = new TestCsvLine { Headers = new[] { "Work order", "Other" }, Values = new[] { "123", "foo" } };
        var numericHeaders = new List<string> { "Work order" };
        var result = ScaffoldingCsvLineParser.IsNumericSapColumn(row, 0, numericHeaders);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNumericSapColumn_WithNonNumericHeader_ReturnsFalse()
    {
        var row = new TestCsvLine { Headers = new[] { "NotNumeric", "Other" }, Values = new[] { "123", "foo" } };
        var numericHeaders = new List<string> { "Work order" };
        var result = ScaffoldingCsvLineParser.IsNumericSapColumn(row, 0, numericHeaders);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ExtractColumnValueFromRow_WithMultipleNonEmptyValues_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "B", "B" }, Values = new[] { "2", "3" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row), Throws.Exception);
    }

    [Test]
    public void ExtractColumnValueFromRow_WithAllValuesEmpty_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "B", "B" }, Values = new[] { "", "" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row), Throws.Exception);
    }

    [Test]
    public void ExtractColumnValueFromRow_WithCaseInsensitiveHeaderMatch_ReturnsExpectedValue()
    {
        var row = new TestCsvLine { Headers = new[] { "b" }, Values = new[] { "2" } };
        var result = ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row);
        Assert.That(result, Is.EqualTo("2"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_WithMultipleNonEmptyWeights_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "Weight kg", "Vekt" }, Values = new[] { "42", "43" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row), Throws.Exception);
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_WithVektColumn_ReturnsWeight()
    {
        var row = new TestCsvLine { Headers = new[] { "Vekt" }, Values = new[] { "99" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.EqualTo("99"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_WithCaseInsensitiveHeaderMatch_ReturnsWeight()
    {
        var row = new TestCsvLine { Headers = new[] { "weight KG" }, Values = new[] { "55" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.EqualTo("55"));
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_WithMultipleNonEmptyDescriptions_ThrowsException()
    {
        var row = new TestCsvLine
        {
            Headers = new[] { "Description", "Description" },
            Values = new[] { "desc1", "desc2" },
        };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, true), Throws.Exception);
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_WithAllDescriptionsEmpty_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "Description", "Description" }, Values = new[] { "", "" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, true), Throws.Exception);
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_WithLayherManufacturerPrefix_ReturnsPrefixedDescription()
    {
        var row = new TestCsvLine { Headers = new[] { "Layher Description" }, Values = new[] { "desc" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, false);
        Assert.That(result, Is.EqualTo("LAYHER desc"));
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_WithCaseInsensitiveHeaderMatch_ReturnsDescription()
    {
        var row = new TestCsvLine { Headers = new[] { "description" }, Values = new[] { "desc" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, true);
        Assert.That(result, Is.EqualTo("desc"));
    }

    [Test]
    public void IsNumericSapColumn_WithCaseInsensitiveHeaderMatch_ReturnsTrue()
    {
        var row = new TestCsvLine { Headers = new[] { "work ORDER" }, Values = new[] { "123" } };
        var numericHeaders = new List<string> { "Work order" };
        var result = ScaffoldingCsvLineParser.IsNumericSapColumn(row, 0, numericHeaders);
        Assert.That(result, Is.True);
    }
}
