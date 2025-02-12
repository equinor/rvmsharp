namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

[TestFixture]
public class PartReplacementUtilsTests
{
    private static (bool inwardNormal, int indicesIncrement) DoesNormalPointInwardsIntoCylinder(
        int indexIntoIndices,
        uint[] indices,
        Vector3[] vertices
    )
    {
        uint i1 = indices[indexIntoIndices];
        uint i2 = indices[indexIntoIndices + 1];
        uint i3 = indices[indexIntoIndices + 2];
        Vector3 r1 = vertices[i1];
        Vector3 r2 = vertices[i2];
        Vector3 r3 = vertices[i3];
        Vector3 u = r1 - r2;
        Vector3 v = r3 - r2;
        Vector3 n = Vector3.Normalize(Vector3.Cross(u, v));
        float r2Len = r2.Length();
        float testLength = (r2 - 0.5f * r2Len * n).Length();
        bool normalPointsInwards = (testLength < r2Len) ? true : false;

        return (normalPointsInwards, 2);
    }

    private static void AssertThatAllPointsAreWithinTheBoundingBox(TriangleMesh triangleMesh)
    {
        BoundingBox bbox1 = triangleMesh.AxisAlignedBoundingBox;
        BoundingBox bbox2 = triangleMesh.Mesh.CalculateAxisAlignedBoundingBox();
        foreach (var p in triangleMesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(bbox1.Min.X, bbox1.Max.X));
                Assert.That(p.Y, Is.InRange(bbox1.Min.Y, bbox1.Max.Y));
                Assert.That(p.Z, Is.InRange(bbox1.Min.Z, bbox1.Max.Z));

                Assert.That(p.X, Is.InRange(bbox2.Min.X, bbox2.Max.X));
                Assert.That(p.Y, Is.InRange(bbox2.Min.Y, bbox2.Max.Y));
                Assert.That(p.Z, Is.InRange(bbox2.Min.Z, bbox2.Max.Z));
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCallingTessellateCylinderPartWithAValidCylinderSpecified_ThenReturnANonNullMesh()
    {
        // Arrange
        // Act
        var meshes = PartReplacementUtils.TessellateCylinderPart(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            1.0f
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(meshes.front, Is.Not.Null);
            Assert.That(meshes.back, Is.Not.Null);
        });
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCallingTessellateCylinderPartWithACylinderAlongXSpecified_ThenRadiusAndLengthOfFrontCylinderIsValid()
    {
        // Arrange
        // Act
        var meshes = PartReplacementUtils.TessellateCylinderPart(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            2.5f
        );

        // Assert
        Assert.That(meshes.front, Is.Not.Null);
        foreach (var p in meshes.front.Mesh.Vertices)
        {
            var r = new Vector2(p.Y, p.Z);

            Assert.Multiple(() =>
            {
                // Assert that all points are equidistant from x-axis
                Assert.That(r.Length(), Is.EqualTo(2.5).Within(0.01));

                // Assert that all x-values are within the cylinder length
                Assert.That(p.X, Is.LessThanOrEqualTo(1.0));
                Assert.That(p.X, Is.GreaterThanOrEqualTo(0.0));
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCallingTessellateCylinderPartWithACylinderAlongYIsSpecified_ThenRadiusAndLengthOfFrontCylinderIsValid()
    {
        // Arrange
        // Act
        var meshes = PartReplacementUtils.TessellateCylinderPart(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            2.5f
        );

        // Assert
        Assert.That(meshes.front, Is.Not.Null);
        foreach (var p in meshes.front.Mesh.Vertices)
        {
            var r = new Vector2(p.X, p.Z);

            Assert.Multiple(() =>
            {
                // Assert that all points are equidistant from x-axis
                Assert.That(r.Length(), Is.EqualTo(2.5).Within(0.01));

                // Assert that all x-values are within the cylinder length
                Assert.That(p.Y, Is.LessThanOrEqualTo(1.0));
                Assert.That(p.Y, Is.GreaterThanOrEqualTo(0.0));
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCallingTessellateCylinderPartWithACylinderAlongZ_ThenRadiusAndLengthOfFrontCylinderIsValid()
    {
        // Arrange
        // Act
        var meshes = PartReplacementUtils.TessellateCylinderPart(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            2.5f
        );

        // Assert
        Assert.That(meshes.front, Is.Not.Null);
        foreach (var p in meshes.front.Mesh.Vertices)
        {
            var r = new Vector2(p.X, p.Y);

            Assert.Multiple(() =>
            {
                // Assert that all points are equidistant from x-axis
                Assert.That(r.Length(), Is.EqualTo(2.5).Within(0.01));

                // Assert that all x-values are within the cylinder length
                Assert.That(p.Z, Is.LessThanOrEqualTo(1.0));
                Assert.That(p.Z, Is.GreaterThanOrEqualTo(0.0));
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCallingTessellateCylinderPartWithACylinderAlongXSpecified_ThenRadiusAndLengthOfBackCylinderIsValid()
    {
        // Arrange
        // Act
        var meshes = PartReplacementUtils.TessellateCylinderPart(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            2.5f
        );

        // Assert
        Assert.That(meshes.back, Is.Not.Null);
        foreach (var p in meshes.back.Mesh.Vertices)
        {
            var r = new Vector2(p.Y, p.Z);

            Assert.Multiple(() =>
            {
                // Assert that all points are equidistant from x-axis
                Assert.That(r.Length(), Is.EqualTo(2.5).Within(0.01));

                // Assert that all x-values are within the cylinder length
                Assert.That(p.X, Is.LessThanOrEqualTo(1.0));
                Assert.That(p.X, Is.GreaterThanOrEqualTo(0.0));
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCallingTessellateCylinderPartWithACylinderAlongXSpecified_ThenNormalsOfBackAndFrontSurfacesAreOpposite()
    {
        // Arrange
        // Act
        var meshes = PartReplacementUtils.TessellateCylinderPart(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            2.5f
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(meshes.back, Is.Not.Null);
            Assert.That(meshes.front, Is.Not.Null);
        });

        bool firstNormalForBackPointsInwards = false;
        bool allNormalsForBackInSameDirection = true;
        for (int i = 0; i < meshes.back.Mesh.Indices.Length; i++)
        {
            var normalPointsInwards = DoesNormalPointInwardsIntoCylinder(
                i,
                meshes.back.Mesh.Indices,
                meshes.back.Mesh.Vertices
            );

            if (i == 0)
                firstNormalForBackPointsInwards = normalPointsInwards.inwardNormal;
            if (normalPointsInwards.inwardNormal != firstNormalForBackPointsInwards)
                allNormalsForBackInSameDirection = false;
            i += normalPointsInwards.indicesIncrement;
        }
        Assert.That(allNormalsForBackInSameDirection, Is.True);

        bool firstNormalForFrontPointsInwards = false;
        bool allNormalsForFrontInSameDirection = true;
        for (int i = 0; i < meshes.front.Mesh.Indices.Length; i++)
        {
            var normalPointsInwards = DoesNormalPointInwardsIntoCylinder(
                i,
                meshes.front.Mesh.Indices,
                meshes.front.Mesh.Vertices
            );

            if (i == 0)
                firstNormalForFrontPointsInwards = normalPointsInwards.inwardNormal;
            if (normalPointsInwards.inwardNormal != firstNormalForFrontPointsInwards)
                allNormalsForFrontInSameDirection = false;
            i += normalPointsInwards.indicesIncrement;
        }
        Assert.That(allNormalsForFrontInSameDirection, Is.True);

        Assert.That(firstNormalForBackPointsInwards, Is.Not.EqualTo(firstNormalForFrontPointsInwards));
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCallingTessellateCylinderPartWithACylinderAlongXYZSpecified_ThenAllPointsAreWithinTheBoundingBox()
    {
        // Arrange
        // Act
        var meshes = PartReplacementUtils.TessellateCylinderPart(
            new Vector3(1.0f, 2.0f, 3.0f),
            new Vector3(3.0f, 5.0f, 7.0f),
            2.5f
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(meshes.back, Is.Not.Null);
            Assert.That(meshes.front, Is.Not.Null);
        });
        AssertThatAllPointsAreWithinTheBoundingBox(meshes.back);
        AssertThatAllPointsAreWithinTheBoundingBox(meshes.front);
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight_WhenCallingTessellateBoxPartWithBoxPartAlongX_ThenReturnANonNullMesh()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            0.05f,
            0.1f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight1_WhenCallingTessellateBoxPartWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Symmetric around X=0, startPoint = min, axis aligned
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
        foreach (var p in mesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(-1.01f, 1.01f)); // Length
                Assert.That(p.Y, Is.InRange(-0.051f, 0.051f)); // Height
                Assert.That(p.Z, Is.InRange(-0.026f, 0.026f)); // Thickness
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight1_WhenCallingTessellateBoxPartWithBoxPartAlongY_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Symmetric around Y=0, startPoint = min, axis aligned
            new Vector3(0, -1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
        foreach (var p in mesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(-0.051f, 0.051f)); // Height
                Assert.That(p.Y, Is.InRange(-1.01f, 1.01f)); // Length
                Assert.That(p.Z, Is.InRange(-0.026f, 0.026f)); // Thickness
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight1_WhenCallingTessellateBoxPartWithBoxPartAlongZ_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Symmetric around Z=0, startPoint = min, axis aligned
            new Vector3(0, 0, -1),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            0.05f,
            0.1f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
        foreach (var p in mesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(-0.026f, 0.026f)); // Thickness
                Assert.That(p.Y, Is.InRange(-0.051f, 0.051f)); // Height
                Assert.That(p.Z, Is.InRange(-1.01f, 1.01f)); // Length
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight2_WhenCallingTessellateBoxPartWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Symmetric around X=0, startPoint = min, axis aligned, but smaller scale than 1
            new Vector3(-0.8f, 0, 0),
            new Vector3(0.8f, 0, 0),
            new Vector3(0, 0, 1),
            0.02f,
            0.08f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
        foreach (var p in mesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(-0.81f, 0.81f)); // Length
                Assert.That(p.Y, Is.InRange(-0.041f, 0.041f)); // Height
                Assert.That(p.Z, Is.InRange(-0.011f, 0.011f)); // Thickness
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight3_WhenCallingTessellateBoxPartWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Symmetric around X=0, startPoint = max, axis aligned, same size as 1
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
        foreach (var p in mesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(-1.01f, 1.01f)); // Length
                Assert.That(p.Y, Is.InRange(-0.051f, 0.051f)); // Height
                Assert.That(p.Z, Is.InRange(-0.026f, 0.026f)); // Thickness
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight4_WhenCallingTessellateBoxPartWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Asymmetric around X=0, startPoint = min, axis aligned, half length of 1
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
        foreach (var p in mesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(-0.01f, 1.01f)); // Length
                Assert.That(p.Y, Is.InRange(-0.051f, 0.051f)); // Height
                Assert.That(p.Z, Is.InRange(-0.026f, 0.026f)); // Thickness
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight5_WhenCallingTessellateBoxPartWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Asymmetric around X=0, startPoint = max, axis aligned, half length of 1
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
        foreach (var p in mesh.Mesh.Vertices)
        {
            Assert.Multiple(() =>
            {
                Assert.That(p.X, Is.InRange(-0.01f, 1.01f)); // Length
                Assert.That(p.Y, Is.InRange(-0.051f, 0.051f)); // Height
                Assert.That(p.Z, Is.InRange(-0.026f, 0.026f)); // Thickness
            });
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight6_WhenCallingTessellateBoxPartWithBoxPartAlongSomeAxis_ThenAllPointsAreWithinItsCylindricalBoundingBox()
    {
        // Arrange
        var startPoint = new Vector3(3, 5, 4);
        var endPoint = new Vector3(1, 3, 2);
        var surfaceDirGuide = new Vector3(0, 0, 1);
        const float height = 0.1f;
        const float thickness = 0.05f;

        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Asymmetric around X=0, startPoint = max, axis aligned, half length of 1
            startPoint,
            endPoint,
            surfaceDirGuide,
            thickness,
            height
        );

        // Assert
        // We create three unit axis, u, v, and w, along the length and the end surface, respectively.
        // We let alpha, beta, and gamma be the distances along u, v, and w, respectively.
        // Note! We assume that rotation of the box is done about the x cross u axis => x cross u is
        // parallel to the endpoint surfaces. We aim to check that the beam box is within a cylinder
        // along the beam length for a radius rMax
        var u = endPoint - startPoint;
        var v = Vector3.Cross(new Vector3(1, 0, 0), u);
        var w = Vector3.Cross(u, v);
        u /= u.Length();
        v /= v.Length();
        w /= w.Length();

        float rMax = height * float.Sqrt(2.0f);
        (float alphaMin, float alphaMax) = ReorganizeToMinMax(Vector3.Dot(startPoint, u), Vector3.Dot(endPoint, u));
        (float betaMin, float betaMax) = ReorganizeToMinMax(
            Vector3.Dot(startPoint - v * rMax, v),
            Vector3.Dot(startPoint + v * rMax, v)
        );
        (float gammaMin, float gammaMax) = ReorganizeToMinMax(
            Vector3.Dot(startPoint - w * rMax, w),
            Vector3.Dot(startPoint + w * rMax, w)
        );

        Assert.That(mesh, Is.Not.Null);
        const float epsilon = 0.001f;
        foreach (var p in mesh.Mesh.Vertices)
        {
            var alpha = Vector3.Dot(p, u);
            var beta = Vector3.Dot(p, v);
            var gamma = Vector3.Dot(p, w);
            Assert.Multiple(() =>
            {
                Assert.That(alpha, Is.InRange(alphaMin - epsilon, alphaMax + epsilon)); // Length
                Assert.That(beta, Is.InRange(betaMin - epsilon, betaMax + epsilon)); // Along end surface
                Assert.That(gamma, Is.InRange(gammaMin - epsilon, gammaMax + epsilon)); // Along end surface
            });
        }

        return;

        (float, float) ReorganizeToMinMax(float a, float b) => (Math.Min(a, b), Math.Max(a, b));
    }

    [Test]
    [TestCase(3, 5, 4, 1, 3, 2, 0, 2, -2)] // First
    [TestCase(1, 3, 2, 3, 5, 4, 0, 2, -2)] // Reverse of first
    [TestCase(-7, -5, -6, -9, -7, -8, 0, 2, -2)] // -10 from first
    [TestCase(3, 5, 4, 6, 3, 7, 0, -3, -2)] // Move second vector in first
    public void GivenStartAndEndPointAndThicknessAndHeight6_WhenCallingTessellateBoxPartWithBoxPartAlongSomeAxis_ThenAllPointsAreWithinItsBoundingBox(
        float startX,
        float startY,
        float startZ,
        float endX,
        float endY,
        float endZ,
        float guideX,
        float guideY,
        float guideZ
    )
    {
        // Arrange
        var startPoint = new Vector3(startX, startY, startZ);
        var endPoint = new Vector3(endX, endY, endZ);
        var surfaceDirGuide = new Vector3(guideX, guideY, guideZ);
        const float height = 0.1f;
        const float thickness = 0.05f;

        // Act
        var mesh = PartReplacementUtils.TessellateBoxPart(
            // Asymmetric around X=0, startPoint = max, axis aligned, half the length of 1
            startPoint,
            endPoint,
            surfaceDirGuide,
            thickness,
            height
        );

        // Assert
        // We create three unit axis, u, v, and w, along the length and the end surface, respectively.
        // We let alpha, beta, and gamma be the distances along u, v, and w, respectively.
        // Note! We assume that rotation of the box is done about the x cross u axis => v = x cross u is
        // parallel to the endpoint surfaces, where u is along the beam length. Now we set w = u cross v.
        // v will be perpendicular to x, but not y in general. To ease the test, we want to rotate the
        // beam around its axis, so that its surface normal is such that it is parallel to v. In that case
        // the beam is axis-aligned with the uvw-coordinate system. However, to achieve this we need
        // to set surfaceDirGuide = v before calling TessellateBoxPart (i.e., we need to a priori calculate v,
        // based on startPoint and endPoint, which was done here by debugging the test with a random and
        // vector in surfaceDirGuide and then reading off the value).
        var u = endPoint - startPoint;
        var v = Vector3.Cross(new Vector3(1, 0, 0), u);
        var w = Vector3.Cross(u, v);
        float length = u.Length();
        u /= u.Length();
        v /= v.Length();
        w /= w.Length();

        (float alphaMin, float alphaMax) = (0, length);
        (float betaMin, float betaMax) = (-thickness / 2.0f, thickness / 2.0f);
        (float gammaMin, float gammaMax) = (-height / 2.0f, height / 2.0f);

        Assert.That(mesh, Is.Not.Null);
        const float epsilon = 0.001f;
        foreach (var p in mesh.Mesh.Vertices)
        {
            var pLocal = p - startPoint;
            var alpha = Vector3.Dot(pLocal, u);
            var beta = Vector3.Dot(pLocal, v);
            var gamma = Vector3.Dot(pLocal, w);
            Assert.Multiple(() =>
            {
                Assert.That(alpha, Is.InRange(alphaMin - epsilon, alphaMax + epsilon)); // Length
                Assert.That(beta, Is.InRange(betaMin - epsilon, betaMax + epsilon)); // Along end surface
                Assert.That(gamma, Is.InRange(gammaMin - epsilon, gammaMax + epsilon)); // Along end surface
            });
        }
    }
}
