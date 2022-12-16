namespace CadRevealFbxProvider.Tests;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Tessellation;
using System.Numerics;


[TestFixture]
public class FbxProviderTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void FbxImporterSdkInitTest()
    {
        using var test = new FbxImporter();
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
    public void SampleModel()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIndexGenerator = new InstanceIdGenerator();

        using var testLoader = new FbxImporter();
        var rootNode = testLoader.LoadFile(@".\TestSamples\fbx_test_model.fbx");
        var lookupA = new Dictionary<IntPtr, (Mesh, ulong)>();
        var nodesToProcess = FbxNodeToCadRevealNodeConverter.ConvertRecursive(
            rootNode,
            treeIndexGenerator,
            instanceIndexGenerator,
            testLoader,
            lookupA).ToList();

        var outputDirectory = new DirectoryInfo(@".\TestSamples");
        var modelParameters = new ModelParameters(new ProjectId(1), new ModelId(1), new RevisionId(1), new InstancingThreshold(1));
        var composerParameters = new ComposerParameters("", false, true, false);

        var geometriesToProcess = nodesToProcess.SelectMany(x => x.Geometries);
        CadRevealComposerRunner.ProcessPrimitives(geometriesToProcess.ToArray(), outputDirectory, modelParameters, composerParameters, treeIndexGenerator);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
    }
}