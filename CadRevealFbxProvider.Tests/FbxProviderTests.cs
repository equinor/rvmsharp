namespace CadRevealFbxProvider.Tests;

using System.Numerics;
using System.Text.RegularExpressions;
using Attributes;
using BatchUtils;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;

[TestFixture]
public class FbxProviderTests
{
    private static readonly DirectoryInfo OutputDirectoryCorrect = new("TestSamples/correct");
    private static readonly DirectoryInfo InputDirectoryCorrect = new("TestSamples/correct");

    private static readonly DirectoryInfo InputDirectoryMissingAttr = new("TestSamples/missingattributes");

    private static readonly DirectoryInfo OutputDirectoryIncorrect = new("TestSamples/missingkey");
    private static readonly DirectoryInfo InputDirectoryIncorrect = new("TestSamples/missingkey");

    private static readonly DirectoryInfo OutputDirectoryMismatch = new("TestSamples/mismatch");
    private static readonly DirectoryInfo InputDirectoryMismatch = new("TestSamples/mismatch");

    private static readonly ModelParameters ModelParameters =
        new(
            new ProjectId(1),
            new ModelId(1),
            new RevisionId(1),
            new InstancingThreshold(1),
            new TemplateCountLimit(100)
        );
    private static readonly ComposerParameters ComposerParameters =
        new(
            false,
            true,
            false,
            new NodeNameExcludeRegex(null),
            new PrioritizedDisciplinesRegex(null),
            new LowPrioritizedDisciplineRegex(null),
            new PrioritizedNodeNamesRegex(null),
            0f,
            null
        );

    private static readonly List<IModelFormatProvider> Providers = [new FbxProvider()];

    [Test]
    public void FbxImporterSdkInitTest()
    {
        using var test = new FbxImporter();
    }

    [Test]
    public void FbxSdkVersionTest()
    {
        using var testImporter = new FbxImporter();
        Assert.That(testImporter.HasValidSdk());
    }

    [Test]
    public void FbxImporterLoadFileTest()
    {
        using var test = new FbxImporter();
        var rootNode = test.LoadFile(InputDirectoryCorrect + "\\fbx_test_model.fbx");
        Iterate(rootNode);
    }

    private void Iterate(FbxNode root)
    {
        Console.WriteLine(root.GetNodeName());
        var childCount = root.GetChildCount();
        Matrix4x4 t = root.GetLocalTransform();
        Console.WriteLine($"Pos: {t.Translation.X}, {t.Translation.Y}, {t.Translation.Z}");
        FbxMeshWrapper.GetGeometricData(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = root.GetChild(i);
            Iterate(child);
        }
    }

    [Test]
    public void Fbx_Importer_GetUniqueMeshesInFileCount()
    {
        using var test = new FbxImporter();
        var rootNode = test.LoadFile(InputDirectoryCorrect + "\\fbx_test_model.fbx");

        var data = FbxGeometryUtils.GetAllGeomPointersWithXOrMoreUses(rootNode);
        Assert.That(data, Has.Exactly(3).Items); // Expecting 3 unique meshes in the source model
    }

    [Test]
    public void ModelAndAttributeFileMismatchGivesErrorMessage()
    {
        Assert.Throws<Exception>(
            () => Process(InputDirectoryMismatch, OutputDirectoryMismatch),
            "An exception was expected, saying that the model and attribute file do not match, but got none."
        );
    }

    [Test]
    public void WrongAttributeFormatGivesErrorMessage()
    {
        Assert.Throws<Exception>(() => Process(InputDirectoryIncorrect, OutputDirectoryIncorrect));
    }

    [Test]
    public void SampleModel_SmokeTest()
    {
        Process(InputDirectoryCorrect, OutputDirectoryCorrect);
    }

    [Test]
    public void SampleModel_AttributeTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();

