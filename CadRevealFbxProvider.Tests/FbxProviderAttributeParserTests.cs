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
                ScaffoldingMetadata.ModelAttributesPerPart.Length + 4,
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.TotalVolume, Is.EqualTo("9.76 m\u00b3"));
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.TotalVolume, Is.EqualTo("9.76 m\u00b3"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctVolumesInList_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndVolumeIsEmpty()
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
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
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctVolumesAndEmptyVolumeEntryInList_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndVolumeIsEmpty()
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.TotalVolume, Is.EqualTo("9.76 m\u00b3"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenMissingFirstBuildOperationNumber_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndBuildOperationNumberAsResult()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451", // Line with missing build operation number
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.BuildOperationNumber, Is.EqualTo("0100"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenMissingBuildOperationNumbersInBetween_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndBuildOperationNumberAsResult()
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
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453", // Line with missing build operation number
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.BuildOperationNumber, Is.EqualTo("0100"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctBuildOperationNumbersInList_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndBuildOperationNumberIsEmpty()
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
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0123;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.BuildOperationNumber, Is.Empty);
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctBuildOperationNumbersAndEmptyVolumeEntryInList_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndBuildOperationNumberIsEmpty()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123455", // Line with missing build operation number
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0123;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.BuildOperationNumber, Is.Empty);
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenIncludingAllBuildOperationNumberEntries_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndBuildOperationNumberAsResult()
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.BuildOperationNumber, Is.EqualTo("0100"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenMissingFirstDismantleOperationNumber_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndDismantleOperationNumberAsResult()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451", // Line with missing dismantle operation number
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.DismantleOperationNumber, Is.EqualTo("9000"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenMissingDismantleOperationNumbersInBetween_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndDismantleOperationNumberAsResult()
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
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453", // Line with missing dismantle operation number
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.DismantleOperationNumber, Is.EqualTo("9000"));
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctDismantleOperationNumbersInList_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndDismantleOperationNumberIsEmpty()
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
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;12345;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.DismantleOperationNumber, Is.Empty);
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenThereAreMultipleDistinctDismantleOperationNumbersAndEmptyVolumeEntryInList_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndDismantleOperationNumberIsEmpty()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();
        var fileLines = new List<string>
        {
            "Schedules-Export;;;",
            "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class;Size (m\u00b3);Length(m);Widht(m);Height(m);Covering (Y or N);Covering material;Last Updated;Item code",
            ";;;;;;;;;;;;;;;;;;;;;;;;",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123451",
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123455", // Line with missing dismantle operation number
            "Alu Pipe 48,3 X 1,50;1.50 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123452",
            "Base Element BS 600 X 34 Solid;5.50 kg;1;12345678;0100;12345;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123453",
            "Clips KF Aluhak KF 49x49;1.10 kg;1;12345678;0100;9000;;;;;;;;;56-LD-0026;;;9.76 m\u00b3;;;;;;;123454",
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.DismantleOperationNumber, Is.Empty);
        });
    }

    [Test]
    public void GivenLinesFromCSVFileAsStrings_WhenIncludingAllDismantleOperationNumberEntries_ThenWeHaveExpectedValuesOnReturnedMetadataAndNoThrowOnWritingToDictAndDismantleOperationNumberAsResult()
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;",
        };

        // Act
        var ret = new ScaffoldingAttributeParser().ParseAttributes(fileLines.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ret.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => ret.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(ret.scaffoldingMetadata.DismantleOperationNumber, Is.EqualTo("9000"));
        });
    }
}
