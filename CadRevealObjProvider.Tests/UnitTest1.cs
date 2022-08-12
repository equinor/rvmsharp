namespace CadRevealObjProvider.Tests;

using CadRevealComposer.IdProviders;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [Explicit("Temp test.")]
    public void Test1()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        new ObjProvider2().ParseFiles(new [] { new FileInfo("TestData/HDA_subset.obj")}, treeIndexGenerator);

    }
}