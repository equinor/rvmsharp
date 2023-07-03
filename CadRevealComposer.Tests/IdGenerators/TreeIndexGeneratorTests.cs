namespace CadRevealComposer.Tests.IdGenerators;

using IdProviders;

public class TreeIndexGeneratorTests
{
    [Test]
    public void TreeIndex_Generator_WhenNew_GivesFirstIndexAs1()
    {
        // The Hierarchy Database uses zero 0 as a special number, so we keep this scheme here as well. Starting always from 1.
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
