namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldOptimizer;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

[TestFixture]
public class ScaffoldOptimizerResultTests
{
    private static Mesh GenSomeMesh()
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();
        vertices.Add(new Vector3(0, 0, 0));
        vertices.Add(new Vector3(0, 1, 0));
        vertices.Add(new Vector3(0, 0, 1));
        indices.Add(0);
        indices.Add(1);
        indices.Add(2);
        return new Mesh(vertices.ToArray(), indices.ToArray(), 1.0E-6f);
    }

    private static InstancedMesh GenSomeInstanceMesh(Matrix4x4 instanceMatrix)
    {
        Mesh mesh = GenSomeMesh();
        return new InstancedMesh(
            0,
            mesh,
            instanceMatrix,
            0,
            Color.Black,
            new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))
        );
    }

    [Test]
    public void ScaffoldOptimizerResult_GivenBaseWithIdentityTransformAndOptimizedEccentricConePrimitive_ThenVerifyTransform()
    {
        // Arrange
        APrimitive basePrimitive = GenSomeInstanceMesh(Matrix4x4.Identity);
        APrimitive optimizedPrimitive = new EccentricCone(
            new Vector3(1, 2, 3),
            new Vector3(8, 10, 9),
            new Vector3(1, 0, 0),
            3,
            4,
            0,
            Color.Brown,
            new BoundingBox(new Vector3(1, 1, 1), new Vector3(10, 10, 10))
        );

        // Act
        var optResult = new ScaffoldOptimizerResult(basePrimitive, optimizedPrimitive);
        var optResultPrimitive = optResult.Get() as EccentricCone;

        // Assert
        Assert.That(optResultPrimitive, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(optResultPrimitive.CenterA.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Y, Is.EqualTo(2).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Z, Is.EqualTo(3).Within(1E-3f));

            Assert.That(optResultPrimitive.CenterB.X, Is.EqualTo(8).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Y, Is.EqualTo(10).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Z, Is.EqualTo(9).Within(1E-3f));

            Assert.That(optResultPrimitive.Normal.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Y, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.RadiusA, Is.EqualTo(3).Within(1E-3f));
            Assert.That(optResultPrimitive.RadiusB, Is.EqualTo(4).Within(1E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Y, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Z, Is.EqualTo(1).Within(1E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.X, Is.EqualTo(10).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Y, Is.EqualTo(10).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Z, Is.EqualTo(10).Within(1E-3f));
        });
    }

    [Test]
    public void ScaffoldOptimizerResult_GivenBaseWithTranslationAndOptimizedEccentricConePrimitive_ThenVerifyTransform()
    {
        // Arrange
        var translation = Matrix4x4.CreateTranslation(new Vector3(4, 2, 7));
        APrimitive basePrimitive = GenSomeInstanceMesh(translation);
        APrimitive optimizedPrimitive = new EccentricCone(
            new Vector3(1, 2, 3),
            new Vector3(8, 10, 9),
            new Vector3(1, 0, 0),
            3,
            4,
            0,
            Color.Brown,
            new BoundingBox(new Vector3(1, 1, 1), new Vector3(10, 10, 10))
        );

        // Act
        var optResult = new ScaffoldOptimizerResult(basePrimitive, optimizedPrimitive);
        var optResultPrimitive = optResult.Get() as EccentricCone;

        // Assert
        Assert.That(optResultPrimitive, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(optResultPrimitive.CenterA.X, Is.EqualTo(5).Within(1.0E-3f));
            Assert.That(optResultPrimitive.CenterA.Y, Is.EqualTo(4).Within(1.0E-3f));
            Assert.That(optResultPrimitive.CenterA.Z, Is.EqualTo(10).Within(1.0E-3f));

            Assert.That(optResultPrimitive.CenterB.X, Is.EqualTo(12).Within(1.0E-3f));
            Assert.That(optResultPrimitive.CenterB.Y, Is.EqualTo(12).Within(1.0E-3f));
            Assert.That(optResultPrimitive.CenterB.Z, Is.EqualTo(16).Within(1.0E-3f));

            Assert.That(optResultPrimitive.Normal.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Y, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.RadiusA, Is.EqualTo(3).Within(1.0E-3f));
            Assert.That(optResultPrimitive.RadiusB, Is.EqualTo(4).Within(1.0E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.X, Is.EqualTo(5).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Y, Is.EqualTo(3).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Z, Is.EqualTo(8).Within(1E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.X, Is.EqualTo(14).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Y, Is.EqualTo(12).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Z, Is.EqualTo(17).Within(1E-3f));
        });
    }

    [Test]
    public void ScaffoldOptimizerResult_GivenBaseWithXRotationAndOptimizedEccentricConePrimitive_ThenVerifyTransform()
    {
        // Arrange
        const float angle = (float)(Math.PI / 2.0);
        var rotation = Matrix4x4.CreateRotationX(angle);
        APrimitive basePrimitive = GenSomeInstanceMesh(rotation);
        APrimitive optimizedPrimitive = new EccentricCone(
            new Vector3(0, 0, -2),
            new Vector3(0, 0, 2),
            new Vector3(0, 0, 1),
            3,
            4,
            0,
            Color.Brown,
            new BoundingBox(new Vector3(-1, -1, -2), new Vector3(1, 1, 2))
        );

        // Act
        var optResult = new ScaffoldOptimizerResult(basePrimitive, optimizedPrimitive);
        var optResultPrimitive = optResult.Get() as EccentricCone;

        // Assert
        Assert.That(optResultPrimitive, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(optResultPrimitive.CenterA.X, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Y, Is.EqualTo(2).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.CenterB.X, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Y, Is.EqualTo(-2).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.Normal.X, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Y, Is.EqualTo(-1).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.RadiusA, Is.EqualTo(3).Within(1.0E-3f));
            Assert.That(optResultPrimitive.RadiusB, Is.EqualTo(4).Within(1.0E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.X, Is.EqualTo(-1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Y, Is.EqualTo(-2).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Z, Is.EqualTo(-1).Within(1E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Y, Is.EqualTo(2).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Z, Is.EqualTo(1).Within(1E-3f));
        });
    }

    [Test]
    public void ScaffoldOptimizerResult_GivenBaseWithYRotationAndOptimizedEccentricConePrimitive_ThenVerifyTransform()
    {
        // Arrange
        const float angle = (float)(Math.PI / 2.0);
        var rotation = Matrix4x4.CreateRotationY(angle);
        APrimitive basePrimitive = GenSomeInstanceMesh(rotation);
        APrimitive optimizedPrimitive = new EccentricCone(
            new Vector3(0, 0, -2),
            new Vector3(0, 0, 2),
            new Vector3(0, 0, 1),
            3,
            4,
            0,
            Color.Brown,
            new BoundingBox(new Vector3(-1, -1, -2), new Vector3(1, 1, 2))
        );

        // Act
        var optResult = new ScaffoldOptimizerResult(basePrimitive, optimizedPrimitive);
        var optResultPrimitive = optResult.Get() as EccentricCone;

        // Assert
        Assert.That(optResultPrimitive, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(optResultPrimitive.CenterA.X, Is.EqualTo(-2).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Y, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.CenterB.X, Is.EqualTo(2).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Y, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.Normal.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Y, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.RadiusA, Is.EqualTo(3).Within(1.0E-3f));
            Assert.That(optResultPrimitive.RadiusB, Is.EqualTo(4).Within(1.0E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.X, Is.EqualTo(-2).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Y, Is.EqualTo(-1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Z, Is.EqualTo(-1).Within(1E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.X, Is.EqualTo(2).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Y, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Z, Is.EqualTo(1).Within(1E-3f));
        });
    }

    [Test]
    public void ScaffoldOptimizerResult_GivenBaseWithZRotationAndOptimizedEccentricConePrimitive_ThenVerifyTransform()
    {
        // Arrange
        const float angle = (float)(Math.PI / 2.0);
        var rotation = Matrix4x4.CreateRotationZ(angle);
        APrimitive basePrimitive = GenSomeInstanceMesh(rotation);
        APrimitive optimizedPrimitive = new EccentricCone(
            new Vector3(-2, 0, 0),
            new Vector3(2, 0, 0),
            new Vector3(1, 0, 0),
            3,
            4,
            0,
            Color.Brown,
            new BoundingBox(new Vector3(-2, -1, -1), new Vector3(2, 1, 1))
        );

        // Act
        var optResult = new ScaffoldOptimizerResult(basePrimitive, optimizedPrimitive);
        var optResultPrimitive = optResult.Get() as EccentricCone;

        // Assert
        Assert.That(optResultPrimitive, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(optResultPrimitive.CenterA.X, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Y, Is.EqualTo(-2).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.CenterB.X, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Y, Is.EqualTo(2).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.Normal.X, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Y, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.RadiusA, Is.EqualTo(3).Within(1.0E-3f));
            Assert.That(optResultPrimitive.RadiusB, Is.EqualTo(4).Within(1.0E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.X, Is.EqualTo(-1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Y, Is.EqualTo(-2).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Z, Is.EqualTo(-1).Within(1E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Y, Is.EqualTo(2).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Z, Is.EqualTo(1).Within(1E-3f));
        });
    }

    [Test]
    public void ScaffoldOptimizerResult_GivenBaseWithUniformScaleAndOptimizedEccentricConePrimitive_ThenVerifyTransform()
    {
        // Arrange
        var scale = Matrix4x4.CreateScale(2.3f, 2.3f, 2.3f);
        APrimitive basePrimitive = GenSomeInstanceMesh(scale);
        APrimitive optimizedPrimitive = new EccentricCone(
            new Vector3(1, 2, 3),
            new Vector3(8, 10, 9),
            new Vector3(1, 0, 0),
            3,
            4,
            0,
            Color.Brown,
            new BoundingBox(new Vector3(1, 2, 3), new Vector3(8, 10, 9))
        );

        // Act
        var optResult = new ScaffoldOptimizerResult(basePrimitive, optimizedPrimitive);
        var optResultPrimitive = optResult.Get() as EccentricCone;

        // Assert
        Assert.That(optResultPrimitive, Is.Not.Null);

        Assert.Multiple(() =>
        {
            // Note that, for a uniform scale factor a, then all distances in the object
            // will scale with a. This is because all points (x_i, y_i, z_i) will scale to
            // (a*x_i, a*y_i, a*z_i) and any new length is given by
            //        L'=sqrt[(a*x_j - a*x_i)^2 + (a*y_j - a*y_i)^2 + ...]
            //          =a * sqrt[(x_j - x_i)^2 + (y_j - y_i)^2 + ...] = a * L.
            // Hence, the two radii, as well as the bounding box, must also scale by 2.3,
            // while the unit normal vector must remain unchanged by a scale operation.
            Assert.That(optResultPrimitive.CenterA.X, Is.EqualTo(2.3f).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Y, Is.EqualTo(4.6f).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterA.Z, Is.EqualTo(6.9f).Within(1E-3f));

            Assert.That(optResultPrimitive.CenterB.X, Is.EqualTo(18.4f).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Y, Is.EqualTo(23.0f).Within(1E-3f));
            Assert.That(optResultPrimitive.CenterB.Z, Is.EqualTo(20.7f).Within(1E-3f));

            Assert.That(optResultPrimitive.Normal.X, Is.EqualTo(1).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Y, Is.EqualTo(0).Within(1E-3f));
            Assert.That(optResultPrimitive.Normal.Z, Is.EqualTo(0).Within(1E-3f));

            Assert.That(optResultPrimitive.RadiusA, Is.EqualTo(6.9f).Within(1.0E-3f));
            Assert.That(optResultPrimitive.RadiusB, Is.EqualTo(9.2f).Within(1.0E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.X, Is.EqualTo(2.3f).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Y, Is.EqualTo(4.6f).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Min.Z, Is.EqualTo(6.9f).Within(1E-3f));

            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.X, Is.EqualTo(18.4f).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Y, Is.EqualTo(23.0f).Within(1E-3f));
            Assert.That(optResultPrimitive.AxisAlignedBoundingBox.Max.Z, Is.EqualTo(20.7f).Within(1E-3f));
        });
    }
}
