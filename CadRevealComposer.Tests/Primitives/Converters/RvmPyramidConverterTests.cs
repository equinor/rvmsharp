namespace CadRevealComposer.Tests.Primitives.Converters
{
    using CadRevealComposer.Primitives;
    using CadRevealComposer.Primitives.Converters;
    using CadRevealComposer.Utils;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using RvmSharp.Tessellation;
    using System.Numerics;

    [TestFixture]
    public class RvmPyramidConverterTests
    {
        private RvmPyramid RvmPyramidTestInstance;
        private readonly RvmBoundingBox _throwawayBoundingBox = new RvmBoundingBox(Vector3.Zero, Vector3.Zero);

        internal RvmNode RvmNodeTestInstance;

        internal const ulong NodeId = 675;
        internal const int TreeIndex = 1337;
        internal CadRevealNode RevealNode;


        [SetUp]
        public void Setup()
        {
            RvmPyramidTestInstance = new RvmPyramid(
                Version: 2,
                Matrix: Matrix4x4.Identity,
                BoundingBoxLocal: _throwawayBoundingBox,
                BottomX: 100,
                BottomY: 50,
                TopX: 50,
                TopY: 25,
                OffsetX: 0,
                OffsetY: 0,
                Height: 25
            );
            RvmNodeTestInstance = new RvmNode(2, "BoxNode", new Vector3(1, 2, 3), 2);
            RevealNode = new CadRevealNode() { NodeId = NodeId, TreeIndex = TreeIndex };
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

        [TestFixture]
        internal class PyramidTemplater : RvmPyramidConverterTests
        {
            private const int UnusedTolerance = -1;

            [Test]
            [DefaultFloatingPointTolerance(0.001)]
            public void ConvertToUnitSizeInXyz()
            {
                const float bottomX = 2.0f;
                const float bottomY = 3.0f;
                const float topX = 4f;
                const float topY = 1f;
                const float offsetX = 2f;
                const float offsetY = 3f;
                const float height = 4f;
                var pyramidA = new RvmPyramid(2, Matrix4x4.Identity, _throwawayBoundingBox, bottomX, bottomY, topX, topY,
                    offsetX, offsetY, height);

                RvmPyramid pyramid =
                    PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(pyramidA);

                Assert.That(pyramid.BottomX, Is.EqualTo(1));
                Assert.That(pyramid.TopX, Is.EqualTo(2));
                Assert.That(pyramid.OffsetX, Is.EqualTo(1f));

                // Check Y sides
                Assert.That(pyramid.BottomY, Is.EqualTo(1f));
                Assert.That(pyramid.TopY, Is.EqualTo(topY / bottomY));
                Assert.That(pyramid.OffsetY, Is.EqualTo(offsetY / bottomY));
                // Check height
                Assert.That(pyramid.Height, Is.EqualTo(1));
            }


            [Test]
            public void TwoPyramidsWithSimilarProportionsAreTheSame()
            {
                var p1 = new RvmPyramid(Version: 2,
                    Matrix: Matrix4x4.Identity,
                    BoundingBoxLocal: _throwawayBoundingBox,
                    BottomX: 2,
                    BottomY: 4,
                    TopX: 6,
                    TopY: 1,
                    OffsetX: 2,
                    OffsetY: 3,
                    Height: 1);


                var p2 = new RvmPyramid(Version: 2,
                    Matrix: Matrix4x4.Identity,
                    BoundingBoxLocal: _throwawayBoundingBox,
                    BottomX: 1,
                    2,
                    3,
                    0.5f,
                    1,
                    1.5f,
                    2f);


                var meshP2 = TessellatorBridge.Tessellate(p2, UnusedTolerance);

                var p3 = p1 with { TopX = p1.TopX + 1 }; // Change proportions of a dimension (Should not match)

                Assert.That(p1, Is.Not.EqualTo(p2));

                RvmPyramid pyramid1 =
                   PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(p1);
                RvmPyramid pyramid2 =
                    PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(p2);
                RvmPyramid pyramid3 =
                    PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(p3);

                var equalMeshPossible12 = PyramidConversionUtils.CanBeRepresentedByEqualMesh(pyramid1, pyramid2);
                Assert.True(equalMeshPossible12, $"Expected {nameof(pyramid1)} to have same mesh representation as {pyramid2}");

                var equalMeshPossible13 = PyramidConversionUtils.CanBeRepresentedByEqualMesh(pyramid1, pyramid3);
                Assert.False(equalMeshPossible13, $"Expected {nameof(pyramid1)} to NOT have same mesh representation as {pyramid3}");

                var srcMesh = TessellatorBridge.Tessellate(p1, 1, UnusedTolerance);
                var scaledUnitMesh = TessellatorBridge.Tessellate(pyramid1, 1, UnusedTolerance);
                srcMesh!.Apply(p1.Matrix);
                scaledUnitMesh!.Apply(pyramid1.Matrix);

                // meshPyramid1!.Apply(Matrix4x4.CreateScale(scales1));
                // Assert.That(meshP1, Is.Not.EqualTo(meshP2))
                Assert.That(srcMesh.Vertices, Is.EqualTo(scaledUnitMesh.Vertices));
                Assert.That(srcMesh.Normals, Is.EqualTo(scaledUnitMesh.Normals));
                Assert.That(srcMesh, Is.EqualTo(scaledUnitMesh));
            }
        }
    }
}