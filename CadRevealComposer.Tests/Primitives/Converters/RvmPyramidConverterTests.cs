namespace CadRevealComposer.Tests.Primitives.Converters;

using CadRevealComposer.Operations.Converters;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using NUnit.Framework;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System.Drawing;
using System.Linq;
using System.Numerics;

[TestFixture]
public class RvmPyramidConverterTests
{
    private readonly RvmBoundingBox _throwawayBoundingBox = new RvmBoundingBox(Vector3.Zero, Vector3.Zero);

    [Test]
    public void ConvertRvmPyramid_WhenTopAndBottomIsEqualAndNoOffset_IsBox()
    {
        var transform = Matrix4x4.Identity; // No rotation, scale 1, position at 0

        var rvmBox = new RvmBox(Version: 2,
            transform,
            new RvmBoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3)),
            LengthX: 2, LengthY: 4, LengthZ: 6);
        var box = rvmBox.ConvertToRevealPrimitive(1337, Color.Red).SingleOrDefault() as Box;

        Assert.That(box, Is.Not.Null);
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

            var rvmPyramidCUnique = rvmPyramidA with { TopX = rvmPyramidA.TopX + 1 }; // Change proportions of a dimension (Should not match)

            Assert.That(rvmPyramidA, Is.Not.EqualTo(rvmPyramidAHalfScaled));

            RvmPyramid pyramid1 =
                PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(rvmPyramidA);
            RvmPyramid pyramid2 =
                PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(rvmPyramidAHalfScaled);
            RvmPyramid pyramid3 =
                PyramidConversionUtils.CreatePyramidWithUnitSizeInAllDimension(rvmPyramidCUnique);

            var equalMeshPossible12 = PyramidConversionUtils.CanBeRepresentedByEqualMesh(pyramid1, pyramid2);
            Assert.True(equalMeshPossible12, $"Expected {nameof(pyramid1)} to have same mesh representation as {pyramid2}");

            var equalMeshPossible13 = PyramidConversionUtils.CanBeRepresentedByEqualMesh(pyramid1, pyramid3);
            Assert.False(equalMeshPossible13, $"Expected {nameof(pyramid1)} to NOT have same mesh representation as {pyramid3}");

            var srcMesh = TessellatorBridge.TessellateWithoutApplyingMatrix(rvmPyramidA, 1, UnusedTolerance);
            var scaledUnitMesh = TessellatorBridge.TessellateWithoutApplyingMatrix(pyramid1, 1, UnusedTolerance);
            srcMesh!.Apply(rvmPyramidA.Matrix);
            scaledUnitMesh!.Apply(pyramid1.Matrix);

            // meshPyramid1!.Apply(Matrix4x4.CreateScale(scales1));
            // Assert.That(meshP1, Is.Not.EqualTo(meshP2))
            Assert.That(srcMesh.Vertices, Is.EqualTo(scaledUnitMesh.Vertices));
            Assert.That(srcMesh.Normals, Is.EqualTo(scaledUnitMesh.Normals));
            Assert.That(srcMesh, Is.EqualTo(scaledUnitMesh));
        }
    }
}