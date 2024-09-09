namespace CadRevealComposer.Tests;

using System.Drawing;
using System.Numerics;
using CadRevealComposer.Utils;

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
    public void CreateBoundingBoxMesh_GivenABoundingBox_ValidateTheOutputMesh()
    {
        // Create randomizer
        var rand = new Random();

        // Create true bounding box, based on random lengths for x, y, z, as well as random position, r
        var xyz = GenRandomVec3(20.0, -5.0);
        var r = GenRandomVec3(100.0, 50.0);
        var trueBoundingBox = new BoundingBox(r - xyz * 0.5f, r + xyz * 0.5f);

        // Create the bounding box mesh (ToBoxMesh is the method we want to test)
        var boundingBoxMesh = trueBoundingBox.ToBoxMesh(0.01f);

        // Verify that all vertices within the generated mesh is on the true bounding box
        // (assures we cannot have a "rotated" Mesh with a bounding box matching the true bounding box)
        const float tolerance = 1.0e-4f;
        var tolerance3 = new Vector3(tolerance, tolerance, tolerance);
        foreach (Vector3 p in boundingBoxMesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    IsWithinBox(p, new BoundingBox(trueBoundingBox.Min - tolerance3, trueBoundingBox.Max + tolerance3)),
                    Is.True
                );
                Assert.That(
                    IsWithinBox(p, new BoundingBox(trueBoundingBox.Min + tolerance3, trueBoundingBox.Max - tolerance3)),
                    Is.False
                );
            });
        }

        // Make sure the bounding box of the Mesh matches the true bounding box
        var boundingBoxOfMesh = boundingBoxMesh.CalculateAxisAlignedBoundingBox();
        Assert.That(
            boundingBoxOfMesh,
            Is.EqualTo(trueBoundingBox)
                .Using<BoundingBox>(
                    (x, y) =>
                        x.Min.EqualsWithinTolerance(y.Min, tolerance) && x.Max.EqualsWithinTolerance(y.Max, tolerance)
                )
        );
        return;

        // Define lambda functions
        Vector3 GenRandomVec3(double maxSize, double offset) =>
            new Vector3(
                (float)(maxSize * rand.NextDouble() - offset),
                (float)(maxSize * rand.NextDouble() - offset),
                (float)(maxSize * rand.NextDouble() - offset)
            );

        bool IsWithinBox(Vector3 coordinate, BoundingBox bbox)
        {
            if (coordinate.X < bbox.Min.X || coordinate.X > bbox.Max.X)
                return false;
            if (coordinate.Y < bbox.Min.Y || coordinate.Y > bbox.Max.Y)
                return false;
            if (coordinate.Z < bbox.Min.Z || coordinate.Z > bbox.Max.Z)
                return false;
            return true;
        }
    }
}
