namespace CadRevealComposer.Tests.Operations
{
    using CadRevealComposer.Operations;
    using CadRevealComposer.Primitives;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;

    [TestFixture]
    public class RvmPyramidInstancerTests
    {
        private RvmBoundingBox _throwawayBoundingBox => new RvmBoundingBox(Vector3.Zero, Vector3.Zero);

        [Test]
        public void Process_WhenTwoIdenticalMeshes_IgnoresOneOfThem()
        {
            // Arbitrary arguments.
            var rvmPyramid = new RvmPyramid(2, Matrix4x4.Identity, _throwawayBoundingBox, 1, 1,
                1, 1, 1, 1, 1);

            // Arbitrary arguments.
            var rvmPyramidNotMAtching = new RvmPyramid(2, Matrix4x4.Identity, _throwawayBoundingBox, 1, 1,
                1, 1, 2, 2, 1);

            var props = new CommonPrimitiveProperties(
                1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 0, new RvmBoundingBox(Vector3.One, Vector3.One),
                Color.Aqua, (Vector3.One, 0), null!);

            // Mark: These two input pyramids will be identical as they are Records with identical values.
            ProtoMeshFromPyramid[] protoPyramids = new[]
            {
                new ProtoMeshFromPyramid(props, rvmPyramid),
                new ProtoMeshFromPyramid(props, rvmPyramid),
                new ProtoMeshFromPyramid(props, rvmPyramidNotMAtching)
            };

            var res =
                RvmPyramidInstancer.Process(protoPyramids, _ => true);

            Assert.That(res, Has.Exactly(2).Items.InstanceOf<RvmPyramidInstancer.InstancedResult>());
        }

        [Test]
        public void TwoPyramidsWithSimilarProportionsAreTheSame()
        {
            var rvmPyramidA = new RvmPyramid(Version: 2,
                Matrix: Matrix4x4.Identity,
                BoundingBoxLocal: _throwawayBoundingBox,
                BottomX: 2,
                BottomY: 4,
                TopX: 6,
                TopY: 1,
                OffsetX: 2,
                OffsetY: 3,
                Height: 1);


            var rvmPyramidAHalfScaled = new RvmPyramid(Version: 2,
                Matrix: Matrix4x4.Identity,
                BoundingBoxLocal: _throwawayBoundingBox,
                BottomX: 1,
                2,
                3,
                0.5f,
                1,
                1.5f,
                2f);

            var rvmPyramidCUnique =
                rvmPyramidA with
                {
                    TopX = rvmPyramidA.TopX + 1
                }; // Change proportions of a dimension (Should not match)
            var arbitraryProps = new CommonPrimitiveProperties(0, 1, Vector3.Zero, Quaternion.Identity, Vector3.One,
                1,
                _throwawayBoundingBox, Color.Aqua, (Vector3.One, 0), null!);
            var protoPyramids = new[] { rvmPyramidA, rvmPyramidAHalfScaled, rvmPyramidCUnique }
                .Select(rvmPyramid => new ProtoMeshFromPyramid(arbitraryProps, rvmPyramid))
                .ToArray();

            Assert.That(rvmPyramidA, Is.Not.EqualTo(rvmPyramidAHalfScaled));

            var results = RvmPyramidInstancer.Process(protoPyramids, _ => true);

            var templates = results
                .OfType<RvmPyramidInstancer.TemplateResult>()
                .Select(x => x.Template)
                .ToArray();

            var instancedPyramids = results
                .OfType<RvmPyramidInstancer.InstancedResult>()
                .Select(x => x.Pyramid.Pyramid)
                .ToArray();

            var notInstancedPyramids = results
                .OfType<RvmPyramidInstancer.NotInstancedResult>()
                .Select(x => x.Pyramid.Pyramid)
                .ToArray();

            Assert.That(templates, Contains.Item(rvmPyramidA));
            Assert.That(notInstancedPyramids, Contains.Item(rvmPyramidCUnique));
            Assert.That(instancedPyramids, Contains.Item(rvmPyramidAHalfScaled)); // This should have been templated
        }
    }
}