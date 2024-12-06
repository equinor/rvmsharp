namespace CadRevealFbxProvider.Tests.Attributes;

using CadRevealFbxProvider.Attributes;

[TestFixture]
public class ScaffoldingMetadataTests
{
    [Test]
    public void GivenMetadataKeyWeightAndVolumeWithValue_WhenCallingTryAdd_ThenValueIsAdded()
    {
        // Arrange
        var metadataWeight = new ScaffoldingMetadata();
        var metadataVolume = new ScaffoldingMetadata();

        // Act
        var retWeight = metadataWeight.TryAddValue("Grand total", "1234");
        var retVolume = metadataVolume.TryAddValue("Size (m\u00b3)", "5678");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retWeight, Is.True);
            Assert.That(metadataWeight.TotalWeight, Is.EqualTo("1234"));

            Assert.That(retVolume, Is.True);
            Assert.That(metadataVolume.TotalVolume, Is.EqualTo("5678"));
        });
    }

    [Test]
    public void GivenMetadataKeyWeightAndVolumeWithEmptyValue_WhenCallingTryAdd_ThenReturnFalse()
    {
        // Arrange
        var metadata = new ScaffoldingMetadata();

        // Act
        bool retWeight = metadata.TryAddValue("Grand total", "");
        bool retVolume = metadata.TryAddValue("Size (m\u00b3)", "");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retWeight, Is.False);
            Assert.That(retVolume, Is.False);
        });
    }

    [Test]
    public void GivenMetadataKeyWeightWithWhiteSpaceValue_WhenCallingTryAdd_ThenReturnFalse()
    {
        // Arrange
        var metadata = new ScaffoldingMetadata();

        // Act
        bool retSpace = metadata.TryAddValue("Grand total", " ");
        bool retCrlf = metadata.TryAddValue("Grand total", "\r\n");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retSpace, Is.False);
            Assert.That(retCrlf, Is.False);
        });
    }

    [Test]
    public void GivenMetadataKeyVolumeWithWhiteSpaceValue_WhenCallingTryAdd_ThenReturnFalse()
    {
        // Arrange
        var metadata = new ScaffoldingMetadata();

        // Act
        bool retSpace = metadata.TryAddValue("Size (m\u00b3)", " ");
        bool retCrlf = metadata.TryAddValue("Size (m\u00b3)", "\r\n");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retSpace, Is.False);
            Assert.That(retCrlf, Is.False);
        });
    }

    [Test]
    public void GivenAScaffoldingMetadataInstance_WhenPopulatedWithMinimumOfRequiredEntries_ThenHasExpectedValuesMethodIsTrueElseFalse()
    {
        // Arrange
        var metadataEmpty = new ScaffoldingMetadata();
        var metadata = new ScaffoldingMetadata();

        // Act
        metadata.TryAddValue("Work order", "1234");
        metadata.TryAddValue("Scaff build Operation number", "5678");
        metadata.TryAddValue("Dismantle Operation number", "9123");
        metadata.TryAddValue("Size (m\u00b3)", "1423");
        metadata.TryAddValue("Grand total", "4321");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(metadataEmpty.HasExpectedValues(), Is.False);
            Assert.That(metadata.HasExpectedValues(), Is.True);
        });
    }

    [Test]
    public void GivenADictionary_WhenPopulatedWithMinimumEntries_ThenHasExpectedValuesFromAttributesPerPartMethodReturnsTrueElseFalse()
    {
        // Arrange
        var targetDictComplete = new Dictionary<string, string>()
        {
            { "Work order", "1234" },
            { "Scaff build Operation number", "5678" },
            { "Dismantle Operation number", "91011" },
            { "Size (m\u00b3)", "121314" },
        };

        var targetDictIncomplete = new Dictionary<string, string>()
        {
            { "Work order", "1234" },
            { "Dismantle Operation number", "91011" }
        };

        var targetDictBeyondComplete = new Dictionary<string, string>()
        {
            { "Work order", "1234" },
            { "Scaff build Operation number", "5678" },
            { "Dismantle Operation number", "91011" },
            { "Size (m\u00b3)", "121314" },
            { "Test entry", "4321" }
        };

        var targetDictCompleteButEmptyValue = new Dictionary<string, string>()
        {
            { "Work order", "1234" },
            { "Scaff build Operation number", "" },
            { "Dismantle Operation number", "91011" },
            { "Size (m\u00b3)", "121314" },
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ScaffoldingMetadata.HasExpectedValuesFromAttributesPerPart(targetDictComplete), Is.True);
            Assert.That(ScaffoldingMetadata.HasExpectedValuesFromAttributesPerPart(targetDictIncomplete), Is.False);
            Assert.That(ScaffoldingMetadata.HasExpectedValuesFromAttributesPerPart(targetDictBeyondComplete), Is.True);
            Assert.That(
                ScaffoldingMetadata.HasExpectedValuesFromAttributesPerPart(targetDictCompleteButEmptyValue),
                Is.False
            );
        });
    }

    [Test]
    public void GivenAScaffoldingMetadataInstance_WhenItIsIncomplete_ThenTryWriteToGenericMetadataDictShouldThrow()
    {
        // Arrange
        var metadataEmpty = new ScaffoldingMetadata();
        var targetDict = new Dictionary<string, string>();

        // Assert
        Assert.Throws<Exception>(() => metadataEmpty.TryWriteToGenericMetadataDict(targetDict));
    }

    [Test]
    public void GivenAScaffoldingMetadataInstance_WhenItIsComplete_ThenTryWriteToGenericMetadataDictShouldFillTheTargetDict()
    {
        var metadata = new ScaffoldingMetadata();
        var targetDict = new Dictionary<string, string>();

        // Act
        metadata.TryAddValue("Work order", "1234");
        metadata.TryAddValue("Scaff build Operation number", "5678");
        metadata.TryAddValue("Dismantle Operation number", "9123");
        metadata.TryAddValue("Size (m\u00b3)", "1423");
        metadata.TryAddValue("Grand total", "4321");
        metadata.TryWriteToGenericMetadataDict(targetDict);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(targetDict["Scaffolding_WorkOrder_WorkOrderNumber"], Is.EqualTo("1234"));
            Assert.That(targetDict["Scaffolding_WorkOrder_BuildOperationNumber"], Is.EqualTo("5678"));
            Assert.That(targetDict["Scaffolding_WorkOrder_DismantleOperationNumber"], Is.EqualTo("9123"));
            Assert.That(targetDict["Scaffolding_TotalVolume"], Is.EqualTo("1423"));
            Assert.That(targetDict["Scaffolding_TotalWeight"], Is.EqualTo("4321"));
        });
    }
}
