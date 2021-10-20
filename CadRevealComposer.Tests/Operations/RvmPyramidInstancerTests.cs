namespace CadRevealComposer.Tests.Operations
{
    using CadRevealComposer.Operations;
    using CadRevealComposer.Primitives;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Drawing;
    using System.Numerics;

    [TestFixture]
    public class RvmPyramidInstancerTests
    {
        [Test]
        public void Process_WhenTwoIdenticalMeshes_IgnoresOneOfThem()
        {
            // Arbitrary arguments.
            var rvmPyramid = new RvmPyramid(2, Matrix4x4.Identity, new RvmBoundingBox(Vector3.One, Vector3.One), 1, 1,
                1, 1, 1, 1, 1);

            var props = new CommonPrimitiveProperties(
                1, 1, Vector3.Zero, Quaternion.Identity, Vector3.One, 0, new RvmBoundingBox(Vector3.One, Vector3.One),
                Color.Aqua, (Vector3.One, 0));

            // Mark: These two input pyramids will be identical as they are Records with identical values.
            ProtoMeshFromPyramid[] protoPyramids = new []{new ProtoMeshFromPyramid(props, rvmPyramid), new ProtoMeshFromPyramid(props, rvmPyramid)};

            var res =
                RvmPyramidInstancer.Process(protoPyramids);

            Assert.That(res, Has.One.Items);
        }
    }
}