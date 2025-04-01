﻿namespace CadRevealFbxProvider.Tests;

using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using CadRevealFbxProvider.Attributes;
using NUnit.Framework;

[TestFixture]
public class FbxProviderAttributeParserTests
{
    private readonly DirectoryInfo _attributeDirectory = new("TestSamples/attributes");
    private readonly List<string> fileLinesOneManufacturer = new List<string>
    {
        "Schedules-Export;;;",
        "Description;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class; Size(m³); Length(m); Widht(m); Height(m); Covering(Y or N); Covering material; Last Updated; Item code",
        ";;;;;;;;;;;;;;;;;;;;;;;;",
        "Alu Pipe 48,3 X 2,00;1.50 kg;1;12345;0040;0380;Stillas 1 topp;11-AA-101A;1111;;;;F1;BH90210;Vaerbeskyttelse;Vaerbeskyttelse;2;15.50 m\u00b3;;;;;;;123451",
        "Base Element BS 600 X 34 Hollow;3.40 kg;1;12345;0040;0380;Stillas 1 topp;11-AA-101A;1111;;;;F1;BH90210;Vaerbeskyttelse;Vaerbeskyttelse;2;15.50 m\u00b3;;;;;;;123452",
        "Base Element BS 600 X 34 Hollow;3.40 kg;1;12345;0040;0380;Stillas 1 topp;11-AA-101A;1111;;;;F1;BH90210;Vaerbeskyttelse;Vaerbeskyttelse;2;15.50 m\u00b3;;;;;;;123453",
        "Grand total: 3;8.3 kg;;;;;;;;;;;;;;;;;;;;;;;"
    };

    private readonly List<string> fileLinesTwoManufacturers = new List<string>
    {
        "Schedules-Export;;;",
        "Description;MAKI Description;MAKI Weight;Weight kg;Count;Work order;Scaff build Operation number;Dismantle Operation number;Scaff tag number;Job pack;Project number;Planned build date;Completion date;Dismantle date;Area;Discipline;Purpose;Scaff type;Load class; Size(m³); Length(m); Widht(m); Height(m); Covering(Y or N); Covering material; Last Updated; Item code",
        ";;;;;;;;;;;;;;;;;;;;;;;;",
        ";450 Lattice Beam 2220 Pockets AL;9.90 kg;;1;12345;0040;0380;Stillas 1 topp;11-AA-101A;1111;;;;F1;BH90210;Vaerbeskyttelse;Vaerbeskyttelse;2;15.50 m\u00b3;;;;;;;123451",
        "Base Element BS 600 X 34 Hollow;;;3.40 kg;1;12345;0040;0380;Stillas 1 topp;11-AA-101A;1111;;;;F1;BH90210;Vaerbeskyttelse;Vaerbeskyttelse;2;15.50 m\u00b3;;;;;;;123452",
        "Base Element BS 600 X 34 Hollow;;;3.40 kg;1;12345;0040;0380;Stillas 1 topp;11-AA-101A;1111;;;;F1;BH90210;Vaerbeskyttelse;Vaerbeskyttelse;2;15.50 m\u00b3;;;;;;;123453",
        "Grand total: 3;;9.9 kg;6.8 kg;;;;;;;;;;;;;;;;;;;;;;;"
    };