        (var nodes, var metadata) = modelFormatProviderFbx.ParseFiles(
            InputDirectoryCorrect.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null)),
            new PriorityMapping(
                new PrioritizedDisciplinesRegex(null),
                new LowPrioritizedDisciplineRegex(null),
                new PrioritizedNodeNamesRegex(null)
            )
        );

        Assert.That(nodes, Has.Count.EqualTo(28));
        Assert.That(nodes[0].Name, Is.EqualTo("RootNode"));
        Assert.That(nodes[1].Attributes, Has.Count.EqualTo(ScaffoldingAttributeParser.NumberOfAttributesPerPart));
        Assert.That(nodes[27].Attributes, Has.Count.EqualTo(ScaffoldingAttributeParser.NumberOfAttributesPerPart));
        Assert.That(nodes[2].Attributes.ContainsKey("Description"));
        Assert.That(nodes[2].Attributes["Description"], Is.EqualTo("Ladder"));
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata.Count(), Is.EqualTo(ScaffoldingMetadata.NumberOfModelAttributes));
    }

    [Test]
    public void SampleModel_Load()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();

        using var testLoader = new FbxImporter();
        var rootNode = testLoader.LoadFile(InputDirectoryCorrect + "\\fbx_test_model.fbx");

        var rootNodeConverted = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
            rootNode,
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null)),
            null
        );

        var flatNodes = CadRevealNode.GetAllNodesFlat(rootNodeConverted!).ToArray();
        // this test model should have a bounding box for each node
        foreach (var node in flatNodes)
        {
            Assert.That(node.BoundingBoxAxisAligned, Is.Not.Null);
            if (node.Name != "RootNode")
                Assert.That(node.Parent, Is.Not.Null); // All nodes except the root should have a parent
        }

        var geometriesToProcess = flatNodes.SelectMany(x => x.Geometries).ToArray();
        Assert.That(geometriesToProcess, Has.None.TypeOf<TriangleMesh>()); // All meshes in the input data are used more than once
        Assert.That(geometriesToProcess, Has.All.TypeOf<InstancedMesh>()); // Because the geometriesThatShouldBeInstanced list is empty
        CadRevealComposerRunner.ProcessPrimitives(
            geometriesToProcess.ToArray(),
            OutputDirectoryCorrect,
            ModelParameters,
            ComposerParameters
        );

        Console.WriteLine(
            $"Export Finished. Wrote output files to \"{Path.GetFullPath(OutputDirectoryCorrect.FullName)}\""
        );
    }

    [Test]
    public void SampleModel_Load_WithInstanceThresholdHigh()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        using var testLoader = new FbxImporter();
        var rootNode = testLoader.LoadFile(InputDirectoryCorrect + "\\fbx_test_model.fbx");
        var rootNodeConverted = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
            rootNode,
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null)),
            null,
            minInstanceCountThreshold: 5 // <-- We have a part which is only used twice, so this value should make those parts into 2 TriangleMeshes.,
        );

        var flatNodes = CadRevealNode.GetAllNodesFlat(rootNodeConverted!).ToArray();
        var geometriesToProcess = flatNodes.SelectMany(x => x.Geometries).ToArray();
        Assert.That(geometriesToProcess, Has.Exactly(2).TypeOf<TriangleMesh>());
        Assert.That(geometriesToProcess, Has.Exactly(25).TypeOf<InstancedMesh>());
    }

    [Test]
    public void NodeMissingAttributesTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();
        var priorityMapping = new PriorityMapping(
            new PrioritizedDisciplinesRegex(null),
            new LowPrioritizedDisciplineRegex(null),
            new PrioritizedNodeNamesRegex(null)
        );

        (IReadOnlyList<CadRevealNode> nodes, _) = modelFormatProviderFbx.ParseFiles(
            InputDirectoryMissingAttr.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null)),
            priorityMapping
        );

        // Ladders have no attributes, should thus be ignored
        Assert.That(nodes, Has.Count.EqualTo(26));
        foreach (var node in nodes)
        {
            Assert.That(node.Name, !Is.EqualTo("Ladder"));
        }
    }

    private static void Process(DirectoryInfo inputDirectory, DirectoryInfo outputDirectory) =>
        CadRevealComposerRunner.Process(
            inputDirectory,
            outputDirectory,
            ModelParameters,
            ComposerParameters,
            Providers
        );
}
