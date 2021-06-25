namespace CadRevealComposer.Tests.Primitives.Converters
{
    using CadRevealComposer.Primitives;
    using CadRevealComposer.Primitives.Converters;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Numerics;

    [TestFixture]
    public class RvmPyramidConverterTests
    {
        private RvmPyramid _rvmPyramid;

        private RvmNode _rvmNode;

        const ulong NodeId = 675;
        const int TreeIndex = 1337;
        private CadRevealNode _revealNode;


        [SetUp]
        public void Setup()
        {
            _rvmPyramid = new RvmPyramid(
                Version: 2,
                Matrix: Matrix4x4.Identity,
                BoundingBoxLocal: new RvmBoundingBox(-Vector3.One, Vector3.One),
                BottomX: 100,
                BottomY: 50,
                TopX: 50,
                TopY: 25,
                OffsetX: 0,
                OffsetY: 0,
                Height: 25
            );
            _rvmNode = new RvmNode(2, "BoxNode", new Vector3(1, 2, 3), 2);
            _revealNode = new CadRevealNode() { NodeId = NodeId, TreeIndex = TreeIndex };
        }


        [Test]
        public void ConvertRvmPyramid_WhenTopAndBottomIsEqualAndNoOffset_IsBox()
        {
            var transform = Matrix4x4.Identity; // No rotation, scale 1, position at 0

            var rvmBox = new RvmBox(Version: 2,
                transform,
                new RvmBoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3)),
                LengthX: 2, LengthY: 4, LengthZ: 6);
            var rvmNode = new RvmNode(2, "BoxNode", new Vector3(1, 2, 3), 2);
            var revealNode = new CadRevealNode() { NodeId = NodeId, TreeIndex = TreeIndex };
            var box = rvmBox.ConvertToRevealPrimitive(revealNode, rvmNode);

            Assert.That(box, Is.Not.Null);
            Assert.That(box, Is.TypeOf<Box>());
            Assert.That(box.DeltaX, Is.EqualTo(2));
            Assert.That(box.DeltaY, Is.EqualTo(4));
            Assert.That(box.DeltaZ, Is.EqualTo(6));
            Assert.That(box.CenterX, Is.EqualTo(0)); // Translation of RvmNode is ignored.
            Assert.That(box.CenterY, Is.EqualTo(0));
            Assert.That(box.CenterZ, Is.EqualTo(0));
            Assert.That(box.NodeId, Is.EqualTo(NodeId));
            Assert.That(box.TreeIndex, Is.EqualTo(TreeIndex));
            Assert.That(box.Normal, Is.EqualTo(Vector3.UnitZ));
            Assert.That(box.RotationAngle, Is.EqualTo(0));
        }
    }
}