    [TestCase("/fbx_test_model.csv")]
    [TestCase("/fbx_test_model_with_header_on_row_two.csv")]
    public void ParseAttributes_ValidCsv_ExtractsCorrectAttributes(string csvFileNmae)
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
                ScaffoldingMetadata.ModelAttributesPerPart.Length + 5,
                Is.EqualTo(ScaffoldingMetadata.NumberOfModelAttributes)
            );
        });
    }

    [Test]
    public void ParseAttributes_TotalWeightMissingInCsv_ThrowsError()
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
    public void ParseAttributes_KeyAttributeMissingInCsv_ThrowsError()
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
    public void ParseAttributes_WrongAttributeCountInCsv_ThrowsError()
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

    // Testing matrix

    // COLUMNS
    // Test reading total volume from attribute file -> check
    // Test reading description and weight and
    // Creating enhanced description string

    // ROWS
    //  -- only 1 manufacturer -> check
    //  -- 2 manufacturers -> check
    //  -- 2 manufacturers where one of them is NOT Aluhak -> TODO:
    //  -- 3 manufacturers -> TODO:

    [Test]
    public void ParseAttributes_OneManufacturer_TotalWeightIsCorrect()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();

        // Act
        var result = new ScaffoldingAttributeParser().ParseAttributes(fileLinesOneManufacturer.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => result.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(result.scaffoldingMetadata.TotalWeight, Is.Not.Empty);
            Assert.That(result.scaffoldingMetadata.TotalWeight, Is.EqualTo("8.3"));
        });
    }

    [Test]
    public void ParseAttributes_TwoManufacturers_ExtractsCorrectTotalWeight()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();

        // Act
        var result = new ScaffoldingAttributeParser().ParseAttributes(fileLinesTwoManufacturers.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.scaffoldingMetadata.HasExpectedValues(), Is.True);
            Assert.DoesNotThrow(() => result.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));
            Assert.That(result.scaffoldingMetadata.TotalWeight, Is.Not.Empty);
            Assert.DoesNotThrow(
                () => float.Parse(result.scaffoldingMetadata.TotalWeight!, CultureInfo.InvariantCulture)
            );
            var tw = float.Parse(result.scaffoldingMetadata.TotalWeight!, CultureInfo.InvariantCulture);
            Assert.That(tw, Is.EqualTo(16.7).Within(0.000001f));
        });
    }

    [Test]
    public void ParseAttributes_OneManufacturer_ReadsDataWithoutException()
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();

        // Act
        var result = new ScaffoldingAttributeParser().ParseAttributes(fileLinesOneManufacturer.ToArray());

        // Assert
        Assert.That(result.attributesDictionary.Values.Count > 0);

        Assert.DoesNotThrow(() => result.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));

        // check if all lines are not null
        Assert.That(result.attributesDictionary.Values.All(v => v != null));
    }

    [Test]
    // (line index, element weight)
    [TestCase(0, 1.5f)]
    [TestCase(1, 3.4f)]
    [TestCase(2, 3.4f)]
    public void ParseAttributes_OneManufacturer_ExtractsWeightsCorrectly(int lineIndex, float weight)
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();

        // Act
        var result = new ScaffoldingAttributeParser().ParseAttributes(fileLinesOneManufacturer.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.attributesDictionary.Values.Count > 0);

            Assert.DoesNotThrow(() => result.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));

            // check if all lines are not null
            Assert.That(result.attributesDictionary.Values.All(v => v != null));

            var line = result.attributesDictionary.Values.ElementAt(lineIndex);

            var actualWeight = line!["Weight kg"].Replace(" kg", String.Empty);

            // Checks if all weights can be cast to floats
            Assert.DoesNotThrow(() =>
            {
                float.Parse(actualWeight, CultureInfo.InvariantCulture);
            });

            // Check their correctness
            Assert.That(float.Parse(actualWeight, CultureInfo.InvariantCulture), Is.EqualTo(weight));
        });
    }

    [Test]
    // (line index)
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void ParseAttributes_OneManufacturer_ExtractsDescription(int lineIndex)
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();

        // Act
        var result = new ScaffoldingAttributeParser().ParseAttributes(fileLinesOneManufacturer.ToArray());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.attributesDictionary.Values.Count > 0);

            Assert.DoesNotThrow(() => result.scaffoldingMetadata.TryWriteToGenericMetadataDict(targetDict));

            // check if all lines are not null
            Assert.That(result.attributesDictionary.Values.All(v => v != null));

            var line = result.attributesDictionary.Values.ElementAt(lineIndex);

            // checks if all lines have a description with something in it
            Assert.That(result.attributesDictionary.Values.All(line => line!["Description"].Length > 0));
        });
    }

    [Test]
    // (line index)
    [TestCase(0, "MAKI 450 Lattice Beam 2220 Pockets AL")]
    [TestCase(1, "Base Element BS 600 X 34 Hollow")]
    [TestCase(2, "Base Element BS 600 X 34 Hollow")]
    public void ParseAttributes_TwoManufacturers_ExtractsEnhancedDescription(int lineIndex, string enhancedDescr)
    {
        // Arrange
        var targetDict = new Dictionary<string, string>();

        // Act
        var result = new ScaffoldingAttributeParser().ParseAttributes(fileLinesTwoManufacturers.ToArray());
        var line = result.attributesDictionary.Values.ElementAt(lineIndex);

        // Assert
        Assert.Multiple(() =>
        {
            // checks if all lines have a description with something in it
            Assert.That(result.attributesDictionary.Values.All(line => line!["Description"].Length > 0));

            Assert.That(line!["Description"], Is.EqualTo(enhancedDescr));
        });
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
            "Grand total: 42;187.70 kg;;;;;;;;;;;;;;;;;;;;;;;"
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
