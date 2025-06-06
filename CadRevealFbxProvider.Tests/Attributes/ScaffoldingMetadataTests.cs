namespace CadRevealFbxProvider.Tests.Attributes;

using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
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
    public void GivenMetadataKeyWeightWithEmptyValue_WhenCallingTryAdd_ThenReturnFalse()
    {
        // Arrange
        var metadata = new ScaffoldingMetadata();

        // Act
        bool retWeight = metadata.TryAddValue("Grand total", "");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retWeight, Is.False);
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
        bool retSpace = metadata.TryAddValue("Work order", " ");
        bool retCrlf = metadata.TryAddValue("Work order", "\r\n");

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
        metadata.TryAddValue("Grand total", "4321");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(metadataEmpty.ModelMetadataHasExpectedValues(), Is.False);
            Assert.That(metadata.ModelMetadataHasExpectedValues(), Is.True);
        });
    }

    [Test]
    public void PartMetadataHasExpectedValues_WhenPopulatedWithMinimumEntries_ThenHasExpectedValuesFromAttributesPerPart()
    {
        // Arrange
        var targetDictComplete = new Dictionary<string, string>()
        {
            { "Work order", "1234" },
            { "Scaff build Operation number", "5678" },
            { "Dismantle Operation number", "91011" },
        };

        var targetDictIncomplete = new Dictionary<string, string>()
        {
            //            { "Work order", "1234" },
            { "Scaff build Operation number", "5678" },
            //            { "Dismantle Operation number", "91011" }
        };

        var targetDictEmpty = new Dictionary<string, string>() { };

        var targetDictBeyondComplete = new Dictionary<string, string>()
        {
            { "Work order", "1234" },
            { "Scaff build Operation number", "5678" },
            { "Dismantle Operation number", "91011" },
            { "Test entry", "4321" },
        };

        var targetDictCompleteButEmptyValue = new Dictionary<string, string>()
        {
            { "Work order", "" },
            //            { "Scaff build Operation number", "" },
            //            { "Dismantle Operation number", "91011" }
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ScaffoldingMetadata.PartMetadataHasExpectedValues(targetDictComplete), Is.True);
            Assert.That(ScaffoldingMetadata.PartMetadataHasExpectedValues(targetDictEmpty), Is.False);
            Assert.That(ScaffoldingMetadata.PartMetadataHasExpectedValues(targetDictIncomplete), Is.False);
            Assert.That(ScaffoldingMetadata.PartMetadataHasExpectedValues(targetDictBeyondComplete), Is.True);
            Assert.That(ScaffoldingMetadata.PartMetadataHasExpectedValues(targetDictCompleteButEmptyValue), Is.False);
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
        // Arrange
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

    [Test]
    public void GivenScaffoldingMetadataWeightAndVolume_WhenValuesHaveUnits_ThenOutputIsStillWithUnits()
    {
        // Arrange
        var metadataWeight1 = new ScaffoldingMetadata();
        var metadataWeight2 = new ScaffoldingMetadata();
        var metadataVolume1 = new ScaffoldingMetadata();

        // Act
        var retWeight1 = metadataWeight1.TryAddValue("Grand total", "12.34 kg");
        var retWeight2 = metadataWeight2.TryAddValue("Grand total", "12_34e-45kg");
        var retVolume1 = metadataVolume1.TryAddValue("Size (m\u00b3)", "567.8 m\u00b3");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retWeight1, Is.True);
            Assert.That(metadataWeight1.TotalWeight, Is.EqualTo("12.34 kg"));

            Assert.That(retWeight2, Is.True);
            Assert.That(metadataWeight2.TotalWeight, Is.EqualTo("12_34e-45kg"));

            Assert.That(retVolume1, Is.True);
            Assert.That(metadataVolume1.TotalVolume, Is.EqualTo("567.8 m\u00b3"));
        });
    }
}
