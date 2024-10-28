namespace CadRevealComposer.Tests.Operations.Splitting;

using System.Numerics;
using CadRevealComposer.Operations;
using CadRevealComposer.Operations.SectorSplitting;
using NUnit.Framework.Legacy;

[TestFixture]
public class SplittingUtilsTests
{
    [Test]
    public void ReturnsCorrectlySplitNodes()
    {
        Node[] nodes = new Node[]
        {
            new Node(4, null, 0, 0, new BoundingBox(Vector3.One, Vector3.Zero), NodePriority.Default),
            new Node(2, null, 0, 0, new BoundingBox(Vector3.Zero, Vector3.One), NodePriority.Default),
            new Node(
                4,
                null,
                0,
                0,
                new BoundingBox(new Vector3(99, 99, 99), new Vector3(100, 100, 100)),
                NodePriority.Default
            ),
            new Node(6, null, 0, 0, new BoundingBox(Vector3.One, Vector3.One), NodePriority.Default)
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
            new Node(4, null, 0, 0, new BoundingBox(Vector3.One, Vector3.Zero), NodePriority.Default),
            new Node(
                4,
                null,
                0,
                0,
                new BoundingBox(new Vector3(99, 99, 99), new Vector3(100, 100, 100)),
                NodePriority.Default
            ),
            new Node(6, null, 0, 0, new BoundingBox(Vector3.One, Vector3.One), NodePriority.Default)
        };

        var groups = SplittingUtils.GroupOutliersRecursive(nodes, 10f);

        Assert.That(groups.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Splitting_ReturnsOutlierSectorsWhenSymmetrical()
    {
        Node[] nodes = new Node[]
        {
            new Node(4, null, 0, 0, new BoundingBox(Vector3.Zero, Vector3.Zero), NodePriority.Default),
            new Node(
                4,
                null,
                0,
                0,
                new BoundingBox(new Vector3(90, 90, 90), new Vector3(100, 100, 100)),
                NodePriority.Default
            ),
            new Node(
                4,
                null,
                0,
                0,
                new BoundingBox(new Vector3(-90, -90, -90), new Vector3(-100, -100, -100)),
                NodePriority.Default
            ),
        };

        var groups = SplittingUtils.GroupOutliersRecursive(nodes, 10f);

        Assert.That(groups.Count(), Is.EqualTo(3));
    }
}
