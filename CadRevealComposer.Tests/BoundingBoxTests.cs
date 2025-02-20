namespace CadRevealComposer.Tests;

using System.Drawing;
using System.Numerics;
using Primitives;
using Tessellation;

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
    public void GivenAMesh_WhenMeshIs2by3by4CenteredAndTransformIsIdentity_ThenProduceABoxMatrixThatScalesByTheSame()
    {
        // Arrange
        var vertices = new Vector3[]
        {
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, -1.5f, 0.0f),
            new Vector3(0.0f, 1.5f, 0.0f),
            new Vector3(0.0f, 0.0f, -2.0f),
            new Vector3(0.0f, 0.0f, 2.0f)
        };
        var indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        var mesh = new Mesh(vertices, indices, 0.0f);
        var instanceMesh = new InstancedMesh(
            0,
            mesh,
            Matrix4x4.Identity,
            0,
            new Color(),
            new BoundingBox(new Vector3(-1.0f, -1.5f, -2.0f), new Vector3(1.0f, 1.5f, 2.0f))
        );

        // Act
        Box box = BoundingBox.ToBoxPrimitive(instanceMesh.TemplateMesh, Matrix4x4.Identity, 0, new Color());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(box.InstanceMatrix[0, 0], Is.EqualTo(2.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[1, 1], Is.EqualTo(3.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[2, 2], Is.EqualTo(4.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 3], Is.EqualTo(1.0f).Within(1.0E-6));
        });

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (i != j)
                    Assert.That(box.InstanceMatrix[i, j], Is.EqualTo(0.0f).Within(1.0E-6));
            }
        }
    }

    [Test]
    public void GivenAMesh_WhenMeshIs2by3by4NotCenteredAndTransformIsIdentity_ThenProduceABoxMatrixThatScalesByTheSameButTranslated()
    {
        // Arrange
        var vertices = new Vector3[]
        {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(2.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 3.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 4.0f)
        };
        var indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        var mesh = new Mesh(vertices, indices, 0.0f);
        var instanceMesh = new InstancedMesh(
            0,
            mesh,
            Matrix4x4.Identity,
            0,
            new Color(),
            new BoundingBox(new Vector3(-1.0f, -1.5f, -2.0f), new Vector3(1.0f, 1.5f, 2.0f))
        );

        // Act
        Box box = BoundingBox.ToBoxPrimitive(instanceMesh.TemplateMesh, Matrix4x4.Identity, 0, new Color());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(box.InstanceMatrix[0, 0], Is.EqualTo(2.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[1, 1], Is.EqualTo(3.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[2, 2], Is.EqualTo(4.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 3], Is.EqualTo(1.0f).Within(1.0E-6));

            Assert.That(box.InstanceMatrix[3, 0], Is.EqualTo(1.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 1], Is.EqualTo(1.5f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 2], Is.EqualTo(2.0f).Within(1.0E-6));

            Assert.That(box.InstanceMatrix[0, 3], Is.EqualTo(0.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[0, 3], Is.EqualTo(0.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[0, 3], Is.EqualTo(0.0f).Within(1.0E-6));
        });

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (i != j)
                    Assert.That(box.InstanceMatrix[i, j], Is.EqualTo(0.0f).Within(1.0E-6));
            }
        }
    }

    [Test]
    public void GivenAMesh_WhenMeshIs2by3by4CenteredAndTransformWithTranslate_ThenProduceABoxMatrixThatScalesByTheSameButTranslated()
    {
        // Arrange
        // csharpier-ignore
        var transform = new Matrix4x4(
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            1.0f, 1.5f, 2.0f, 1.0f
        );
        // csharpier-restore
        var vertices = new Vector3[]
        {
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, -1.5f, 0.0f),
            new Vector3(0.0f, 1.5f, 0.0f),
            new Vector3(0.0f, 0.0f, -2.0f),
            new Vector3(0.0f, 0.0f, 2.0f)
        };
        var indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        var mesh = new Mesh(vertices, indices, 0.0f);
        var instanceMesh = new InstancedMesh(
            0,
            mesh,
            Matrix4x4.Identity,
            0,
            new Color(),
            new BoundingBox(new Vector3(-1.0f, -1.5f, -2.0f), new Vector3(1.0f, 1.5f, 2.0f))
        );

        // Act
        Box box = BoundingBox.ToBoxPrimitive(instanceMesh.TemplateMesh, transform, 0, new Color());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(box.InstanceMatrix[0, 0], Is.EqualTo(2.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[1, 1], Is.EqualTo(3.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[2, 2], Is.EqualTo(4.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 3], Is.EqualTo(1.0f).Within(1.0E-6));

            Assert.That(box.InstanceMatrix[3, 0], Is.EqualTo(1.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 1], Is.EqualTo(1.5f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 2], Is.EqualTo(2.0f).Within(1.0E-6));

            Assert.That(box.InstanceMatrix[0, 3], Is.EqualTo(0.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[0, 3], Is.EqualTo(0.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[0, 3], Is.EqualTo(0.0f).Within(1.0E-6));
        });

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (i != j)
                    Assert.That(box.InstanceMatrix[i, j], Is.EqualTo(0.0f).Within(1.0E-6));
            }
        }
    }

    [Test]
    public void GivenAMesh_WhenMeshIs2by3by4CenteredAndTransformIsScaled_ThenProduceABoxMatrixThatScalesByTheSameAndThenByScaleInTransform()
    {
        // Arrange
        // csharpier-ignore
        var transform = new Matrix4x4(
            4.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 5.0f, 0.0f, 0.0f,
            0.0f, 0.0f, 6.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
        // csharpier-restore
        var vertices = new Vector3[]
        {
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, -1.5f, 0.0f),
            new Vector3(0.0f, 1.5f, 0.0f),
            new Vector3(0.0f, 0.0f, -2.0f),
            new Vector3(0.0f, 0.0f, 2.0f)
        };
        var indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        var mesh = new Mesh(vertices, indices, 0.0f);
        var instanceMesh = new InstancedMesh(
            0,
            mesh,
            Matrix4x4.Identity,
            0,
            new Color(),
            new BoundingBox(new Vector3(-1.0f, -1.5f, -2.0f), new Vector3(1.0f, 1.5f, 2.0f))
        );

        // Act
        Box box = BoundingBox.ToBoxPrimitive(instanceMesh.TemplateMesh, transform, 0, new Color());

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(box.InstanceMatrix[0, 0], Is.EqualTo(2.0f * 4.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[1, 1], Is.EqualTo(3.0f * 5.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[2, 2], Is.EqualTo(4.0f * 6.0f).Within(1.0E-6));
            Assert.That(box.InstanceMatrix[3, 3], Is.EqualTo(1.0f).Within(1.0E-6));
        });

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (i != j)
                    Assert.That(box.InstanceMatrix[i, j], Is.EqualTo(0.0f).Within(1.0E-6));
            }
        }
    }

    [Test]
    public void GivenAMesh_WhenMeshIs1by1by1CenteredAndTransformContainsRotZ_ThenProduceABoxMatrixThatRotateAccordingly()
    {
        // Arrange
        const double angle = Math.PI / 5.0;
        // csharpier-ignore
        var transform = new Matrix4x4(
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, (float)Math.Cos(angle), (float)Math.Sin(angle), 0.0f,
            0.0f, -(float)Math.Sin(angle), (float)Math.Cos(angle), 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
        // csharpier-restore
        var vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0.0f, 0.0f),
            new Vector3(0.5f, 0.0f, 0.0f),
            new Vector3(0.0f, -0.5f, 0.0f),
            new Vector3(0.0f, 0.5f, 0.0f),
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(0.0f, 0.0f, 0.5f)
        };
        var indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        var mesh = new Mesh(vertices, indices, 0.0f);
        var instanceMesh = new InstancedMesh(
            0,
            mesh,
            Matrix4x4.Identity,
            0,
            new Color(),
            new BoundingBox(new Vector3(-1.0f, -1.5f, -2.0f), new Vector3(1.0f, 1.5f, 2.0f))
        );

        // Act
        Box box = BoundingBox.ToBoxPrimitive(instanceMesh.TemplateMesh, transform, 0, new Color());

        // Assert
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Assert.That(box.InstanceMatrix[i, j], Is.EqualTo(transform[i, j]).Within(1.0E-6));
            }
        }
    }

    [Test]
    public void GivenAMesh_WhenMeshIs1by1by1CenteredAndTransformContainsRotXYZ_ThenProduceABoxMatrixThatRotateAccordingly()
    {
        // Arrange
        const double angle = Math.PI / 5.0;
        // csharpier-ignore
        var rotZ = new Matrix4x4(
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, (float)Math.Cos(angle), (float)Math.Sin(angle), 0.0f,
            0.0f, -(float)Math.Sin(angle), (float)Math.Cos(angle), 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
        // csharpier-ignore
        var rotY = new Matrix4x4(
            (float)Math.Cos(angle), 0.0f, -(float)Math.Sin(angle), 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            (float)Math.Sin(angle), 0.0f, (float)Math.Cos(angle), 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
        // csharpier-ignore
        var rotX = new Matrix4x4(
            (float)Math.Cos(angle), (float)Math.Sin(angle), 0.0f, 0.0f,
            -(float)Math.Sin(angle), (float)Math.Cos(angle), 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        );
        // csharpier-restore
        var transform = rotX * rotY * rotZ;
        var vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0.0f, 0.0f),
            new Vector3(0.5f, 0.0f, 0.0f),
            new Vector3(0.0f, -0.5f, 0.0f),
            new Vector3(0.0f, 0.5f, 0.0f),
            new Vector3(0.0f, 0.0f, -0.5f),
            new Vector3(0.0f, 0.0f, 0.5f)
        };
        var indices = new uint[] { 0, 1, 2, 3, 4, 5 };
        var mesh = new Mesh(vertices, indices, 0.0f);
        var instanceMesh = new InstancedMesh(
            0,
            mesh,
            Matrix4x4.Identity,
            0,
            new Color(),
            new BoundingBox(new Vector3(-1.0f, -1.5f, -2.0f), new Vector3(1.0f, 1.5f, 2.0f))
        );

        // Act
        Box box = BoundingBox.ToBoxPrimitive(instanceMesh.TemplateMesh, transform, 0, new Color());

        // Assert
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Assert.That(box.InstanceMatrix[i, j], Is.EqualTo(transform[i, j]).Within(1.0E-6));
            }
        }
    }
}
