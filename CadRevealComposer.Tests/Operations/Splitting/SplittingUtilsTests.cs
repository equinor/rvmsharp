namespace CadRevealComposer.Tests.Operations.Splitting;

using CadRevealComposer.Operations.SectorSplitting;
using System.Numerics;

[TestFixture]
public class SplittingUtilsTests
{
    [Test]
    public void ReturnsCorrectlySplitNodes()
    {
        Node[] nodes =
        [
            new Node(4, null, 0, 0, new BoundingBox(Vector3.One, Vector3.Zero)),
            new Node(2, null, 0, 0, new BoundingBox(Vector3.Zero, Vector3.One)),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(99, 99, 99), new Vector3(100, 100, 100))),
            new Node(6, null, 0, 0, new BoundingBox(Vector3.One, Vector3.One))
        ];

        (Node[] regularNodes, Node[] outlierNodes) = nodes.SplitNodesIntoRegularAndOutlierNodes();

        // Assert
        Assert.AreEqual(3, regularNodes.Length);
        Assert.AreEqual(1, outlierNodes.Length);
    }

    [Test]
    public void Splitting_ReturnsOutlierSectors()
    {
        Node[] nodes =
        [
            new Node(4, null, 0, 0, new BoundingBox(Vector3.One, Vector3.Zero)),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(99, 99, 99), new Vector3(100, 100, 100))),
            new Node(6, null, 0, 0, new BoundingBox(Vector3.One, Vector3.One))
        ];

        var groups = SplittingUtils.GroupOutliersRecursive(nodes, 10f);

        Assert.AreEqual(2, groups.Count());
    }

    [Test]
    public void Splitting_ReturnsOutlierSectorsWhenSymmetrical()
    {
        Node[] nodes =
        [
            new Node(4, null, 0, 0, new BoundingBox(Vector3.Zero, Vector3.Zero)),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(90, 90, 90), new Vector3(100, 100, 100))),
            new Node(4, null, 0, 0, new BoundingBox(new Vector3(-90, -90, -90), new Vector3(-100, -100, -100)))
        ];

        var groups = SplittingUtils.GroupOutliersRecursive(nodes, 10f);

        Assert.AreEqual(3, groups.Count());
    }
}
