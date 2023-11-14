namespace CadRevealComposer.Tests.Operations.Splitting;

using CadRevealComposer.Operations.SectorSplitting;
using System.Numerics;

[TestFixture]
public class SplitNodesIntoRegularAndOutlierNodesTests
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
        Assert.AreEqual(3, regularNodes.Length);
        Assert.AreEqual(1, outlierNodes.Length);
    }
}
