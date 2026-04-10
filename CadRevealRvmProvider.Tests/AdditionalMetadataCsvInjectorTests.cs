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
            "RefNo,Name,Description,Value\n"
                + "=123/321,Pump A,Main pump,100\n"
                + "=123/456,Pump B,Secondary pump,200\n"
        );

        // Act
        var result = AdditionalMetadataCsvInjector.LoadAdditionalMetadataFromCsv(new FileInfo(filePath));

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(2));
        Assert.That(result["=123/321"]["Name"], Is.EqualTo("Pump A"));
        Assert.That(result["=123/321"]["Description"], Is.EqualTo("Main pump"));
        Assert.That(result["=123/321"]["Value"], Is.EqualTo("100"));
        Assert.That(result["=123/321"].ContainsKey("RefNo"), Is.False);
    }

    [Test]
    public void AddCustomEchoAttributeMetadata_WhenPartialNodesMatch_OnlyUpdatesMatchingNodes()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "EquipmentMetadata.csv");
        File.WriteAllText(filePath, "RefNo,Name,Category\n" + "=123/321,Pump A,Mechanical\n");

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
            Assert.That(node1.Attributes["Echo_Name"], Is.EqualTo("Pump A"));
            Assert.That(node1.Attributes["Echo_Category"], Is.EqualTo("Mechanical"));
            Assert.That(node2.Attributes, Has.Count.EqualTo(1));
        }
    }
}
