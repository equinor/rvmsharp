using NUnit.Framework;

namespace RvmSharp.Tests.Primitives
{
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Numerics;

    [TestFixture]
    [DefaultFloatingPointTolerance(0.0001)]
    public class RvmPrimitive
    {
        [Test]
        public void GetAxisAlignedBoundingBox_WhenNoRotation_AppliesScale()
        {
            var matrix = Matrix4x4Helpers.CalculateTransformMatrix(Vector3.Zero,
                Quaternion.Identity, scale: new Vector3(0.01f, 0.01f, 0.01f));

            var min = new Vector3(100, 100, 100);
            var max = new Vector3(200, 200, 200);
            var primitive = new RvmBox(2, matrix, new RvmBoundingBox(Min: min, Max: max), 1, 1, 1);

            (Vector3 bbMin, Vector3 bbMax) = primitive.CalculateAxisAlignedBoundingBox();
            var diagonal = Vector3.Distance(bbMin, bbMax);

            Assert.That(bbMin.X, Is.EqualTo(1));
            Assert.That(bbMin.Y, Is.EqualTo(1));
            Assert.That(bbMin.Z, Is.EqualTo(1));
            Assert.That(bbMax.X, Is.EqualTo(2));
            Assert.That(bbMax.Y, Is.EqualTo(2));
            Assert.That(bbMax.Z, Is.EqualTo(2));
            Assert.That(diagonal, Is.EqualTo(1.73205081));
        }
    }
}