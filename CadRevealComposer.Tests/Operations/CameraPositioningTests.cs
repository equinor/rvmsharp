namespace CadRevealComposer.Tests.Operations
{
    using CadRevealComposer.Operations;
    using CadRevealComposer.Primitives;
    using NUnit.Framework;
    using RvmSharp.Primitives;
    using System.Drawing;
    using System.Numerics;

    [TestFixture]
    public class CameraPositioningTests
    {
        [Test]
        public void InitialCameraPositionXaxis()
        {
            APrimitive[] geometries = {
                new TestPrimitiveWithBoundingBox(
                    new Vector3(1, 1, 1),
                    new Vector3(1, 1, 1)
                    )
            };

            var (position, direction) = CameraPositioning.CalculateInitialCamera(geometries);
            Assert.AreEqual(new Vector3(), position);
            Assert.AreEqual(new Vector3(), direction);
        }

        [Test]
        public void InitialCameraPositionYaxis()
        {
            APrimitive[] geometries = {
                new TestPrimitiveWithBoundingBox(
                    new Vector3(1, 1, 1),
                    new Vector3(1, 1, 1)
                )
            };

            var (position, direction) = CameraPositioning.CalculateInitialCamera(geometries);
            Assert.AreEqual(new Vector3(), position);
            Assert.AreEqual(new Vector3(), direction);
        }

        private record TestPrimitiveWithBoundingBox : APrimitive
        {
            public TestPrimitiveWithBoundingBox(Vector3 bbMin, Vector3 bbMax)
                : base(new CommonPrimitiveProperties(
                    int.MaxValue,
                    int.MaxValue,
                    Vector3.One,
                    Quaternion.Identity,
                    Vector3.One,
                    float.Epsilon,
                    new RvmBoundingBox(bbMin, bbMax),
                    Color.Black,
                    (Vector3.One, float.Epsilon),
                    null))
            {

            }
        }
    }
}