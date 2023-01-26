namespace CadRevealFbxProvider.Tests;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Tessellation;
using System.Numerics;

[TestFixture]
public class FbxProviderTests
{
    private DirectoryInfo outputDirectory = new DirectoryInfo(@".\TestSamples");
    private DirectoryInfo inputDirectory = new DirectoryInfo(@".\TestSamples");

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
        var RootNode = test.LoadFile(@".\TestSamples\fbx_test_model.fbx");
        Iterate(RootNode, test);
    }

    private void Iterate(FbxNode root, FbxImporter fbxImporter)
    {
        Console.WriteLine(FbxNodeWrapper.GetNodeName(root));
        var childCount = FbxNodeWrapper.GetChildCount(root);
        Matrix4x4 t = FbxNodeWrapper.GetTransform(root);
        Console.WriteLine($"Pos: {t.Translation.X}, {t.Translation.Y}, {t.Translation.Z}");
        FbxMeshWrapper.GetGeometricData(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = FbxNodeWrapper.GetChild(i, root);
            Iterate(child, fbxImporter);
        }
    }

    [Test]
    public void SampleModel_SmokeTest()
    {
        try
        {
            var providers = new List<IModelFormatProvider>() { new FbxProvider() };

            var modelParameters = new ModelParameters(
                new ProjectId(1),
                new ModelId(1),
                new RevisionId(1),
                new InstancingThreshold(1),
                new TemplateCountLimit(100)
            );
            var composerParameters = new ComposerParameters("", false, true, false);

            CadRevealComposerRunner.Process(
                inputDirectory,
                outputDirectory,
                modelParameters,
                composerParameters,
                providers
            );
        }
        catch (Exception ex)
        {
            Assert.Fail("Expected no exception, but got: " + ex.Message);
        }
    }

    [Test]
    public void SampleModel_AttributeTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();
        var modelFormatProviderFbx = new FbxProvider();

        var nodes = modelFormatProviderFbx.ParseFiles(
            inputDirectory.EnumerateFiles(),
            treeIndexGenerator,
            instanceIndexGenerator
        );

        Assert.That(nodes.Count() == 28);
        Assert.That(nodes[0].Name, Is.EqualTo("RootNode"));
        Assert.That(nodes[1].Attributes.Count(), Is.EqualTo(12));
        Assert.That(nodes[27].Attributes.Count(), Is.EqualTo(12));
        Assert.That(nodes[2].Attributes.ContainsKey("Description"));
        Assert.That(nodes[2].Attributes["Description"].Equals("Ladder"));
    }

    [Test]
    public void SampleModel_Load()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();

        using var testLoader = new FbxImporter();
        var rootNode = testLoader.LoadFile(@".\TestSamples\fbx_test_model.fbx");
        var lookupA = new Dictionary<IntPtr, (Mesh, ulong)>();

        var rootNodeConverted = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
            rootNode,
            treeIndexGenerator,
            instanceIndexGenerator,
            testLoader,
            lookupA);

        var flatNodes = CadRevealNode.GetAllNodesFlat(rootNodeConverted).ToArray();
        // this test model should have a bounding box for each node
        foreach ( var node in flatNodes) {
            if(node.Geometries.Length>0) Assert.That(node.BoundingBoxAxisAligned != null);
        }
        
        var modelParameters = new ModelParameters(
            new ProjectId(1),
            new ModelId(1),
            new RevisionId(1),
            new InstancingThreshold(1),
            new TemplateCountLimit(100)
        );
        var composerParameters = new ComposerParameters("", false, true, false);

        var geometriesToProcess = flatNodes.SelectMany(x => x.Geometries);

        CadRevealComposerRunner.ProcessPrimitives(
            geometriesToProcess.ToArray(),
            outputDirectory,
            modelParameters,
            composerParameters,
            treeIndexGenerator
        );
       
        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
    }
}
