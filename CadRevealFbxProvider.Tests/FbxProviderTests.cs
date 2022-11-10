namespace CadRevealFbxProvider.Tests;

using CadRevealComposer;
using BatchUtils;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
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
        var treeIndexGenerator = new TreeIndexGenerator();
        using var test = new FbxImporter();
    }

    [Test]
    public void FbxImporterLoadFileTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        using var test = new FbxImporter();
        var RootNode = test.LoadFile(@"E:\tmp\AQ110South-3DView.FBX");
        Iterate(RootNode, test);
    }

    private void Iterate(FbxImporter.FbxNode root, FbxImporter test)
    {
        Console.WriteLine(test.GetNodeName(root));
        var childCount = test.GetChildCount(root);
        var t = test.GetTransform(root);
        Console.WriteLine($"Pos: {t.posX}, {t.posY}, {t.posZ}");
        test.GetGeometricData(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = test.GetChild(i, root);
            Iterate(child, test);
        }
    }

    [Test]
    public void SampleModel()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        using var test = new FbxImporter();
        var RootNode = test.LoadFile(@"E:\tmp\A6001-20A06.fbx");
        var lookupA = new Dictionary<IntPtr, (Mesh, int)>();
        List<APrimitive> geometriesToProcess = new List<APrimitive>();
        var nodesToProcess = FbxWorkload.IterateAndGenerate(RootNode, treeIndexGenerator, test, lookupA, geometriesToProcess).ToList();

        var outputDirectory = new DirectoryInfo(@"E:\tmp\lol");
        var modelParameters = new ModelParameters(new ProjectId(1), new ModelId(1), new RevisionId(1), new InstancingThreshold(1));
        var composerParameters = new ComposerParameters("", false, true, false);

        CadRevealComposerRunner.ProcessPrimitives(geometriesToProcess.ToArray(), outputDirectory, modelParameters, composerParameters, treeIndexGenerator);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
    }
}