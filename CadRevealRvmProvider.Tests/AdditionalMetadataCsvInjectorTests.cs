namespace CadRevealRvmProvider.Tests;

using CadRevealComposer;

[TestFixture]
public class AdditionalMetadataCsvInjectorTests
{
    private string _testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"equipment_metadata_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, recursive: true);
    }

    [Test]
    public void LoadAdditionalMetadataFromCsv_WhenCsvContainsValidData_ParsesByRefNo()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "valid.csv");
        File.WriteAllText(
            filePath,
            "RefNo,Tag,Description,Value\n"
                + "=123/321,20PA001,Main pump,100\n"
                + "=123/456,20PA002,Secondary pump,200\n"
        );

        // Act
        var result = AdditionalMetadataCsvInjector.LoadAdditionalMetadataFromCsv(new FileInfo(filePath));

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result["=123/321"]["Tag"], Is.EqualTo("20PA001"));
        Assert.That(result["=123/321"]["Description"], Is.EqualTo("Main pump"));
        Assert.That(result["=123/321"]["Value"], Is.EqualTo("100"));
        Assert.That(result["=123/321"].ContainsKey("RefNo"), Is.False);
    }

    [Test]
    public void AddCustomEchoAttributeMetadata_WhenPartialNodesMatch_OnlyUpdatesMatchingNodes()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "EquipmentMetadata.csv");
        File.WriteAllText(filePath, "RefNo,Tag,Category\n" + "=123/321,20PA001,Mechanical\n");

        var node1 = new CadRevealNode
        {
            Name = "/Equipment/Pump",
            TreeIndex = 1,
            Parent = null,
            Attributes = new Dictionary<string, string> { { "RefNo", "=123/321" } },
        };

        var node2 = new CadRevealNode
        {
            Name = "/Equipment/Valve",
            TreeIndex = 2,
            Parent = null,
            Attributes = new Dictionary<string, string> { { "RefNo", "=123/456" } },
        };

        // Act
        AdditionalMetadataCsvInjector.AddCustomEchoAttributeMetadata([node1, node2], new FileInfo(filePath));

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(node1.Attributes["Tag"], Is.EqualTo("20PA001"));
            Assert.That(node1.Attributes["EchoInjected_Tag"], Is.EqualTo("true"));
            Assert.That(node1.Attributes["Category"], Is.EqualTo("Mechanical"));
            Assert.That(node1.Attributes["EchoInjected_Category"], Is.EqualTo("true"));
            Assert.That(node1.Attributes.ContainsKey("Tag_Original"), Is.False);
            Assert.That(node1.Attributes.ContainsKey("Category_Original"), Is.False);
            Assert.That(node2.Attributes, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void ApplyMetadataToNodes_WhenAttributeAlreadyExists_ReplacesValueAndPreservesOriginal()
    {
        // Arrange
        var node = new CadRevealNode
        {
            Name = "/Equipment/Pump",
            TreeIndex = 1,
            Parent = null,
            Attributes = new Dictionary<string, string> { { "RefNo", "=123/321" }, { "Tag", "OldTag" } },
        };

        var metadataByRefNo = new Dictionary<string, Dictionary<string, string>>
        {
            ["=123/321"] = new() { ["Tag"] = "NewTag", ["Category"] = "Mechanical" },
        };

        // Act
        var matched = AdditionalMetadataCsvInjector.ApplyMetadataToNodes([node], metadataByRefNo);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(matched, Is.EqualTo(1));
            Assert.That(node.Attributes["Tag"], Is.EqualTo("NewTag"));
            Assert.That(node.Attributes["EchoInjected_Tag"], Is.EqualTo("true"));
            Assert.That(node.Attributes["Tag_Original"], Is.EqualTo("OldTag"));
            Assert.That(node.Attributes["Category"], Is.EqualTo("Mechanical"));
            Assert.That(node.Attributes["EchoInjected_Category"], Is.EqualTo("true"));
            Assert.That(node.Attributes.ContainsKey("Category_Original"), Is.False);
            Assert.That(node.Attributes["RefNo"], Is.EqualTo("=123/321"));
        }
    }
}
