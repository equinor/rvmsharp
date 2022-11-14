namespace CadRevealFbxProvider.Tests;

using BatchUtils;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Tessellation;

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
        var RootNode = test.LoadFile(@"E:\tmp\AQ110South-3DView.FBX");
        Iterate(RootNode, test);
    }

    private void Iterate(FbxImporter.FbxNode root, FbxImporter fbxImporter)
    {
        Console.WriteLine(fbxImporter.GetNodeName(root));
        var childCount = fbxImporter.GetChildCount(root);
        var t = fbxImporter.GetTransform(root);
        Console.WriteLine($"Pos: {t.posX}, {t.posY}, {t.posZ}");
        fbxImporter.GetGeometricData(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = fbxImporter.GetChild(i, root);
            Iterate(child, fbxImporter);
        }
    }

    [Test]
    public void SampleModel()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        using var test = new FbxImporter();
        var rootNode = test.LoadFile(@"E:\tmp\A6001-20A06.fbx");
        var lookupA = new Dictionary<IntPtr, (Mesh, int)>();
        var nodesToProcess = FbxWorkload.ConvertFbxNodesToCadRevealRecursive(rootNode, treeIndexGenerator, test, lookupA).ToList();

        var outputDirectory = new DirectoryInfo(@"E:\tmp\lol");
        var modelParameters = new ModelParameters(new ProjectId(1), new ModelId(1), new RevisionId(1), new InstancingThreshold(1));
        var composerParameters = new ComposerParameters("", false, true, false);

        var geometriesToProcess = nodesToProcess.SelectMany(x => x.Geometries);
        CadRevealComposerRunner.ProcessPrimitives(geometriesToProcess.ToArray(), outputDirectory, modelParameters, composerParameters, treeIndexGenerator);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
    }
}