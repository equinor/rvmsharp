namespace CadRevealObjProvider.Tests;

using CadRevealComposer;
using CadRevealComposer.Configuration;
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
        var instanceIdGenerator = new InstanceIdGenerator();
        var treeIndexGenerator = new TreeIndexGenerator();
        new ObjProvider().ParseFiles(
            new[] { new FileInfo("TestData/HDA_subset.obj") },
            treeIndexGenerator,
            instanceIdGenerator,
            new NodeNameFiltering(new NodeNameExcludeGlobs(Array.Empty<string>()))
        );
    }
}
