namespace CadRevealComposer.Tests.Primitives.Converters
{
    using CadRevealComposer.Operations.Converters;
    using CadRevealComposer.Primitives;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Numerics;

    [TestFixture]
    public class RvmSnoutConverterTests
    {
        private static RvmSnout _rvmSnout;
        private static RvmNode _rvmNode = new RvmNode(2, "SnoutParent", Vector3.One, 2);
        private static CadRevealNode _revealNode = new CadRevealNode() {NodeId = 456, TreeIndex = 1337};

        [SetUp]
        public void Setup()
        {
            _rvmSnout = new RvmSnout(
                Version: 2,
                Matrix: Matrix4x4.Identity,
                BoundingBoxLocal: new RvmBoundingBox(Vector3.Zero, Vector3.Zero),
                RadiusBottom: 1,
                RadiusTop: 0.1f,
                Height: 2,
                OffsetX: 0,
                OffsetY: 0,
                BottomShearX: 0,
                BottomShearY: 0,
                TopShearX: 0,
                TopShearY: 0);

            _rvmNode = new RvmNode(2, "SnoutParent", Vector3.One, 2);

            _revealNode = new CadRevealNode() {NodeId = 456, TreeIndex = 1337};
        }

        [Test]
        public void ConvertToRevealPrimitive()
        {
            var cone = _rvmSnout.ConvertToRevealPrimitive(_revealNode, _rvmNode);

            Assert.That(cone, Is.Not.Null);
            Assert.That(cone, Is.TypeOf<ClosedCone>());
        }

        [Test]
        public void ConvertToRevealPrimitive_WhenConnections_ProducesOpenCone()
        {
            _rvmSnout.Connections[0] = new RvmConnection(_rvmSnout, _rvmSnout, 0, 0, Vector3.One, Vector3.UnitZ,
                RvmConnection.ConnectionType.HasCircularSide);

            var cone = _rvmSnout.ConvertToRevealPrimitive(_revealNode, _rvmNode);

            Assert.That(cone, Is.Not.Null);
            Assert.That(cone, Is.TypeOf<OpenCone>());
        }

        [Test]
        [Ignore("Case Not implemented yet")]
        public void ConvertToRevealPrimitive_WhenOffset_ProducesCorrectPrimitive()
        {
            var rvmSnoutWithOffset = _rvmSnout with {OffsetX = 1};
            var cone = rvmSnoutWithOffset.ConvertToRevealPrimitive(_revealNode, _rvmNode);
            Assert.That(cone, Is.Not.Null);
        }
    }
}