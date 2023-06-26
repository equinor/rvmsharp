namespace CadRevealComposer.Tests.Operations;

using CadRevealComposer.Operations;
using IdProviders;
using Primitives;
using System.Drawing;
using System.Numerics;

public class NodeFilteringTests
{
    [Test]
    public void FilterNodes_WhenRemovingANode_RemovesAllChildrenOfThatNodeToo()
    {
        CadRevealNode[] nodes = GenerateTestRevealNodeData();
        var treeIndexGenerator = new TreeIndexGenerator();
        var filteredNodes = NodeFiltering.FilterAndReindexNodesByGlobs(nodes, new[] { "cde" }, treeIndexGenerator);

        Assert.That(filteredNodes, Has.Exactly(2).Items);
        Assert.That(filteredNodes[0].TreeIndex, Is.EqualTo(0));
        Assert.That(filteredNodes[0].Name, Is.EqualTo("abc"));
        Assert.That(filteredNodes.Last().TreeIndex, Is.EqualTo(1));
        Assert.That(filteredNodes.Last().Name, Is.EqualTo("root2"));
    }

    [Test]
    public void FilterAndReindex_WhenFilteringChildNode_ReindexesWithParentBeforeChild_And_RemovesAllChildrenOfNode()
    {
        CadRevealNode[] nodes = GenerateTestRevealNodeData();
        var treeIndexGenerator = new TreeIndexGenerator();
        var filteredNodes = NodeFiltering.FilterAndReindexNodesByGlobs(nodes, new[] { "fgh" }, treeIndexGenerator);

        Assert.That(filteredNodes, Has.Exactly(3).Items);
        Assert.That(filteredNodes[0].TreeIndex, Is.EqualTo(0));
        Assert.That(filteredNodes[0].Name, Is.EqualTo("abc"));
        Assert.That(filteredNodes[1].Name, Is.EqualTo("cde"));
        Assert.That(filteredNodes[1].TreeIndex, Is.EqualTo(1));
        Assert.That(filteredNodes[1].Children, Is.Empty);
        Assert.That(filteredNodes[1].Geometries[0].TreeIndex, Is.EqualTo(1)); // Keep geometries, and reindex them too!
        Assert.That(filteredNodes[1].Parent, Is.EqualTo(filteredNodes[0]));

        Assert.That(filteredNodes[2].TreeIndex, Is.EqualTo(2));
        Assert.That(filteredNodes[2].Name, Is.EqualTo("root2"));
    }

    private static CadRevealNode[] GenerateTestRevealNodeData()
    {
        var root1 = new CadRevealNode()
        {
            TreeIndex = 1,
            Name = "abc",
            Parent = null
        };
        var data2 = new CadRevealNode()
        {
            TreeIndex = 2,
            Name = "cde",
            Parent = root1,
            Geometries = new[]
            {
                new Box(Matrix4x4.Identity, 2, Color.Aqua, new BoundingBox(-Vector3.One, Vector3.One))
            }
        };
        var data3 = new CadRevealNode()
        {
            TreeIndex = 3,
            Name = "fgh",
            Parent = data2,
            Geometries = new[]
            {
                new Box(Matrix4x4.Identity, 3, Color.Aqua, new BoundingBox(-Vector3.One, Vector3.One))
            }
        };
        data2.Children = new[] { data3 };
        root1.Children = new[] { data2 };
        var root2 = new CadRevealNode()
        {
            TreeIndex = 4,
            Name = "root2",
            Parent = null
        };
        var nodes = new[] { root1, data2, root2 };
        return nodes;
    }
}
