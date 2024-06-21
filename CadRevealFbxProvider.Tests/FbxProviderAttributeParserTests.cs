namespace CadRevealFbxProvider.Tests;

using Attributes;
using NUnit.Framework;

[TestFixture]
public class FbxProviderAttributeParserTests
{
    private readonly DirectoryInfo _attributeDirectory = new("TestSamples/attributes");

    [TestCase("/fbx_test_model.csv")]
    [TestCase("/fbx_test_model_with_header_on_row_two.csv")]
    public void ParseCorrectAttributesTest(string csvFileNmae)
    {
        string infoTextFilename = _attributeDirectory.FullName + csvFileNmae;
        var lines = File.ReadAllLines(infoTextFilename);
        (var attributes, var metadata) = new ScaffoldingAttributeParser().ParseAttributes(lines);

        int countNodesWithMissingAttrib = 0;
        foreach (var attribute in attributes)
        {
            if (attribute.Value != null)
            {
                Assert.That(attribute.Value, Has.Count.EqualTo(ScaffoldingAttributeParser.NumberOfAttributesPerPart));
            }
            else
            {
                countNodesWithMissingAttrib++;
            }
        }
        // expects three lines with missing attributes
        Assert.That(countNodesWithMissingAttrib, Is.EqualTo(3));
        Assert.That(metadata.HasExpectedValues());

        Assert.That(
            ScaffoldingMetadata.ModelAttributesPerPart.Length + 1,
            Is.EqualTo(ScaffoldingMetadata.NumberOfModelAttributes)
        );
    }

    [Test]
    public void MissingTotalWeightTest()
    {
        Assert.Throws<Exception>(
            () =>
            {
                string infoTextFilename = _attributeDirectory.FullName + "/missing_total_weight.csv";
                var lines = File.ReadAllLines(infoTextFilename);
                new ScaffoldingAttributeParser().ParseAttributes(lines);
            },
            "Was expecting an exception saying that the key total weight is missing, but got none"
        );
    }

    [Test]
    public void MissingKeyAttributesTest()
    {
        Assert.Throws<Exception>(
            () =>
            {
                string infoTextFilename = _attributeDirectory.FullName + "/missing_key_attribute.csv";
                var lines = File.ReadAllLines(infoTextFilename);
                new ScaffoldingAttributeParser().ParseAttributes(lines);
            },
            "Was expecting an exception saying that the key attribute is missing, but got none"
        );
    }

    [Test]
    public void WrongNumberAttributesTest()
    {
        Assert.Throws<Exception>(
            () =>
            {
                string infoTextFilename = _attributeDirectory.FullName + "/wrong_attribute_count.csv";
                var lines = File.ReadAllLines(infoTextFilename);
                new ScaffoldingAttributeParser().ParseAttributes(lines);
            },
            "Was expecting an exception saying that the attribute count is off, but got none"
        );
    }
}
