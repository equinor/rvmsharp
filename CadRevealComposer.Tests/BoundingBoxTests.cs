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
}
