namespace CadRevealComposer.Tests.Operations.Splitting;

using CadRevealComposer.Operations.SectorSplitting;
using NUnit.Framework.Legacy;
using System.Numerics;

[TestFixture]
public class SplittingUtilsTests
{
    [Test]
    public void ReturnsCorrectlySplitNodes()
    {
        Node[] nodes = new Node[]
        {
            new Node(4, null, 0, 0, new BoundingBox(Vector3.One, Vector3.Zero)),
            new Node(2, null, 0, 0, new BoundingBox(Vector3.Zero, Vector3.One)),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(99, 99, 99), new Vector3(100, 100, 100))),
            new Node(6, null, 0, 0, new BoundingBox(Vector3.One, Vector3.One))
        };

        (Node[] regularNodes, Node[] outlierNodes) = nodes.SplitNodesIntoRegularAndOutlierNodes();

        // Assert
        Assert.That(regularNodes.Length, Is.EqualTo(3));
        Assert.That(outlierNodes.Length, Is.EqualTo(1));
    }

    [Test]
    public void Splitting_ReturnsOutlierSectors()
    {
        Node[] nodes = new Node[]
        {
            new Node(4, null, 0, 0, new BoundingBox(Vector3.One, Vector3.Zero)),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(99, 99, 99), new Vector3(100, 100, 100))),
            new Node(6, null, 0, 0, new BoundingBox(Vector3.One, Vector3.One))
        };

        var groups = SplittingUtils.GroupOutliersRecursive(nodes, 10f);

        Assert.That(groups.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Splitting_ReturnsOutlierSectorsWhenSymmetrical()
    {
        Node[] nodes = new Node[]
        {
            new Node(4, null, 0, 0, new BoundingBox(Vector3.Zero, Vector3.Zero)),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(90, 90, 90), new Vector3(100, 100, 100))),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(-90, -90, -90), new Vector3(-100, -100, -100))),
        };

        var groups = SplittingUtils.GroupOutliersRecursive(nodes, 10f);

        Assert.That(groups.Count(), Is.EqualTo(3));
    }
}
