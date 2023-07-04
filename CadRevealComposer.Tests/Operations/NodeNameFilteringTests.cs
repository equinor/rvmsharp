namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations;
using Configuration;

public class NodeNameFilteringTests
{
    readonly NodeNameFiltering _nnfWithExampleRegex = new NodeNameFiltering(
        new NodeNameExcludeRegex("mech\\-tempsteel")
    );

    [Test]
    [TestCase("mech-tempsteel", ExpectedResult = true)]
    [TestCase("something_els", ExpectedResult = false)]
    [TestCase("contains_mech-tempsteel_1", ExpectedResult = true)] // A Regex is by default "contains" unless it starts with ^ or ends with dollar
    [TestCase("mEcH-tempsteel_case_insensitive", ExpectedResult = true)]
    [TestCase("", ExpectedResult = false)]
    [TestCase("My regex", ExpectedResult = false)]
    public bool ShouldExcludeNode_TestCases(string nodeName) => _nnfWithExampleRegex.ShouldExcludeNode(nodeName);

    [Test]
    public void NodeNameFiltering_WhenNullFilter_FiltersNothing()
    {
        var nnf = new NodeNameFiltering(new NodeNameExcludeRegex(null));
        Assert.That(nnf.ShouldExcludeNode("anything"), Is.False);
        Assert.That(nnf.ShouldExcludeNode(""), Is.False);
    }
}
