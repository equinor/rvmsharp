namespace CadRevealFbxProvider.Tests;

using CadRevealComposer.IdProviders;

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
        var RootNode = test.LoadFile(@"E:\gush\projects\experimental\study\rvmsharp-scaffolding\fbximport\build\Debug\AQ110South-3DView.FBX");
        Iterate(RootNode, test);
    }

    private void Iterate(FbxImporter.FbxNode root, FbxImporter test)
    {
        Console.WriteLine(test.GetNodeName(root));
        var childCount = test.GetChildCount(root);
        var t = test.GetTransform(root);
        Console.WriteLine($"Pos: {t.posX}, {t.posY}, {t.posZ}");
        for (var i = 0; i < childCount; i++)
        {
            var child = test.GetChild(i, root);
            Iterate(child, test);
        }
    }
}