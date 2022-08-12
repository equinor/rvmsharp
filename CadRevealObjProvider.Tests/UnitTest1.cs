namespace CadRevealObjProvider.Tests;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        new ObjProvider().ParseFiles(new [] { "TestData/HDA_subset.obj"});

    }
}