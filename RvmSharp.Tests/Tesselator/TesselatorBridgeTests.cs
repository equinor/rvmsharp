namespace RvmSharp.Tests.Tesselator
{
    using NUnit.Framework;
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Numerics;
    using Tessellation;

    [TestFixture]
    public class TessellatorBridgeTests
    {
        private static readonly RvmBoundingBox ArbitraryBoundingBox =
            new RvmBoundingBox(Min: Vector3.Zero, Max: Vector3.One);

        [TestFixture]
        public class TessellateBoxTests
        {
            [Test]
            public void TessellateBox_WithUnitBox_ReturnsExpected1x1Mesh()
            {
                var unitBox = new RvmBox(1, Matrix4x4.Identity,
                    new RvmBoundingBox(Min: new Vector3(-0.5f, -0.5f, -0.5f), Max: new Vector3(0.5f, 0.5f, 0.5f)), 1, 1,
                    1);

                var randomToleranceValue = 0.1f;
                var box = TessellatorBridge.TessellateWithoutApplyingMatrix(unitBox, 1, randomToleranceValue);
                Assert.That(box, Is.Not.Null);
                Assert.That(box.Vertices, Has.Exactly(24).Items);
            }
        }

        [TestFixture]
        public class TessellatePyramidTests
        {
            [Test]
            public void TessellatePyramid_WithUnitPyramid_MatchesReferenceMethod()
            {
                var unitPyramid = new RvmPyramid(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 1, 0, 1, 0, 0, 1);

                var randomToleranceValue = 0.1f;
                var pyramid = TessellatorBridge.TessellateWithoutApplyingMatrix(unitPyramid, 1, randomToleranceValue);
                Assert.That(pyramid, Is.Not.Null);
            }

            [Test]
            public void TessellatePyramid_WithHeightZero_HasNormalsThatAreNotNan()
            {
                var unitPyramid = new RvmPyramid(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 1, 0, 1, 0, 0, 0);

                var unusedTolerance = 0.1f;
                var pyramid = TessellatorBridge.Tessellate(unitPyramid, unusedTolerance);
                Assert.That(pyramid, Is.Not.Null);

                // Normals should never be NaN or infinite
                Assert.That(pyramid.Normals, Has.All.Matches<Vector3>(x => x.X.IsFinite() && x.Y.IsFinite() && x.Z.IsFinite()));
            }
        }

        [TestFixture]
        public class TessellateCylinderTests
        {
            [Test]
            public void TessellatePyramid_WithUnitPyramid_MatchesReferenceMethod()
            {
                var unitCylinder = new RvmCylinder(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 1);

                var randomToleranceValue = 0.1f;
                var cylinder = TessellatorBridge.TessellateWithoutApplyingMatrix(unitCylinder, 1, randomToleranceValue);

                Assert.That(cylinder, Is.Not.Null);
                Assert.That(cylinder.Triangles, Has.Exactly(156).Items);
            }
        }

        [TestFixture]
        public class TessellateLineTests
        {
            [Test]
            public void TessellateLine_IsNotPossible_ReturnsNull()
            {
                // If somehow Line is tessellated, improve this test.
                var rvmLine = new RvmLine(1, Matrix4x4.Identity, ArbitraryBoundingBox, 1, 3);

                var randomToleranceValue = 0.1f;
                var line = TessellatorBridge.TessellateWithoutApplyingMatrix(rvmLine, 1, randomToleranceValue);

                Assert.That(line, Is.Null);
            }
        }

        [TestFixture]
        public class SigmaErrorTests
        {
            [Test]
            public void SigmaShouldBeOk()
            {
                var rvmCircularTorus = new RvmCircularTorus(1, new Matrix4x4(
                        0.000958819757f, -0.000284015347f, 0, 0,
                        0.000284015347f, 0.000958819757f, 0, 0,
                        0, 0, 0.00100000005f, 0,
                        314.999939f, 122.049881f, 555.681885f, 1),
                    new RvmBoundingBox(new Vector3(-57025, 0, -25), new Vector3(57025, 57025, 25)),
                    57000, 25, 3.14159274f);
                TessellatorBridge.TessellateWithoutApplyingMatrix(rvmCircularTorus, 0.001f, 0.01f);
            }
        }
    }
}