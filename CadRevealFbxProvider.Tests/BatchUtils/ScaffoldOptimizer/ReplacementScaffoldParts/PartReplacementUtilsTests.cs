namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

[TestFixture]
public class PartReplacementUtilsTests
{
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
    public void GivenStartAndEndPointAndRadius_WhenCreatingTessellatedCylinderPrimitiveWithAValidCylinderSpecified_ThenReturnANonNullMesh()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedCylinderPrimitive(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            1.0f,
            0,
            true
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(mesh.cylinder, Is.Not.Null);
            Assert.That(mesh.startCap, Is.Not.Null);
            Assert.That(mesh.endCap, Is.Not.Null);
        });
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCreatingTessellatedCylinderPrimitiveWithACylinderAlongXSpecified_ThenRadiusAndLengthOfCylinderIsValid()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedCylinderPrimitive(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            2.5f,
            0
        );

        // Assert
        Assert.That(mesh.cylinder, Is.Not.Null);
        foreach (var p in mesh.cylinder.Mesh.Vertices)
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
    public void GivenStartAndEndPointAndRadius_WhenCreatingTessellatedCylinderPrimitiveWithACylinderAlongYIsSpecified_ThenRadiusAndLengthOfCylinderIsValid()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedCylinderPrimitive(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            2.5f,
            0
        );

        // Assert
        Assert.That(mesh.cylinder, Is.Not.Null);
        foreach (var p in mesh.cylinder.Mesh.Vertices)
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
    public void GivenStartAndEndPointAndRadius_WhenCreatingTessellatedCylinderPrimitiveWithACylinderAlongZ_ThenRadiusAndLengthOfCylinderIsValid()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedCylinderPrimitive(
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            2.5f,
            0
        );

        // Assert
        Assert.That(mesh.cylinder, Is.Not.Null);
        foreach (var p in mesh.cylinder.Mesh.Vertices)
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
    public void GivenStartAndEndPointAndRadius_WhenCreatingTessellatedCylinderPrimitiveWithACylinderAlongXYZSpecified_ThenAllPointsAreWithinTheBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedCylinderPrimitive(
            new Vector3(1.0f, 2.0f, 3.0f),
            new Vector3(3.0f, 5.0f, 7.0f),
            2.5f,
            0
        );

        // Assert
        Assert.That(mesh.cylinder, Is.Not.Null);
        AssertThatAllPointsAreWithinTheBoundingBox(mesh.cylinder);
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCreatingTessellatedCylinderPrimitiveWithACylinderAlongXYZSpecified_ThenCapsAreCorrectlyPlacedWithCorrectRadiusAndNormal()
    {
        // Arrange
        // Act
        var startPoint = new Vector3(1.0f, 2.0f, 3.0f);
        var endPoint = new Vector3(3.0f, 5.0f, 7.0f);
        const float radius = 2.5f;
        var mesh = PartReplacementUtils.CreateTessellatedCylinderPrimitive(startPoint, endPoint, radius, 0, true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(mesh.startCap, Is.Not.Null);
            Assert.That(mesh.endCap, Is.Not.Null);
        });

        Vector3 startCapCenter = CalcCenter(mesh.startCap.Mesh);
        Vector3 endCapCenter = CalcCenter(mesh.endCap.Mesh);

        Assert.Multiple(() =>
        {
            Assert.That(startCapCenter.X, Is.EqualTo(startPoint.X).Within(0.1));
            Assert.That(startCapCenter.Y, Is.EqualTo(startPoint.Y).Within(0.1));
            Assert.That(startCapCenter.Z, Is.EqualTo(startPoint.Z).Within(0.1));
            Assert.That(endCapCenter.X, Is.EqualTo(endPoint.X).Within(0.1));
            Assert.That(endCapCenter.Y, Is.EqualTo(endPoint.Y).Within(0.1));
            Assert.That(endCapCenter.Z, Is.EqualTo(endPoint.Z).Within(0.1));
        });

        foreach (var p in mesh.startCap.Mesh.Vertices)
        {
            var d = p - startPoint;
            if (d.Length() < 0.01)
                continue; // Exclude vertices at the circle center
            Assert.That(d.Length(), Is.EqualTo(radius).Within(0.1));
        }
        foreach (var p in mesh.endCap.Mesh.Vertices)
        {
            var d = p - endPoint;
            if (d.Length() < 0.01)
                continue; // Exclude vertices at the circle center
            Assert.That(d.Length(), Is.EqualTo(radius).Within(0.1));
        }

        Vector3 startCapNormal = CalcAvgNormal(mesh.startCap.Mesh, startCapCenter);
        Vector3 endCapNormal = CalcAvgNormal(mesh.endCap.Mesh, endCapCenter);
        Vector3 normal = Vector3.Normalize(endPoint - startPoint);

        Assert.Multiple(() =>
        {
            Assert.That(startCapNormal.X, Is.EqualTo(normal.X).Within(0.1).Or.EqualTo(-normal.X).Within(0.1));
            Assert.That(startCapNormal.Y, Is.EqualTo(normal.Y).Within(0.1).Or.EqualTo(-normal.Y).Within(0.1));
            Assert.That(startCapNormal.Z, Is.EqualTo(normal.Z).Within(0.1).Or.EqualTo(-normal.Z).Within(0.1));
            Assert.That(endCapNormal.X, Is.EqualTo(normal.X).Within(0.1).Or.EqualTo(-normal.X).Within(0.1));
            Assert.That(endCapNormal.Y, Is.EqualTo(normal.Y).Within(0.1).Or.EqualTo(-normal.Y).Within(0.1));
            Assert.That(endCapNormal.Z, Is.EqualTo(normal.Z).Within(0.1).Or.EqualTo(-normal.Z).Within(0.1));
        });

        return;
        Vector3 CalcCenter(Mesh m)
        {
            var c = new Vector3();
            foreach (var p in m.Vertices)
            {
                if ((p - c).Length() < 0.01)
                    continue; // Exclude vertices at the circle center
                c += p;
            }
            return c / m.Vertices.Length;
        }
        Vector3 CalcAvgNormal(Mesh m, Vector3 c)
        {
            var n = new Vector3();
            for (int i = 0; i < m.Vertices.Length - 1; i++)
            {
                var p1 = m.Vertices[i];
                if ((p1 - c).Length() < 0.01)
                    continue; // Exclude vertices at the circle center
                var p2 = m.Vertices[i + 1];
                if ((p2 - c).Length() < 0.01)
                    continue; // Exclude vertices at the circle center
                n += Vector3.Cross(p1 - c, p2 - c);
            }
            return Vector3.Normalize(n);
        }
    }

    [Test]
    public void GivenStartAndEndPointAndRadius_WhenCreatingCylinderPrimitiveWithACylinderAlongXYZSpecified_ThenAllPointsAreWithinTheBoundingBox()
    {
        // Arrange
        // Act
        var primitive = PartReplacementUtils.CreateCylinderPrimitive(
            new Vector3(1.0f, 2.0f, 3.0f),
            new Vector3(3.0f, 5.0f, 7.0f),
            2.5f,
            0
        );

        // Assert
        Assert.That(primitive.cylinder, Is.Not.Null);
        var cylinder = EccentricConeTessellator.Tessellate(primitive.cylinder);
        Assert.That(cylinder, Is.Not.Null);
        AssertThatAllPointsAreWithinTheBoundingBox(cylinder);
    }

    [Test]
    [TestCase(-1.0f, 0.0f, 0.0f, 5.0f, 0.0f, 0.0f, 5.4f, 0.43f)]
    [TestCase(0.0f, -1.0f, 0.0f, 0.0f, 5.0f, 0.0f, 5.6f, 0.53f)]
    public void GivenCoordinatesWithinACylinderWithLengthMuchGreaterThanDiameter_WhenCreatingCylinderPrimitiveFromMesh_ThenReturnSmallestCylinderSurroundingAllPoints(
        float xA,
        float yA,
        float zA,
        float xB,
        float yB,
        float zB,
        float L,
        float R
    )
    {
        // Arrange
        // Generate points within a cylinder, which central axis is along (xA, yA, zA) to (xB, yB, zB), but stretches L meters from (xA, yA, zA), with a radius R
        var vertices = new List<Vector3>();
        var indices = new List<uint>();
        var a = new Vector3(xA, yA, zA);
        var b = new Vector3(xB, yB, zB);
        var u = Vector3.Normalize(b - a);
        var v = Vector3.Normalize(Vector3.Cross(FindNonParallelVector(u), u));
        var w = Vector3.Normalize(Vector3.Cross(u, v));
        uint index = 0;
        const int N = 5;
        for (int i = 0; i < N; i++)
        {
            float cylPos = (float)i * L / (float)(N - 1);
            for (float angle = 0.0f; angle < 2.0 * Math.PI; angle += 0.3f)
            {
                var r = a + (u * cylPos) + (v * R * (float)Math.Cos(angle)) + (w * R * (float)Math.Sin(angle));
                vertices.Add(r);
                indices.Add(index++);
            }
        }

        var cylinderMesh = new Mesh(vertices.ToArray(), indices.ToArray(), 1.0E-6f);

        // Act
        EccentricCone? mesh = cylinderMesh.ToCylinderPrimitive(0).cylinder;

        // Assert
        Assert.That(mesh, Is.Not.Null);
        Vector3 cA = mesh.CenterA;
        Vector3 cB = mesh.CenterB;
        float lenCyl = (cB - cA).Length();

        Assert.Multiple(() =>
        {
            Assert.That(mesh.RadiusB, Is.EqualTo(mesh.RadiusA));
            Assert.That(mesh.RadiusA, Is.EqualTo(R).Within(1.0E-2f));

            Assert.That(lenCyl, Is.EqualTo(L).Within(1.0E-2f));

            Assert.That(cA.X, Is.EqualTo(a.X).Within(1.0E-2f));
            Assert.That(cA.Y, Is.EqualTo(a.Y).Within(1.0E-2f));
            Assert.That(cA.Z, Is.EqualTo(a.Z).Within(1.0E-2f));

            Assert.That(cB.X, Is.EqualTo((a + u * L).X).Within(1.0E-2f));
            Assert.That(cB.Y, Is.EqualTo((a + u * L).Y).Within(1.0E-2f));
            Assert.That(cB.Z, Is.EqualTo((a + u * L).Z).Within(1.0E-2f));
        });

        var U = Vector3.Normalize(cB - cA);
        Assert.That(U.X, Is.EqualTo(u.X).Within(1.0E-3f));
        Assert.That(U.Y, Is.EqualTo(u.Y).Within(1.0E-3f));
        Assert.That(U.Z, Is.EqualTo(u.Z).Within(1.0E-3f));

        return;
        Vector3 FindNonParallelVector(Vector3 vec)
        {
            var ux = new Vector3(1.0f, 0.0f, 0.0f);
            var uy = new Vector3(0.0f, 1.0f, 0.0f);
            var uz = new Vector3(0.0f, 0.0f, 1.0f);

            if (Math.Abs(Vector3.Dot(vec, ux) + 1.0) > 1.0E-3 && Math.Abs(1.0f - Vector3.Dot(vec, ux)) > 1.0E-3)
                return ux;
            if (Math.Abs(Vector3.Dot(vec, uy) + 1.0) > 1.0E-3 && Math.Abs(1.0f - Vector3.Dot(vec, uy)) > 1.0E-3)
                return uy;
            if (Math.Abs(Vector3.Dot(vec, uz) + 1.0) > 1.0E-3 && Math.Abs(1.0f - Vector3.Dot(vec, uz)) > 1.0E-3)
                return uz;
            return ux;
        }
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongX_ThenReturnANonNullMesh()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            0.05f,
            0.1f,
            0
        );

        // Assert
        Assert.That(mesh, Is.Not.Null);
    }

    [Test]
    public void GivenStartAndEndPointAndThicknessAndHeight1_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Symmetric around X=0, startPoint = min, axis aligned
            new Vector3(-1, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight1_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongY_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Symmetric around Y=0, startPoint = min, axis aligned
            new Vector3(0, -1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight1_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongZ_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Symmetric around Z=0, startPoint = min, axis aligned
            new Vector3(0, 0, -1),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 0),
            0.05f,
            0.1f,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight2_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Symmetric around X=0, startPoint = min, axis aligned, but smaller scale than 1
            new Vector3(-0.8f, 0, 0),
            new Vector3(0.8f, 0, 0),
            new Vector3(0, 0, 1),
            0.02f,
            0.08f,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight3_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Symmetric around X=0, startPoint = max, axis aligned, same size as 1
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight4_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Asymmetric around X=0, startPoint = min, axis aligned, half-length of 1
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight5_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongX_ThenAllPointsAreWithinItsBoundingBox()
    {
        // Arrange
        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Asymmetric around X=0, startPoint = max, axis aligned, half-length of 1
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 1),
            0.05f,
            0.1f,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight6_WhenCreatingTessellatedBoxPrimitiveAlongSomeAxis_ThenAllPointsAreWithinItsCylindricalBoundingBox()
    {
        // Arrange
        var startPoint = new Vector3(3, 5, 4);
        var endPoint = new Vector3(1, 3, 2);
        var surfaceDirGuide = new Vector3(0, 0, 1);
        const float height = 0.1f;
        const float thickness = 0.05f;

        // Act
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Asymmetric around X=0, startPoint = max, axis aligned, half-length of 1
            startPoint,
            endPoint,
            surfaceDirGuide,
            thickness,
            height,
            0
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
    public void GivenStartAndEndPointAndThicknessAndHeight6_WhenCreatingTessellatedBoxPrimitiveWithBoxPartAlongSomeAxis_ThenAllPointsAreWithinItsBoundingBox(
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
        var mesh = PartReplacementUtils.CreateTessellatedBoxPrimitive(
            // Asymmetric around X=0, startPoint = max, axis aligned, half the length of 1
            startPoint,
            endPoint,
            surfaceDirGuide,
            thickness,
            height,
            0
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

    [Test]
    public void GivenMeshListWhereOneObjectHasLargestVolume_WhenSearchingForMeshWithLargestBoundingBox_ThenReturnMeshWithLargestBoundingBoxVolume()
    {
        // Arrange
        var meshXs = PartReplacementUtils
            .CreateTessellatedCylinderPrimitive(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), 1.0f, 0)
            .cylinder;
        var meshS = PartReplacementUtils
            .CreateTessellatedCylinderPrimitive(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.1f, 0.0f, 0.0f), 1.0f, 0)
            .cylinder;
        var meshM = PartReplacementUtils
            .CreateTessellatedCylinderPrimitive(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.1f, 0.0f, 0.0f), 1.1f, 0)
            .cylinder;
        var meshL = PartReplacementUtils
            .CreateTessellatedCylinderPrimitive(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.2f, 0.0f, 0.0f), 1.1f, 0)
            .cylinder;
        var meshXl = PartReplacementUtils
            .CreateTessellatedCylinderPrimitive(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.2f, 0.0f, 0.0f), 1.2f, 0)
            .cylinder;
        var meshList1 = new List<Mesh?> { meshXs?.Mesh, meshS?.Mesh, meshM?.Mesh, meshL?.Mesh, meshXl?.Mesh };
        var meshList2 = new List<Mesh?> { meshXs?.Mesh, meshS?.Mesh, meshXl?.Mesh, meshM?.Mesh, meshL?.Mesh };
        var meshList3 = new List<Mesh?> { meshXl?.Mesh, meshXs?.Mesh, meshS?.Mesh, meshM?.Mesh, meshL?.Mesh };
        var meshList4 = new List<Mesh?>
        {
            meshXs?.Mesh,
            meshXl?.Mesh,
            meshS?.Mesh,
            meshXl?.Mesh,
            meshM?.Mesh,
            meshL?.Mesh
        };

        // Act
        Mesh? mesh1 = PartReplacementUtils.FindMeshWithLargestBoundingBoxVolume(meshList1);
        Mesh? mesh2 = PartReplacementUtils.FindMeshWithLargestBoundingBoxVolume(meshList2);
        Mesh? mesh3 = PartReplacementUtils.FindMeshWithLargestBoundingBoxVolume(meshList3);
        Mesh? mesh4 = PartReplacementUtils.FindMeshWithLargestBoundingBoxVolume(meshList4);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(mesh1, Is.SameAs(meshXl?.Mesh));
            Assert.That(mesh2, Is.SameAs(meshXl?.Mesh));
            Assert.That(mesh3, Is.SameAs(meshXl?.Mesh));
            Assert.That(mesh4, Is.SameAs(meshXl?.Mesh));
        });
    }

    [Test]
    public void GivenA2by3by4CenteredMeshWhereTransformIsIdentity_WhenCreatingBoxPrimitiveFromMesh_ThenProduceABoxMatrixThatScalesByTheSame()
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
        Box box = instanceMesh.TemplateMesh.ToBoxPrimitive(Matrix4x4.Identity, 0, new Color());

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
    public void GivenA2by3by4NotCenteredMeshWhereTransformIsIdentity_WhenCreatingBoxPrimitiveFromMesh_ThenProduceABoxMatrixThatScalesByTheSameButTranslated()
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
        Box box = instanceMesh.TemplateMesh.ToBoxPrimitive(Matrix4x4.Identity, 0, new Color());

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
    public void GivenA2by3by4CenteredMeshAndTransformWithTranslation_WhenCreatingBoxPrimitiveFromMesh_ThenProduceABoxMatrixThatScalesByTheSameButTranslated()
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
        Box box = instanceMesh.TemplateMesh.ToBoxPrimitive(transform, 0, new Color());

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
    public void GivenA2by3by4CenteredMeshWhereTransformIsScaled_WhenCreatingBoxPrimitiveFromMesh_ThenProduceABoxMatrixThatScalesByTheSameAndThenByScaleInTransform()
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
        Box box = instanceMesh.TemplateMesh.ToBoxPrimitive(transform, 0, new Color());

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
    public void GivenA1by1by1CenteredMeshWhereTransformContainsRotZ_WhenCreatingBoxPrimitiveFromMesh_ThenProduceABoxMatrixThatRotateAccordingly()
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
        Box box = instanceMesh.TemplateMesh.ToBoxPrimitive(transform, 0, new Color());

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
    public void GivenA1by1by1CenteredMeshWhereTransformContainsRotXYZ_WhenCreatingBoxPrimitiveFromMesh_ThenProduceABoxMatrixThatRotateAccordingly()
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
        Box box = instanceMesh.TemplateMesh.ToBoxPrimitive(transform, 0, new Color());

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
