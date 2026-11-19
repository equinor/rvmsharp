namespace CadRevealRvmProvider.Tests;

using System.Text.Json;
using CadRevealComposer;

[TestFixture]
public class TagMetadataInjectorTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Directory.CreateTempSubdirectory("tag_injector_tests_").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void ApplyMetadataToNodes_MatchingTag_AddsMetadataAndPreservesNodeAttributes()
    {
        var repository = LoadRepository(new TagMetadataEntry("TEST-LINE-001", 6, "Imaginary line", "42"));
        var node = CreateNode("TEST-LINE-001");
        var expectedAttributes = new Dictionary<string, string>
        {
            ["Tag"] = "TEST-LINE-001",
            ["Description"] = "Invented equipment description",
            ["TagMetadata.CategoryId"] = "6",
            ["TagMetadata.CategoryDescription"] = "Imaginary line",
            ["TagMetadata.System"] = "42",
        };

        var summary = TagMetadataInjector.ApplyMetadataToNodes([node], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Attributes, Is.EquivalentTo(expectedAttributes));
            Assert.That(summary, Is.EqualTo(new TagEnrichmentSummary(1, 1, 0, 0, 0)));
        }
    }

    [TestCase("UNKNOWN", 1)]
    [TestCase("", 1)]
    [TestCase(" ", 1)]
    [TestCase(null, 0)]
    public void ApplyMetadataToNodes_InvalidOrMissingTag_PreservesUnrelatedAttributes(string? tag, int removedTags)
    {
        var repository = LoadRepository(new TagMetadataEntry("TEST-LINE-001", 6, "Imaginary line", "42"));
        var node = CreateNode(tag);
        var expectedAttributes = new Dictionary<string, string> { ["Description"] = "Invented equipment description" };

        var summary = TagMetadataInjector.ApplyMetadataToNodes([node], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Attributes, Is.EquivalentTo(expectedAttributes));
            Assert.That(summary, Is.EqualTo(new TagEnrichmentSummary(0, 0, removedTags, removedTags, 0)));
        }
    }

    [TestCase("TEST-LINE-*")]
    [TestCase("*LINE-001")]
    [TestCase("*LINE*")]
    public void ApplyMetadataToNodes_Wildcard_EnrichesOnlyMatchingLinesAndPreservesPattern(string pattern)
    {
        var repository = LoadRepository(
            new("TEST-LINE-001", 6, "Imaginary line", "42"),
            new("TEST-LINE-PUMP-LINE-001", 7, "Imaginary pump", "99"),
            new("UNRELATED-001", 6, "Other imaginary line", "88")
        );
        var node = CreateNode(pattern);

        var summary = TagMetadataInjector.ApplyMetadataToNodes([node], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                repository.FindMatches(pattern).Select(entry => entry.TagNo),
                Is.EqualTo(new[] { "TEST-LINE-001" })
            );
            Assert.That(node.Attributes["Tag"], Is.EqualTo(pattern));
            Assert.That(node.Attributes["TagMetadata.System"], Is.EqualTo("42"));
            Assert.That(summary, Is.EqualTo(new TagEnrichmentSummary(1, 1, 0, 0, 0)));
        }
    }

    [Test]
    public void ApplyMetadataToNodes_NonLineCategory_RequiresExactTag()
    {
        var repository = LoadRepository(new TagMetadataEntry("TEST-PUMP-001", 7, "Imaginary pump", "42"));
        var exact = CreateNode("TEST-PUMP-001");
        var wildcard = CreateNode("TEST-PUMP-*");

        var summary = TagMetadataInjector.ApplyMetadataToNodes([exact, wildcard], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exact.Attributes["Tag"], Is.EqualTo("TEST-PUMP-001"));
            Assert.That(exact.Attributes["TagMetadata.CategoryId"], Is.EqualTo("7"));
            Assert.That(
                wildcard.Attributes,
                Is.EquivalentTo(new Dictionary<string, string> { ["Description"] = "Invented equipment description" })
            );
            Assert.That(
                summary,
                Is.EqualTo(
                    new TagEnrichmentSummary(
                        MatchedNodes: 1,
                        EnrichedNodes: 1,
                        UnmatchedNodes: 1,
                        RemovedTags: 1,
                        SkippedExistingKeys: 0
                    )
                )
            );
        }
    }

    [TestCase("Imaginary line", "42", true)]
    [TestCase("Different imaginary line", "42", false)]
    [TestCase("Imaginary line", "99", false)]
    public void ApplyMetadataToNodes_MultipleWildcardMatches_EnrichesOnlyWhenMetadataAgrees(
        string otherDescription,
        string otherSystem,
        bool shouldEnrich
    )
    {
        var repository = LoadRepository(
            new("TEST-LINE-001", 6, "Imaginary line", "42"),
            new("TEST-LINE-002", 6, otherDescription, otherSystem)
        );
        var node = CreateNode("TEST-LINE-*");
        var expectedAttributes = new Dictionary<string, string>(node.Attributes);
        if (shouldEnrich)
        {
            expectedAttributes["TagMetadata.CategoryId"] = "6";
            expectedAttributes["TagMetadata.CategoryDescription"] = "Imaginary line";
            expectedAttributes["TagMetadata.System"] = "42";
        }

        var summary = TagMetadataInjector.ApplyMetadataToNodes([node], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Attributes, Is.EquivalentTo(expectedAttributes));
            Assert.That(
                summary,
                Is.EqualTo(
                    new TagEnrichmentSummary(
                        MatchedNodes: 1,
                        EnrichedNodes: shouldEnrich ? 1 : 0,
                        UnmatchedNodes: 0,
                        RemovedTags: 0,
                        SkippedExistingKeys: 0
                    )
                )
            );
        }
    }

    [Test]
    public void ApplyMetadataToNodes_WildcardPrefersAsBuiltButExactHistoricalTagRemainsValid()
    {
        var repository = LoadRepository(
            new("TEST-LINE-OLD", 6, "Imaginary line", "99", "H"),
            new("TEST-LINE-NEW-1", 6, "Imaginary line", "42", "A"),
            new("TEST-LINE-NEW-2", 6, "Imaginary line", "42", "A")
        );
        var wildcard = CreateNode("TEST-LINE-*");
        var historical = CreateNode("TEST-LINE-OLD");

        var summary = TagMetadataInjector.ApplyMetadataToNodes([wildcard, historical], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                repository.FindMatches("TEST-LINE-*").Select(entry => entry.TagNo),
                Is.EquivalentTo(new[] { "TEST-LINE-NEW-1", "TEST-LINE-NEW-2" })
            );
            Assert.That(wildcard.Attributes["Tag"], Is.EqualTo("TEST-LINE-*"));
            Assert.That(wildcard.Attributes["TagMetadata.System"], Is.EqualTo("42"));
            Assert.That(historical.Attributes["Tag"], Is.EqualTo("TEST-LINE-OLD"));
            Assert.That(historical.Attributes["TagMetadata.System"], Is.EqualTo("99"));
            Assert.That(
                summary,
                Is.EqualTo(
                    new TagEnrichmentSummary(
                        MatchedNodes: 2,
                        EnrichedNodes: 2,
                        UnmatchedNodes: 0,
                        RemovedTags: 0,
                        SkippedExistingKeys: 0
                    )
                )
            );
        }
    }

    [Test]
    public void ApplyMetadataToNodes_NoAsBuiltMatches_KeepsHistoricalAndUnspecifiedStatuses()
    {
        var repository = LoadRepository(
            new("TEST-LINE-OLD", 6, "Imaginary line", "42", "H"),
            new("TEST-LINE-OTHER", 6, "Imaginary line", "42")
        );
        var node = CreateNode("TEST-LINE-*");

        var summary = TagMetadataInjector.ApplyMetadataToNodes([node], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                repository.FindMatches("TEST-LINE-*").Select(entry => entry.TagNo),
                Is.EquivalentTo(new[] { "TEST-LINE-OLD", "TEST-LINE-OTHER" })
            );
            Assert.That(node.Attributes["Tag"], Is.EqualTo("TEST-LINE-*"));
            Assert.That(node.Attributes["TagMetadata.System"], Is.EqualTo("42"));
            Assert.That(
                summary,
                Is.EqualTo(
                    new TagEnrichmentSummary(
                        MatchedNodes: 1,
                        EnrichedNodes: 1,
                        UnmatchedNodes: 0,
                        RemovedTags: 0,
                        SkippedExistingKeys: 0
                    )
                )
            );
        }
    }

    [Test]
    public void Load_DuplicatesDifferingOnlyInCoordinatesAndSource_CollapsesAndEnrichesTag()
    {
        var file = WriteRepository(
            new[]
            {
                new
                {
                    TAG_NO = "TEST-LINE-001",
                    TAG_CATEGORY = 6,
                    SYSTEM = "42",
                    X_EASTING = 1,
                    Y_NORTHING = 2,
                    Z_ELEVATION = 3,
                    DATA_SOURCE = "Imaginary source A",
                },
                new
                {
                    TAG_NO = "TEST-LINE-001",
                    TAG_CATEGORY = 6,
                    SYSTEM = "42",
                    X_EASTING = 9,
                    Y_NORTHING = 8,
                    Z_ELEVATION = 7,
                    DATA_SOURCE = "Imaginary source B",
                },
            }
        );
        var repository = TagMetadataRepository.Load(file);
        var node = CreateNode("TEST-LINE-001");

        var summary = TagMetadataInjector.ApplyMetadataToNodes([node], repository);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(repository.Count, Is.EqualTo(1));
            Assert.That(node.Attributes["Tag"], Is.EqualTo("TEST-LINE-001"));
            Assert.That(node.Attributes["TagMetadata.System"], Is.EqualTo("42"));
            Assert.That(summary, Is.EqualTo(new TagEnrichmentSummary(1, 1, 0, 0, 0)));
        }
    }

    [TestCase(7, "Imaginary line", "42", "A")]
    [TestCase(6, "Different imaginary line", "42", "A")]
    [TestCase(6, "Imaginary line", "99", "A")]
    [TestCase(6, "Imaginary line", "42", "H")]
    public void EnrichFromFiles_ConflictingDuplicate_RejectsRepositoryWithoutChangingNode(
        int categoryId,
        string description,
        string system,
        string status
    )
    {
        var file = WriteRepository(
            new[]
            {
                new TagMetadataEntry("TEST-LINE-001", 6, "Imaginary line", "42", "A"),
                new("TEST-LINE-001", categoryId, description, system, status),
            }
        );
        var node = CreateNode("TEST-LINE-001");
        var originalAttributes = new Dictionary<string, string>(node.Attributes);

        Assert.That(
            () => TagMetadataInjector.EnrichFromFiles([node], [file]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("Conflicting metadata")
        );
        Assert.That(node.Attributes, Is.EquivalentTo(originalAttributes));
    }

    private TagMetadataRepository LoadRepository(params TagMetadataEntry[] rows)
    {
        return TagMetadataRepository.Load(WriteRepository(rows));
    }

    private FileInfo WriteRepository(object rows)
    {
        var path = Path.Combine(_directory, "FICTIONAL_TAG_IN_3D_test.json");
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                new
                {
                    SchemaVersion = 1,
                    ReportName = "TAG_IN_3D",
                    Rows = rows,
                }
            )
        );
        return new FileInfo(path);
    }

    private static CadRevealNode CreateNode(string? tag)
    {
        var node = new CadRevealNode
        {
            Name = "Imaginary model part",
            TreeIndex = 1,
            Parent = null,
            Attributes = { ["Description"] = "Invented equipment description" },
        };
        if (tag != null)
            node.Attributes["Tag"] = tag;
        return node;
    }
}
