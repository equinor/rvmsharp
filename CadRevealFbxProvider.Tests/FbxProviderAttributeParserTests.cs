namespace CadRevealFbxProvider.Tests;

using CadRevealFbxProvider.Attributes;
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

        Assert.Multiple(() =>
        {
            // expects three lines with missing attributes
            Assert.That(countNodesWithMissingAttrib, Is.EqualTo(3));
            Assert.That(metadata.HasExpectedValues());

            Assert.That(
                ScaffoldingMetadata.ModelAttributesPerPart.Length + 2,
                Is.EqualTo(ScaffoldingMetadata.NumberOfModelAttributes)
            );
        });
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

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenNotIncludingAnyVolume_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndVolumeIsEmpty()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;;;;;;;;123451",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.TotalVolume, Is.Empty);
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenMissingFirstVolume_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndVolumeAsResult()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;;;;;;;;123451", // Line with missing volume
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.TotalVolume, Is.EqualTo("9.76"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenMissingVolumeInBetween_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndVolumeAsResult()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;;;;;;;;123453", // Line with missing volume
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.TotalVolume, Is.EqualTo("9.76"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctVolumesInList_ThenThrowOnAttributeParsing()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;3.98 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
        };

        // Act + Assert
        Assert.Throws<Exception>(() => new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray()));
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctVolumesAndEmptyVolumeEntryInList_ThenThrowOnAttributeParsing()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;;;;;;;;123455", // Line with missing volume
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;3.98 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
        };

        // Act + Assert
        Assert.Throws<Exception>(() => new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray()));
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenIncludingAllVolumeEntries_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndVolumeAsResult()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123455",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.TotalVolume, Is.EqualTo("9.76"));
        });
    }
}
