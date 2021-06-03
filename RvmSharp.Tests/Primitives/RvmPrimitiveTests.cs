using NUnit.Framework;

namespace RvmSharp.Tests.Primitives
{
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Numerics;

    [TestFixture]
    [DefaultFloatingPointTolerance(0.0001)]
    public class RvmPrimitiveTests
    {
        [Test]
        public void GetAxisAlignedBoundingBox_WhenNoRotation_AppliesScale()
        {
            var matrix = Matrix4x4Helpers.CalculateTransformMatrix(new Vector3(1.5f, 1.5f, 1.5f),
                Quaternion.Identity, scale: new Vector3(0.01f, 0.01f, 0.01f));

            var min = new Vector3(-50, -50, -50);
            var max = new Vector3(50, 50, 50);
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


        [Test]
        public void GetAxisAlignedBoundingBox_WhenOriginalHasRotation_AppliesScaleAndRotation()
        {
            const float degToRad = MathF.PI / 180;

            var matrix = Matrix4x4Helpers.CalculateTransformMatrix(new Vector3(1.5f, 1.5f, 1.5f),
                Quaternion.CreateFromYawPitchRoll(0, 45f * degToRad, 0), scale: new Vector3(0.01f, 0.01f, 0.01f));

            var min = new Vector3(-50, -50, -50);
            var max = new Vector3(50, 50, 50);
            var primitive = new RvmBox(2, matrix, new RvmBoundingBox(Min: min, Max: max), 1, 1, 1);

            (Vector3 bbMin, Vector3 bbMax) = primitive.CalculateAxisAlignedBoundingBox();
            var diagonal = Vector3.Distance(bbMin, bbMax);

            Assert.That(bbMin.X, Is.EqualTo(1));
            Assert.That(bbMin.Y, Is.EqualTo(0.792893231));
            Assert.That(bbMin.Z, Is.EqualTo(0.792893231));
            Assert.That(bbMax.X, Is.EqualTo(2));
            Assert.That(bbMax.Y, Is.EqualTo(2.2071068));
            Assert.That(bbMax.Z, Is.EqualTo(2.2071068));
            Assert.That(diagonal, Is.EqualTo(2.236068));
        }
    }
}