namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations;
using Configuration;

public class NodeNameFilteringTests
{
    readonly NodeNameFiltering _nnfWithExampleRegex = new NodeNameFiltering(new NodeNameExcludeRegex("my_regex"));

    [Test]
    [TestCase("my_regex", ExpectedResult = true)]
    [TestCase("something_els", ExpectedResult = false)]
    [TestCase("contains_my_regex_1", ExpectedResult = true)] // A Regex is by default "contains" unless it starts with ^ or ends with dollar
    [TestCase("my_rEgEx_case_insensitive", ExpectedResult = true)]
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
