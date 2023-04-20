namespace CadRevealComposer.Tests;

[TestFixture]
public class ProgramTests
{
    [Test]
    public void CollectGeometryNodesRecursive_WhenGivenNodes_ExpandsHierarchyWithChildren()
    {
        // var leaf = new RvmBox(2, Matrix4x4.Identity, new RvmBoundingBox(Max: Vector3.One, Min: Vector3.Zero),
        //     1, 1, 1);
        //
        // var node1 = new RvmNode(2, "Node1", Vector3.Zero, 1);
        //
        // node1.Children.Add(leaf);
        // var rvmRootNode = new RvmNode(2, "Root", Vector3.Zero, 1);
        // rvmRootNode.Children.Add(node1);
        //
        // var nodeIdGenerator = new NodeIdProvider();
        // var treeIndexGenerator = new TreeIndexGenerator();
        //
        // var rootNodeCadNode =
        //     RvmNodeToCadRevealNodeConverter.CollectGeometryNodesRecursive(rvmRootNode, new CadRevealNode(), nodeIdGenerator, treeIndexGenerator);
        //
        // Assert.That(rootNodeCadNode.Children, Has.One.Items);
        // Assert.That(rootNodeCadNode.Children![0].Group, Is.EqualTo(node1));
        //
        // // Expecting that the leaf node not is included (yet).
        // Assert.That(rootNodeCadNode.Children[0].Children, Is.Null.Or.Empty);
    }
}
