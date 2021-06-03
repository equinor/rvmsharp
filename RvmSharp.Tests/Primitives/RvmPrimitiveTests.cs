using NUnit.Framework;

namespace RvmSharp.Tests.Primitives
{
    using RvmSharp.Operations;
    using RvmSharp.Primitives;
    using System;
    using System.Collections;
    using System.Linq;
    using System.Numerics;

    [TestFixture]
    [DefaultFloatingPointTolerance(0.0001)]
    public class RvmPrimitiveTests
    {
        static readonly Vector3 Min = new Vector3(-50, -50, -50);
        static readonly Vector3 Max = new Vector3(50, 50, 50);
        private static readonly RvmBoundingBox DefaultBoundingBox = new RvmBoundingBox(Min: Min, Max: Max);

        private static RvmBox CreateUnitBoxWithMatrix(Matrix4x4 transform)
        {
            return new RvmBox(2, transform, DefaultBoundingBox, 1, 1, 1);
        }

        public class BoundingBoxTestCaseDescription
        {
            public string TestDescription = "";
            public Vector3 Position = Vector3.Zero;
            public Quaternion Rotation = Quaternion.Identity;
            public Vector3 Scale = Vector3.One;

            public Matrix4x4 Transform => Matrix4x4Helpers.CalculateTransformMatrix(Position,
                Rotation, Scale);

            public Vector3 ExpectedMin = Vector3.One * 100 / 2 * -1;
            public Vector3 ExpectedMax = Vector3.One * 100 / 2;
            public double ExpectedDiagonal = 173.205081;
        }

        private static IEnumerable _boundingBoxTestCases = new[]
        {
            new BoundingBoxTestCaseDescription() {TestDescription = "Default Test"},
            new BoundingBoxTestCaseDescription()
            {
                TestDescription = "Scaling",
                Scale = Vector3.One * 0.01f,
                ExpectedDiagonal = 1.73205,
                ExpectedMin = new Vector3(-0.5f, -0.5f, -0.5f),
                ExpectedMax = new Vector3(0.5f, 0.5f, 0.5f),
            },
            new BoundingBoxTestCaseDescription()
            {
                TestDescription = "Position",
                Position = new Vector3(100, 100, 100),
                ExpectedDiagonal = 173.205,
                ExpectedMin = new Vector3(50, 50, 50),
                ExpectedMax = new Vector3(150, 150, 150),
            },
            new BoundingBoxTestCaseDescription()
            {
                TestDescription = "Offset, Scaling",
                Position = new Vector3(-100,0,100),
                Scale = Vector3.One * 0.01f,
                ExpectedDiagonal = 1.73205,
                ExpectedMin = new Vector3(-100.5f, -0.5f, 99.5f),
                ExpectedMax = new Vector3(-99.5f, 0.5f, 100.5f),
            },
            new BoundingBoxTestCaseDescription()
            {
                TestDescription = "Rotation",
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 4 /* 45 degrees */),
                ExpectedDiagonal = 223.6068,
                ExpectedMin = new Vector3(-70.71068f, -70.71068f, -50f),
                ExpectedMax = new Vector3(70.71068f, 70.71068f, 50f),
            },
            new BoundingBoxTestCaseDescription()
            {
                TestDescription = "Zero Axis Scale",
                Scale = new Vector3(0, 0, 1),
                ExpectedDiagonal = 100,
                ExpectedMin = new Vector3(0, 0, -50f),
                ExpectedMax = new Vector3(0, 0, 50f),
            },
            new BoundingBoxTestCaseDescription()
            {
                TestDescription = "Negative Axis Scale",
                Scale = new Vector3(-1, 1, 1),
                ExpectedDiagonal = 173.205,
                ExpectedMin = new Vector3(-50, -50, -50f),
                ExpectedMax = new Vector3(50, 50f, 50f),
            },
        }.Select(x => new TestCaseData(x).SetName(x.TestDescription));

        [Test]
        [TestCaseSource(nameof(_boundingBoxTestCases))]
        public void GetAxisAlignedBoundingBox_WithUnitBox(BoundingBoxTestCaseDescription testCaseDescription)
        {
            var primitive = CreateUnitBoxWithMatrix(testCaseDescription.Transform) ;
            RvmBoundingBox bb = primitive.CalculateAxisAlignedBoundingBox();
            var diagonal = bb.Diagonal;

            Assert.That(bb.Min, Is.EqualTo(testCaseDescription.ExpectedMin));
            Assert.That(bb.Max, Is.EqualTo(testCaseDescription.ExpectedMax));
            Assert.That(diagonal, Is.EqualTo(testCaseDescription.ExpectedDiagonal));
        }
    }
}