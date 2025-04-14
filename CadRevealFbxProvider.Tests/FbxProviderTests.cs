namespace CadRevealFbxProvider.Tests;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealFbxProvider.Attributes;
using CadRevealFbxProvider.BatchUtils;

[TestFixture]
public class FbxProviderTests
{
    private static readonly DirectoryInfo TestSamplesDirectory = new("TestSamples");
    private static readonly DirectoryInfo OutputDirectoryCorrect = new("TestSamples/correct");
    private static readonly DirectoryInfo InputDirectoryCorrect = new("TestSamples/correct");

    private static readonly ModelParameters ModelParameters =
        new(
            new ProjectId(1),
            new ModelId(1),
            new RevisionId(1),
            new InstancingThreshold(1),
            new TemplateCountLimit(100)
        );
    private static readonly ComposerParameters ComposerParameters =
        new(false, true, false, new NodeNameExcludeRegex(null), 0f, null);

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
        var rootNode = test.LoadFile(InputDirectoryCorrect + "/fbx_test_model.fbx");
        Iterate(rootNode);
    }

    [Test]
    public void GreenAndRedCube_LoadFile_VerifyCorrectColors()
    {
        using var test = new FbxImporter();
        var rootNode = test.LoadFile(TestSamplesDirectory + "/green_and_red_cubes.fbx");

        var revealNode = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
            rootNode,
            new TreeIndexGenerator(),
            new InstanceIdGenerator(),
            new NodeNameFiltering(new NodeNameExcludeRegex(null)),
            null
        );

        Assert.That(revealNode, Is.Not.Null);

        var children = revealNode!.Children;
        Assert.That(children, Is.Not.Null);
        Assert.That(children, Has.Length.EqualTo(2));

        AssertHasOnePrimitiveWithColor(children[0], Color.FromArgb(255, 204, 0, 1));
        AssertHasOnePrimitiveWithColor(children[1], Color.FromArgb(255, 2, 204, 0));
        return;

        void AssertHasOnePrimitiveWithColor(CadRevealNode node, Color expectedColor)
        {
            var primitives = node.Geometries;
            Assert.That(primitives, Has.Length.EqualTo(1));
            var color = primitives[0].Color;
            Assert.That(color, Is.EqualTo(expectedColor));
        }
    }

    private void Iterate(FbxNode root)
    {
        Console.WriteLine(root.GetNodeName());
        var childCount = root.GetChildCount();
        Matrix4x4 t = root.GetLocalTransform();
        Console.WriteLine($"Pos: {t.Translation.X}, {t.Translation.Y}, {t.Translation.Z}");
        IntPtr meshGeometryPtr = FbxMeshWrapper.GetMeshGeometryPtr(root);
        if (meshGeometryPtr != IntPtr.Zero)
        {
            FbxMeshWrapper.GetGeometricData(meshGeometryPtr);
        }
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
        var rootNode = test.LoadFile(InputDirectoryCorrect + "/fbx_test_model.fbx");

        var data = FbxGeometryUtils.GetAllGeomPointersWithXOrMoreUses(rootNode);
        Assert.That(data, Has.Exactly(3).Items); // Expecting 3 unique meshes in the source model
    }

    [TestCase("TestSamples/mismatch")]
    public void Process_ModelAndAttributeFileMismatch_ThrowsError(string strDir)
    {
        DirectoryInfo outputDirectoryMismatch = new(strDir);
        DirectoryInfo inputDirectoryMismatch = new(strDir);

        Assert.Throws<Exception>(
            () => Process(inputDirectoryMismatch, outputDirectoryMismatch),
            "An exception was expected, saying that the model and attribute file do not match, but got none."
        );
    }

    [Test]
    public void Process_WrongAttributeFormat_ThrowsError()
    {
        DirectoryInfo OutputDirectoryIncorrect = new("TestSamples/missingKey");
        DirectoryInfo InputDirectoryIncorrect = new("TestSamples/missingKey");
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
            new NodeNameFiltering(new NodeNameExcludeRegex(null))
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
        var rootNode = testLoader.LoadFile(InputDirectoryCorrect + "/fbx_test_model.fbx");

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
        CadRevealComposerRunner.SplitAndExportSectors(
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
        var rootNode = testLoader.LoadFile(InputDirectoryCorrect + "/fbx_test_model.fbx");
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

    [TestCase("TestSamples/missingAttributes")]
    public void ParseFiles_ModelWithNodeMissingAttributes_NodeGetsRemoved(string inputDir)
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();

        DirectoryInfo inputDirectoryMissingAttr = new(inputDir);

        (IReadOnlyList<CadRevealNode> nodes, _) = modelFormatProviderFbx.ParseFiles(
            inputDirectoryMissingAttr.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null))
        );

        // Ladders have no attributes, should thus be ignored
        Assert.That(nodes, Has.Count.EqualTo(26));
        foreach (var node in nodes)
        {
            Assert.That(node.Name, !Is.EqualTo("Ladder"));
        }
    }

    [TestCase("TestSamples/tempScaff")]
    public void ParseFiles_TempScaffolding_ProcessingSucceedsMetadataHasPositiveTempFlag(string inputDir)
    {
        // arrange
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();
        DirectoryInfo inputDirectoryTempScaff = new(inputDir);

        // act
        (var rootNode, var metadata) = modelFormatProviderFbx.ParseFiles(
            inputDirectoryTempScaff.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null))
        );

        // assert
        Assert.That(metadata!.checkValue("Scaffolding_IsTemporary", "True"), Is.True);
    }

    [TestCase("TestSamples/correct")]
    public void ParseFiles_WorkorderScaffolding_ProcessingSucceedsMetadataHasNegativeTempFlag(string inputDir)
    {
        // arrange
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();
        DirectoryInfo inputDirectoryTempScaff = new(inputDir);

        // act
        (var rootNode, var metadata) = modelFormatProviderFbx.ParseFiles(
            inputDirectoryTempScaff.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null))
        );

        // assert
        Assert.That(metadata!.checkValue("Scaffolding_IsTemporary", "False"), Is.True);
    }

    [TestCase("TestSamples/tempScaff_wrongNaming")]
    public void ParseFiles_TempScaffoldingWithWrongName_ProcessingFails(string inputDir)
    {
        // arrange
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();
        DirectoryInfo inputDirectoryTempScaff = new(inputDir);

        // act
        // no act, assert that exception is thrown

        // assert
        Assert.Throws<Exception>(
            () =>
                modelFormatProviderFbx.ParseFiles(
                    inputDirectoryTempScaff.EnumerateFiles(),
                    treeIndexGenerator,
                    instanceIndexGenerator,
                    new NodeNameFiltering(new NodeNameExcludeRegex(null))
                )
        ); // this scaff is not a valid temp scaffolding
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
