namespace CadRevealComposer.Tests.Primitives.Converters
{
    using CadRevealComposer.Operations.Converters;
    using CadRevealComposer.Primitives;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Numerics;

    [TestFixture]
    public class RvmBoxConverterTests
    {

        [Test]
        public void ConvertRvmBoxToBox()
        {
            const ulong nodeId = 675;
            const int treeIndex = 1337;

            var transform = Matrix4x4.Identity; // No rotation, scale 1, position at 0

            var rvmBox = new RvmBox(Version: 2,
                transform,
                new RvmBoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3)),
                LengthX: 2, LengthY: 4, LengthZ: 6);
            var rvmNode = new RvmNode(2, "BoxNode", new Vector3(1, 2, 3), 2);
            var revealNode = new CadRevealNode() { NodeId = nodeId, TreeIndex = treeIndex };
            var box = rvmBox.ConvertToRevealPrimitive(revealNode, rvmNode);

            Assert.That(box, Is.Not.Null);
            Assert.That(box, Is.TypeOf<Box>());
            Assert.That(box.DeltaX, Is.EqualTo(2));
            Assert.That(box.DeltaY, Is.EqualTo(4));
            Assert.That(box.DeltaZ, Is.EqualTo(6));
            Assert.That(box.CenterX, Is.EqualTo(0)); // Translation of RvmNode is ignored.
            Assert.That(box.CenterY, Is.EqualTo(0));
            Assert.That(box.CenterZ, Is.EqualTo(0));
            Assert.That(box.NodeId, Is.EqualTo(nodeId));
            Assert.That(box.TreeIndex, Is.EqualTo(treeIndex));
            Assert.That(box.Normal, Is.EqualTo(Vector3.UnitZ));
            Assert.That(box.RotationAngle, Is.EqualTo(0));
        }
    }
}