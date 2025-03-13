using CadRevealComposer;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using System.Security.Cryptography;

[TestFixture]
public class SortedBoundingBoxExtentTests
{
    [Test]
    public void GivenABoundingBox_WhenLenXIsLargestAndLenZIsSmallest_ThenTheExtentIsSortedCorrectlyAndAxisIndicesAreCorrect()
    {
        // Arrange
        var boundingBox = new BoundingBox(new Vector3(-30.0f, 20.0f, -15.0f), new Vector3(-20.0f, 25.0f, -14.0f));

        // Act
        var sortedBoundingBox = new SortedBoundingBoxExtent(boundingBox);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sortedBoundingBox.ValueOfLargest, Is.EqualTo(boundingBox.Extents.X).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfMiddle, Is.EqualTo(boundingBox.Extents.Y).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfSmallest, Is.EqualTo(boundingBox.Extents.Z).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.AxisIndexOfLargest, Is.EqualTo(0));
            Assert.That(sortedBoundingBox.AxisIndexOfMiddle, Is.EqualTo(1));
            Assert.That(sortedBoundingBox.AxisIndexOfSmallest, Is.EqualTo(2));
        });
    }

    [Test]
    public void GivenABoundingBox_WhenLenYIsLargestAndLenZIsSmallest_ThenTheExtentIsSortedCorrectlyAndAxisIndicesAreCorrect()
    {
        // Arrange
        var boundingBox = new BoundingBox(new Vector3(20.0f, -30.0f, -15.0f), new Vector3(25.0f, -20.0f, -14.0f));

        // Act
        var sortedBoundingBox = new SortedBoundingBoxExtent(boundingBox);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sortedBoundingBox.ValueOfLargest, Is.EqualTo(boundingBox.Extents.Y).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfMiddle, Is.EqualTo(boundingBox.Extents.X).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfSmallest, Is.EqualTo(boundingBox.Extents.Z).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.AxisIndexOfLargest, Is.EqualTo(1));
            Assert.That(sortedBoundingBox.AxisIndexOfMiddle, Is.EqualTo(0));
            Assert.That(sortedBoundingBox.AxisIndexOfSmallest, Is.EqualTo(2));
        });
    }

    [Test]
    public void GivenABoundingBox_WhenLenZIsLargestAndLenXIsSmallest_ThenTheExtentIsSortedCorrectlyAndAxisIndicesAreCorrect()
    {
        // Arrange
        var boundingBox = new BoundingBox(new Vector3(-15.0f, 20.0f, -30.0f), new Vector3(-14.0f, 25.0f, -20.0f));

        // Act
        var sortedBoundingBox = new SortedBoundingBoxExtent(boundingBox);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sortedBoundingBox.ValueOfLargest, Is.EqualTo(boundingBox.Extents.Z).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfMiddle, Is.EqualTo(boundingBox.Extents.Y).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfSmallest, Is.EqualTo(boundingBox.Extents.X).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.AxisIndexOfLargest, Is.EqualTo(2));
            Assert.That(sortedBoundingBox.AxisIndexOfMiddle, Is.EqualTo(1));
            Assert.That(sortedBoundingBox.AxisIndexOfSmallest, Is.EqualTo(0));
        });
    }

    [Test]
    public void GivenABoundingBox_WhenLenZIsLargestAndLenXAndYAreSmallest_ThenTheExtentIsSortedCorrectlyAndAxisIndicesAreCorrect()
    {
        // Arrange
        var boundingBox = new BoundingBox(new Vector3(-15.0f, 24.0f, -30.0f), new Vector3(-14.0f, 25.0f, -20.0f));

        // Act
        var sortedBoundingBox = new SortedBoundingBoxExtent(boundingBox);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sortedBoundingBox.ValueOfLargest, Is.EqualTo(boundingBox.Extents.Z).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfMiddle, Is.EqualTo(boundingBox.Extents.Y).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.ValueOfSmallest, Is.EqualTo(boundingBox.Extents.X).Within(1.0E-13f));
            Assert.That(sortedBoundingBox.AxisIndexOfLargest, Is.EqualTo(2));
            Assert.That(sortedBoundingBox.AxisIndexOfMiddle, Is.EqualTo(1).Or.EqualTo(0));
            Assert.That(sortedBoundingBox.AxisIndexOfSmallest, Is.EqualTo(0).Or.EqualTo(1));
            Assert.That(sortedBoundingBox.AxisIndexOfSmallest, Is.Not.EqualTo(sortedBoundingBox.AxisIndexOfMiddle));
        });
    }

    [Test]
    public void GivenBoundingBox_WhenShapedAsABeam_ThenCalculatePointsAtEndOfBeamCenteredRelativeToBeamThicknessAndTopOfBeam()
    {
        // Arrange
        var boundingBox = new BoundingBox(new Vector3(-30.0f, 20.0f, -30.0f), new Vector3(-10, 20.5f, -25.0f)); // Length:20, Depth:0.5, Height:5
        var sortedBoundingBox = new SortedBoundingBoxExtent(boundingBox);

        // Act
        var points = sortedBoundingBox.CalcPointsAtEndOfABeamShapedBox(
            SortedBoundingBoxExtent.DisplacementOrigin.BeamTop,
            1.0f
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(points.p1, Is.EqualTo(new Vector3(-30.0f, 20.25f, -26.0f)).Using<Vector3>(Vector3Comparator));
            Assert.That(points.p2, Is.EqualTo(new Vector3(-10.0f, 20.25f, -26.0f)).Using<Vector3>(Vector3Comparator));

            return;
            bool Vector3Comparator(Vector3 v1, Vector3 v2) =>
                Math.Abs(v1.X - v2.X) < 1.0E-3 && Math.Abs(v1.Y - v2.Y) < 1.0E-3 && Math.Abs(v1.Z - v2.Z) < 1.0E-3;
        });
    }

    [Test]
    public void GivenBoundingBox_WhenShapedAsABeam_ThenCalculatePointsAtEndOfBeamCenteredRelativeToBeamThicknessAndBottomOfBeam()
    {
        // Arrange
        var boundingBox = new BoundingBox(new Vector3(-30.0f, 20.0f, -30.0f), new Vector3(-10, 20.5f, -25.0f)); // Length:20, Depth:0.5, Height:5
        var sortedBoundingBox = new SortedBoundingBoxExtent(boundingBox);

        // Act
        var points = sortedBoundingBox.CalcPointsAtEndOfABeamShapedBox(
            SortedBoundingBoxExtent.DisplacementOrigin.BeamBottom,
            1.0f
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(points.p1, Is.EqualTo(new Vector3(-30.0f, 20.25f, -29.0f)).Using<Vector3>(Vector3Comparator));
            Assert.That(points.p2, Is.EqualTo(new Vector3(-10.0f, 20.25f, -29.0f)).Using<Vector3>(Vector3Comparator));

            return;
            bool Vector3Comparator(Vector3 v1, Vector3 v2) =>
                Math.Abs(v1.X - v2.X) < 1.0E-3 && Math.Abs(v1.Y - v2.Y) < 1.0E-3 && Math.Abs(v1.Z - v2.Z) < 1.0E-3;
        });
    }
}
