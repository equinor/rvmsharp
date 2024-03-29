﻿namespace CadRevealComposer.Tests;

[TestFixture]
public class BoxCadRevealPrimitiveTests
{
    [Test]
    [DefaultFloatingPointTolerance(0.0001)]
    public void FromBoxPrimitive_WhenTested_CreatesNewPrimitive()
    {
        // CadRevealNode revealNode = new CadRevealNode();
        // RvmNode container = new RvmNode(2, "Name", Vector3.One, 2);
        //
        // var center = new Vector3(10, 0, 0);
        // var expectedAngle = 1.337f;
        //
        // // TODO: Test with real values.
        // var matrix = Matrix4x4Helpers.CalculateTransformMatrix(center,
        //     Quaternion.CreateFromAxisAngle(new Vector3(0.0f, 0, 1), expectedAngle),
        //     new Vector3(2, 2, 2));
        //
        // var boundingBox = new RvmBoundingBox(Min: new Vector3(9, -1, -1), Max: new Vector3(11, 1, 1));
        // var expectedDiagonal = 7.90086317; // Diagonal of scaled, rotated bounding box.
        //
        // RvmBox rvmBox = new RvmBox(2, matrix, boundingBox, 2, 2, 2);
        // revealNode.Group = container;
        // var box = APrimitive.FromRvmPrimitive(revealNode, rvmBox).SingleOrDefault() as Box;
        //
        // Assert.That(box, Is.Not.Null);
        // Assert.That(box.InstanceMatrix.Translation.X, Is.EqualTo(10));
        // Assert.That(box.InstanceMatrix.Translation.Y, Is.EqualTo(0));
        // Assert.That(box.InstanceMatrix.Translation.Z, Is.EqualTo(0));
        // Assert.That(box.AxisAlignedBoundingBox.Diagonal, Is.EqualTo(expectedDiagonal));
    }
}
