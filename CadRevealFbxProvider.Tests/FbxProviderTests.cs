namespace CadRevealFbxProvider.Tests;

using BatchUtils;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using System.Numerics;

[TestFixture]
public class FbxProviderTests
{
    private static readonly DirectoryInfo outputDirectoryCorrect = new DirectoryInfo(@".\TestSamples\correct");
    private static readonly DirectoryInfo inputDirectoryCorrect = new DirectoryInfo(@".\TestSamples\correct");

    private static readonly DirectoryInfo outputDirectoryMissingAttr = new DirectoryInfo(
        @".\TestSamples\missingattributes"
    );
    private static readonly DirectoryInfo inputDirectoryMissingAttr = new DirectoryInfo(
        @".\TestSamples\missingattributes"
    );

    private static readonly DirectoryInfo outputDirectoryIncorrect = new DirectoryInfo(@".\TestSamples\missingkey");
    private static readonly DirectoryInfo inputDirectoryIncorrect = new DirectoryInfo(@".\TestSamples\missingkey");

    private static readonly DirectoryInfo outputDirectoryMismatch = new DirectoryInfo(@".\TestSamples\mismatch");
    private static readonly DirectoryInfo inputDirectoryMismatch = new DirectoryInfo(@".\TestSamples\mismatch");

    private static readonly ModelParameters modelParameters = new ModelParameters(
        new ProjectId(1),
        new ModelId(1),
        new RevisionId(1),
        new InstancingThreshold(1),
        new TemplateCountLimit(100)
    );
    private static readonly ComposerParameters composerParameters = new ComposerParameters(
        false,
        true,
        false,
        new NodeNameExcludeRegex(null)
    );

    private static readonly List<IModelFormatProvider> providers = new List<IModelFormatProvider>()
    {
        new FbxProvider()
    };

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
        var RootNode = test.LoadFile(inputDirectoryCorrect + "\\fbx_test_model.fbx");
        Iterate(RootNode);
    }

    private void Iterate(FbxNode root)
    {
        Console.WriteLine(FbxNodeWrapper.GetNodeName(root));
        var childCount = FbxNodeWrapper.GetChildCount(root);
        Matrix4x4 t = FbxNodeWrapper.GetTransform(root);
        Console.WriteLine($"Pos: {t.Translation.X}, {t.Translation.Y}, {t.Translation.Z}");
        FbxMeshWrapper.GetGeometricData(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = FbxNodeWrapper.GetChild(i, root);
            Iterate(child);
        }
    }

    [Test]
    public void Fbx_Importer_GetUniqueMeshesInFileCount()
    {
        using var test = new FbxImporter();
        var RootNode = test.LoadFile(inputDirectoryCorrect + "\\fbx_test_model.fbx");

        var data = FbxGeometryUtils.GetAllGeomPointersWithXOrMoreUses(RootNode);
        Assert.That(data, Has.Exactly(3).Items); // Expecting 3 unique meshes in the source model
    }

    [Test]
    public void ModelAndAttributeFileMismatchGivesErrorMessage()
    {
        Assert.Throws<Exception>(
            () =>
            {
                CadRevealComposerRunner.Process(
                    inputDirectoryMismatch,
                    outputDirectoryMismatch,
                    modelParameters,
                    composerParameters,
                    providers
                );
            },
            "An exception was expected, saying that the model and attribute file do not match, but got none."
        );
    }

    [Test]
    public void WrongAttributeFormatGivesErrorMessage()
    {
        Assert.Throws<Exception>(() =>
        {
            CadRevealComposerRunner.Process(
                inputDirectoryIncorrect,
                outputDirectoryIncorrect,
                modelParameters,
                composerParameters,
                providers
            );
        });
    }

    [Test]
    public void SampleModel_SmokeTest()
    {
        CadRevealComposerRunner.Process(
            inputDirectoryCorrect,
            outputDirectoryCorrect,
            modelParameters,
            composerParameters,
            providers
        );
    }

    [Test]
    public void SampleModel_AttributeTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();

        var nodes = modelFormatProviderFbx.ParseFiles(
            inputDirectoryCorrect.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null))
        );

        Assert.That(nodes, Has.Count.EqualTo(28));
        Assert.That(nodes[0].Name, Is.EqualTo("RootNode"));
        Assert.That(nodes[1].Attributes, Has.Count.EqualTo(23));
        Assert.That(nodes[27].Attributes, Has.Count.EqualTo(23));
        Assert.That(nodes[2].Attributes.ContainsKey("Description"));
        Assert.That(nodes[2].Attributes["Description"], Is.EqualTo("Ladder"));
    }

    [Test]
    public void SampleModel_Load()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();

        using var testLoader = new FbxImporter();
        var rootNode = testLoader.LoadFile(inputDirectoryCorrect + "\\fbx_test_model.fbx");

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
            outputDirectoryCorrect,
            modelParameters,
            composerParameters,
            treeIndexGenerator
        );

        Console.WriteLine(
            $"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectoryCorrect.FullName)}\""
        );
    }

    [Test]
    public void SampleModel_Load_WithInstanceThresholdHigh()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        using var testLoader = new FbxImporter();
        var rootNode = testLoader.LoadFile(inputDirectoryCorrect + "\\fbx_test_model.fbx");
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

        var nodes = modelFormatProviderFbx.ParseFiles(
            inputDirectoryMissingAttr.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator,
            new NodeNameFiltering(new NodeNameExcludeRegex(null))
        );

        // there are 28 attributes, out of which 2 should be ignored
        Assert.That(nodes, Has.Count.EqualTo(26));

        // Ladders have no attributes, should thus be ignored
        for (int i = 0; i < 26; i++)
        {
            Assert.That(nodes[i].Name, !Is.EqualTo("Ladder"));
        }
    }
}
