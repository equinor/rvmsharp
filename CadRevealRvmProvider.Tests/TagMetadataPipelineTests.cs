namespace CadRevealRvmProvider.Tests;

using System.Buffers.Binary;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using Microsoft.Data.Sqlite;
using RvmSharp.Primitives;

[TestFixture]
public class TagMetadataPipelineTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Directory.CreateTempSubdirectory("tag_pipeline_tests_").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void ParseFiles_MatchingTag_EnrichesNodeAndPersistsMetadata()
    {
        WriteModel();
        WriteFile("FICTIONAL_TAG_IN_3D_test.json",
            """
            {
              "SchemaVersion": 1,
              "ReportName": "TAG_IN_3D",
              "Rows": [
                {
                  "TAG_NO": "TEST-LINE-001",
                  "TAG_CATEGORY": 6,
                  "TAG_CATEGORY_DESCRIPTION": "Imaginary line",
                  "SYSTEM": "42"
                }
              ]
            }
            """
        );
        var expectedStoredAttributes = new Dictionary<string, string>
        {
            ["Tag"] = "TEST-LINE-001",
            ["Description"] = "Invented equipment description",
            ["TagMetadata.CategoryId"] = "6",
            ["TagMetadata.CategoryDescription"] = "Imaginary line",
            ["TagMetadata.System"] = "42",
        };
        var expectedNodeAttributes = new Dictionary<string, string>(expectedStoredAttributes)
        {
            ["RefNo"] = "=123/321",
        };
        var databasePath = Path.Combine(_directory, "hierarchy.db");

        var nodes = ParseModel();
        var node = nodes.Single();
        SceneCreator.ExportHierarchyDatabase(databasePath, nodes);
        var storedAttributes = ReadAttributes(databasePath, node.TreeIndex);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Attributes, Is.EquivalentTo(expectedNodeAttributes));
            Assert.That(node.Geometries, Has.Length.EqualTo(1));
            Assert.That(storedAttributes, Is.EquivalentTo(expectedStoredAttributes));
        }
    }

    [Test]
    public void ParseFiles_NoTagRepository_PreservesTxtAttributes()
    {
        WriteModel();
        var expectedAttributes = new Dictionary<string, string>
        {
            ["RefNo"] = "=123/321",
            ["Tag"] = "TEST-LINE-001",
            ["Description"] = "Invented equipment description",
        };

        var node = ParseModel().Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(node.Attributes, Is.EquivalentTo(expectedAttributes));
            Assert.That(node.Geometries, Has.Length.EqualTo(1));
        }
    }

    private IReadOnlyList<CadRevealNode> ParseModel()
    {
        return new RvmProvider().ParseFiles(
            new DirectoryInfo(_directory).EnumerateFiles(),
            new TreeIndexGenerator(),
            new InstanceIdGenerator(),
            new NodeNameFiltering(new NodeNameExcludeRegex(null))
        ).Item1;
    }

    private void WriteFile(string filename, string content)
    {
        File.WriteAllText(Path.Combine(_directory, filename), content);
    }

    private static Dictionary<string, string> ReadAttributes(string databasePath, uint nodeId)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pdms.Key, pdms.Value
            FROM PDMSEntries pdms
            JOIN NodeToPdmsEntry mapping ON mapping.PDMSEntryId = pdms.Id
            WHERE mapping.NodeId = $nodeId;
            """;
        command.Parameters.AddWithValue("$nodeId", nodeId);
        using var reader = command.ExecuteReader();
        var attributes = new Dictionary<string, string>();
        while (reader.Read())
            attributes.Add(reader.GetString(0), reader.GetString(1));
        return attributes;
    }

    private void WriteModel()
    {
        using var stream = File.Create(Path.Combine(_directory, "model.rvm"));
        void WriteUint(uint value)
        {
            Span<byte> bytes = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
            stream.Write(bytes);
        }
        void WriteFloat(float value) => WriteUint(BitConverter.SingleToUInt32Bits(value));
        void WriteChunk(string name)
        {
            foreach (var character in name)
                WriteUint(character);
            WriteUint(0);
            WriteUint(0);
        }

        WriteChunk("HEAD");
        WriteUint(1);
        for (var field = 0; field < 4; field++)
            WriteUint(0);
        WriteChunk("MODL");
        WriteUint(1);
        WriteUint(0);
        WriteUint(0);
        WriteChunk("CNTB");
        WriteUint(1);
        WriteUint(1);
        stream.Write("ROOT"u8);
        for (var field = 0; field < 4; field++)
            WriteUint(0);
        WriteChunk("PRIM");
        WriteUint(1);
        WriteUint((uint)RvmPrimitiveKind.Box);
        foreach (var component in new float[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 })
            WriteFloat(component);
        foreach (var component in new float[] { -1, -1, -1, 1, 1, 1, 2, 2, 2 })
            WriteFloat(component);
        WriteChunk("CNTE");
        WriteUint(1);
        WriteChunk("END:");

        WriteFile("model.txt",
            """
            CADC_Attributes_File v1.0 , start: NEW , end: END , name_end: := , sep: &end&
            NEW Header Information
              Source:= Synthetic test data
            END
            NEW ROOT
              RefNo:= '=123/321'
              Tag:= 'TEST-LINE-001'
              Description:= 'Invented equipment description'
            END
            """
        );
    }
}
