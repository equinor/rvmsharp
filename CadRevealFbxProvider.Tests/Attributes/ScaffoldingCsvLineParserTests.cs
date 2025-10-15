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
    public void ExtractColumnValueFromRow_ValidHeader_ReturnsExpectedValue()
    {
        var row = new TestCsvLine { Headers = new[] { "A", "B", "C" }, Values = new[] { "1", "2", "3" } };
        var result = ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row);
        Assert.That(result, Is.EqualTo("2"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_ValidWeightHeader_ReturnsWeight()
    {
        var row = new TestCsvLine { Headers = new[] { "Weight kg", "Other" }, Values = new[] { "42", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.EqualTo("42"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_EmptyWeightHeader_ReturnsNull()
    {
        var row = new TestCsvLine { Headers = new[] { "Weight kg", "Other" }, Values = new[] { "", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_ManufacturerFalse_ReturnsDescriptionManufacturer()
    {
        var row = new TestCsvLine { Headers = new[] { "HAKI Description", "Other" }, Values = new[] { "desc", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, false);
        Assert.That(result, Does.Contain("desc"));
    }

    [Test]
    public void ExtractKeyFromCsvRow_ValidKeyHeader_ReturnsKey()
    {
        var row = new TestCsvLine { Headers = new[] { "Item code", "Other" }, Values = new[] { "KEY123", "foo" } };
        var result = ScaffoldingCsvLineParser.ExtractKeyFromCsvRow(row, "Item code");
        Assert.That(result, Is.EqualTo("KEY123"));
    }

    [Test]
    public void ExtractKeyFromCsvRow_MissingKeyHeader_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "Item code", "Other" }, Values = new[] { "", "foo" } };
        HelperFunctions.AssertThrowsCustomScaffoldingException<ScaffoldingAttributeParsingException>(() =>
            ScaffoldingCsvLineParser.ExtractKeyFromCsvRow(row, "Item code")
        );
    }

    [Test]
    public void IsNumericSapColumn_NumericHeader_ReturnsTrue()
    {
        var row = new TestCsvLine { Headers = new[] { "Work order", "Other" }, Values = new[] { "123", "foo" } };
        var numericHeaders = new List<string> { "Work order" };
        var result = ScaffoldingCsvLineParser.IsNumericSapColumn(row, 0, numericHeaders);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsNumericSapColumn_NonNumericHeader_ReturnsFalse()
    {
        var row = new TestCsvLine { Headers = new[] { "NotNumeric", "Other" }, Values = new[] { "123", "foo" } };
        var numericHeaders = new List<string> { "Work order" };
        var result = ScaffoldingCsvLineParser.IsNumericSapColumn(row, 0, numericHeaders);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ExtractColumnValueFromRow_MultipleNonEmptyValues_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "B", "B" }, Values = new[] { "2", "3" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row), Throws.Exception);
    }

    [Test]
    public void ExtractColumnValueFromRow_AllValuesEmpty_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "B", "B" }, Values = new[] { "", "" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row), Throws.Exception);
    }

    [Test]
    public void ExtractColumnValueFromRow_CaseInsensitiveHeaderMatch_ReturnsExpectedValue()
    {
        var row = new TestCsvLine { Headers = new[] { "b" }, Values = new[] { "2" } };
        var result = ScaffoldingCsvLineParser.ExtractColumnValueFromRow("B", row);
        Assert.That(result, Is.EqualTo("2"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_MultipleNonEmptyWeights_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "Weight kg", "Vekt" }, Values = new[] { "42", "43" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row), Throws.Exception);
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_VektColumn_ReturnsWeight()
    {
        var row = new TestCsvLine { Headers = new[] { "Vekt" }, Values = new[] { "99" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.EqualTo("99"));
    }

    [Test]
    public void ExtractSingleWeightFromCsvRow_CaseInsensitiveHeaderMatch_ReturnsWeight()
    {
        var row = new TestCsvLine { Headers = new[] { "weight KG" }, Values = new[] { "55" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleWeightFromCsvRow(row);
        Assert.That(result, Is.EqualTo("55"));
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_MultipleNonEmptyDescriptions_ThrowsException()
    {
        var row = new TestCsvLine
        {
            Headers = new[] { "Description", "Description" },
            Values = new[] { "desc1", "desc2" },
        };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, true), Throws.Exception);
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_AllDescriptionsEmpty_ThrowsException()
    {
        var row = new TestCsvLine { Headers = new[] { "Description", "Description" }, Values = new[] { "", "" } };
        Assert.That(() => ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, true), Throws.Exception);
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_LayherManufacturerPrefix_ReturnsPrefixedDescription()
    {
        var row = new TestCsvLine { Headers = new[] { "Layher Description" }, Values = new[] { "desc" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, false);
        Assert.That(result, Is.EqualTo("LAYHER desc"));
    }

    [Test]
    public void ExtractSingleDescriptionFromCsvRow_CaseInsensitiveHeaderMatch_ReturnsDescription()
    {
        var row = new TestCsvLine { Headers = new[] { "description" }, Values = new[] { "desc" } };
        var result = ScaffoldingCsvLineParser.ExtractSingleDescriptionFromCsvRow(row, true);
        Assert.That(result, Is.EqualTo("desc"));
    }

    [Test]
    public void IsNumericSapColumn_CaseInsensitiveHeaderMatch_ReturnsTrue()
    {
        var row = new TestCsvLine { Headers = new[] { "work ORDER" }, Values = new[] { "123" } };
        var numericHeaders = new List<string> { "Work order" };
        var result = ScaffoldingCsvLineParser.IsNumericSapColumn(row, 0, numericHeaders);
        Assert.That(result, Is.True);
    }
}
