namespace CadRevealComposer.Tests.IdGenerators;

using IdProviders;

public class TreeIndexGeneratorTests
{
    [Test]
    public void TreeIndex_Generator_WhenNew_GivesFirstIndexAs1()
    {
        var tig = new TreeIndexGenerator();
        var firstIndex = tig.GetNextId();
        Assert.That(firstIndex, Is.EqualTo(1));
    }

    [Test]
    public void TreeIndex_Generator_IteratesToNextIdEveryTimeANewIdIsRequested()
    {
        var tig = new TreeIndexGenerator();
        var firstIndex = tig.GetNextId();
        Assert.That(firstIndex, Is.EqualTo(1));
        var secondIndex = tig.GetNextId();
        Assert.That(secondIndex, Is.EqualTo(2));
    }
}
