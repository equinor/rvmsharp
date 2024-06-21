namespace HierarchyComposer.Tests;

using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Functions;
using Model;
using NUnit.Framework;

public class DatabaseComposerTests
{
    private static readonly string TestFilePath = Path.Combine(".", "temp_hierarchy_integration_test.db")!;

    [SetUp]
    public void Setup()
    {
        CleanDatabaseFile();
    }

    [TearDown]
    public void TearDown()
    {
        CleanDatabaseFile();
    }

    private static void CleanDatabaseFile()
    {
        if (File.Exists(TestFilePath))
            File.Delete(TestFilePath);
    }

    [Test]
    public void ComposeDatabase_DoesSerializeFullNodeToDatabase_WithoutCrashing()
    {
        // This test does not at the time of writing actually check that the output is correct.
        // But it will at least check if something is really wrong.
        var databaseComposer = new DatabaseComposer();

        var nodes = new List<HierarchyNode>()
        {
            new HierarchyNode()
            {
                NodeId = 1,
                Name = "23L0001",
                HasMesh = true,
                ParentId = null,
                OptionalDiagnosticInfo = @"{ ""someProp"" : ""someVal"" }",
                RefNoDb = 2,
                RefNoSequence = 3,
                TopNodeId = 1,
                PDMSData = new Dictionary<string, string>() { { "Tag", "23L0001" } },
                AABB = new AABB
                {
                    min = new Vector3EfSerializable(-Vector3.One),
                    max = new Vector3EfSerializable(Vector3.One)
                }
            }
        };

        databaseComposer.ComposeDatabase(nodes, TestFilePath);

        Assert.That(TestFilePath, Does.Exist.IgnoreDirectories);

        var databaseFileInfo = new FileInfo(TestFilePath);
        Assert.That(databaseFileInfo, Has.Length.GreaterThan(0)); // Simple check that anything happened to the database.
    }
}
