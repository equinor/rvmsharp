namespace CadRevealObjProvider.Tests;

using CadRevealComposer.IdProviders;

[TestFixture]
public class ObjProviderTests
{
    [SetUp]
    public void Setup() { }

    [Test]
    [Explicit("Temp test, requires non-checked in obj file")]
    public void ObjProviderTests_SmokeTest()
    {
        var treeIndexGenerator = new TreeIndexGenerator();
        var instanceIdGenerator = new InstanceIdGenerator();
        new ObjProvider().ParseFiles(
            new[] { new FileInfo("TestData/HDA_subset.obj") },
            treeIndexGenerator,
            instanceIdGenerator
        );
    }
}
