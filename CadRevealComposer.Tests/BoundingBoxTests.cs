namespace CadRevealComposer.Tests;

using System.Drawing;
using System.Numerics;

public class BoundingBoxTests
{
    [Test]
    public void ToBoxPrimitive_WhenGivenBoundingBox_ReturnsBoxWithExpectedData()
    {
        // Arrange
        var boundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));

        // Act
        var box = boundingBox.ToBoxPrimitive(1337, Color.Yellow);

        // Assert
        Assert.That(
            box.InstanceMatrix,
            Is.EqualTo(Matrix4x4.CreateScale(boundingBox.Extents) * Matrix4x4.CreateTranslation(boundingBox.Center))
        );
        Assert.That(box.TreeIndex, Is.EqualTo(1337));
        Assert.That(box.Color, Is.EqualTo(Color.Yellow));
    }

    [Test]
    public void EqualTo_GivenEqualBoundingBox_ReturnsTrue()
    {
        var boundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        var equalBoundingBox = new BoundingBox(new Vector3(1.00001f, 2, 3), new Vector3(4, 5, 6));

        Assert.That(boundingBox.EqualTo(equalBoundingBox), Is.True);
    }

    [Test]
    public void EqualTo_GivenNotEqualBoundingBox_ReturnsFalse()
    {
        var boundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        var notEqualBoundingBox = new BoundingBox(new Vector3(1.01f, 2, 3), new Vector3(4, 5, 6));

        Assert.That(boundingBox.EqualTo(notEqualBoundingBox), Is.False);
    }

    [Test]
    public void EqualToWithVaryingPrecision_GivenSimilarBoundingBox_ReturnsCorrectResult()
    {
        var boundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        var similarBoundingBox = new BoundingBox(new Vector3(1.001f, 2, 3), new Vector3(4, 5, 6));

        Assert.That(boundingBox.EqualTo(similarBoundingBox), Is.False);
        Assert.That(boundingBox.EqualTo(similarBoundingBox, 2), Is.True);
    }

    [Test]
    public void Center_ReturnsCenter()
    {
        var boundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        var center = new Vector3((1 + 4) / 2f, (2 + 5) / 2f, (3 + 6) / 2f);
        Assert.That(boundingBox.Center, Is.EqualTo(center));
    }

    [Test]
    public void Extents_ReturnsSizeInAllDimensions()
    {
        var boundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));
        var extents = new Vector3(3, 3, 3);
        Assert.That(boundingBox.Extents, Is.EqualTo(extents));
    }

    [Test]
    public void Volume_CalculatesCorrectVolume()
    {
        var boundingBox = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 6, 8));
        const float expectedVolume = 3 * 4 * 5; // Extents.X * Extents.Y * Extents.Z
        Assert.That(boundingBox.Volume, Is.EqualTo(expectedVolume));
    }
}
