namespace CadRevealFbxProvider.Tests.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

using System.Numerics;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ReplacementScaffoldParts;

[TestFixture]
public class PrincipleComponentAnalyzerTests
{
    [Test]
    public void PcaResult3_WhenEnteringValues_ThenSameValuesAreReturnedButSortedByEigenValue()
    {
        // Arrange
        var v1 = new Vector3(1.2f, 5.6f, 0.4f);
        var v2 = new Vector3(7.8f, 10.3f, 8.4f);
        var v3 = new Vector3(0.8f, 3.4f, 0.2f);

        const float l1 = 3.5f;
        const float l2 = 1.2f;
        const float l3 = 2.8f;

        // Act
        var pcaResult = new PcaResult3(v1, v2, v3, l1, l2, l3);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(pcaResult.V(0).X, Is.EqualTo(v1.X).Within(1.0E-3f));
            Assert.That(pcaResult.V(0).Y, Is.EqualTo(v1.Y).Within(1.0E-3f));
            Assert.That(pcaResult.V(0).Z, Is.EqualTo(v1.Z).Within(1.0E-3f));

            Assert.That(pcaResult.V(1).X, Is.EqualTo(v3.X).Within(1.0E-3f));
            Assert.That(pcaResult.V(1).Y, Is.EqualTo(v3.Y).Within(1.0E-3f));
            Assert.That(pcaResult.V(1).Z, Is.EqualTo(v3.Z).Within(1.0E-3f));

            Assert.That(pcaResult.V(2).X, Is.EqualTo(v2.X).Within(1.0E-3f));
            Assert.That(pcaResult.V(2).Y, Is.EqualTo(v2.Y).Within(1.0E-3f));
            Assert.That(pcaResult.V(2).Z, Is.EqualTo(v2.Z).Within(1.0E-3f));

            Assert.That(pcaResult.Lambda(0), Is.EqualTo(l1).Within(1.0E-3f));
            Assert.That(pcaResult.Lambda(1), Is.EqualTo(l3).Within(1.0E-3f));
            Assert.That(pcaResult.Lambda(2), Is.EqualTo(l2).Within(1.0E-3f));
        });
    }

    [Test]
    public void Invoke_WhenOnlyTwoPoints_ThenPointsAreAlongFirstPrincipleComponent()
    {
        // Arrange
        var X = new List<Vector3> { new Vector3(0.3f, 8.3f, 3.4f), new Vector3(4.3f, 2.3f, 10.4f) };
        var dirVec = X[1] - X[0];

        // Act
        PcaResult3 pca = PrincipleComponentAnalyzer.Invoke(X);

        // Assert
        Assert.That(Vector3.Cross(pca.V(0), dirVec).Length(), Is.EqualTo(0).Within(1.0E-3f));
    }

    [Test]
    public void Invoke_WhenFlatEllipsoidPointCloud_ThenVectorsAreAlongMajorIntermediateAndMinorEllipsoidAxisAndAreOrdered()
    {
        // Arrange
        var u1 = new Vector3(1.2f, 3.4f, 8.2f);
        var u2 = Vector3.Cross(new Vector3(1, 0, 0), u1);
        var u3 = Vector3.Cross(u2, u1);

        u1 = Vector3.Normalize(u1); // Semi-major ellipsoid axis unit vector
        u2 = Vector3.Normalize(u2); // Intermediate ellipsoid axis unit vector
        u3 = Vector3.Normalize(u3); // Semi-minor ellipsoid axis unit vector

        const float r1 = 10.0f; // Semi-major ellipsoid axis radius
        const float r2 = 5.0f; // Intermediate ellipsoid axis radius
        const float r3 = 2.0f; // Semi-minor ellipsoid axis radius

        var pos = new Vector3(2.3f, 9.5f, 1.4f); // Ellipsoid position

        var rand = new Random();
        var X = new List<Vector3>();
        for (int i = 0; i < 10000; i++)
        {
            var theta = (float)rand.NextDouble() * 2.0 * Math.PI;
            var phi = (float)rand.NextDouble() * Math.PI;

            float R1 = r1 * (2.0f * (float)rand.NextDouble() - 1.0f);
            float R2 = r2 * (2.0f * (float)rand.NextDouble() - 1.0f);
            float R3 = r3 * (2.0f * (float)rand.NextDouble() - 1.0f);

            X.Add(
                pos
                    + (R1 * (float)Math.Cos(theta) * (float)Math.Sin(phi) * u1)
                    + (R2 * (float)Math.Sin(theta) * (float)Math.Sin(phi) * u2)
                    + (R3 * (float)Math.Cos(phi) * u3)
            );
        }

        // Act
        PcaResult3 pca = PrincipleComponentAnalyzer.Invoke(X);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Vector3.Dot(pca.V(0), pca.V(1)), Is.EqualTo(0).Within(1.0E-3f));
            Assert.That(Vector3.Dot(pca.V(0), pca.V(2)), Is.EqualTo(0).Within(1.0E-3f));
            Assert.That(Vector3.Dot(pca.V(1), pca.V(2)), Is.EqualTo(0).Within(1.0E-3f));

            Assert.That(Vector3.Cross(pca.V(0), u1).Length(), Is.EqualTo(0).Within(1.0E-1f));
            Assert.That(Vector3.Cross(pca.V(1), u2).Length(), Is.EqualTo(0).Within(1.0E-1f));
            Assert.That(Vector3.Cross(pca.V(2), u3).Length(), Is.EqualTo(0).Within(1.0E-1f));
        });
    }

    [Test]
    public void Invoke_WhenCylinderWallPointCloud_ThenPrincipleComponentIsAlongCylinderAxisAndOthersPerpendicularToThat()
    {
        // Arrange
        var u1 = new Vector3(1.2f, 3.4f, 8.2f);
        var u2 = Vector3.Cross(new Vector3(1, 0, 0), u1);
        var u3 = Vector3.Cross(u2, u1);

        u1 = Vector3.Normalize(u1); // Cylinder center axis unit vector
        u2 = Vector3.Normalize(u2); // Cylinder cap axis1 unit vector
        u3 = Vector3.Normalize(u3); // Cylinder cap axis2 unit vector

        const float r = 5.0f; // Cylinder radius
        var pos = new Vector3(2.3f, 9.5f, 1.4f); // Cylinder position

        var cylinderRingPositions = new List<float>() { 0, 5, 10, 15, 20 };

        var X = new List<Vector3>();
        foreach (float ringPosition in cylinderRingPositions)
        {
            for (float theta = 0; theta < 2.0 * Math.PI; theta += 0.1f)
            {
                Vector3 p = pos + ringPosition * u1 + r * (float)Math.Cos(theta) * u2 + r * (float)Math.Sin(theta) * u3;
                X.Add(p);
            }
        }

        // Act
        PcaResult3 pca = PrincipleComponentAnalyzer.Invoke(X);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Vector3.Dot(pca.V(0), pca.V(1)), Is.EqualTo(0).Within(1.0E-3f));
            Assert.That(Vector3.Dot(pca.V(0), pca.V(2)), Is.EqualTo(0).Within(1.0E-3f));
            Assert.That(Vector3.Dot(pca.V(1), pca.V(2)), Is.EqualTo(0).Within(1.0E-3f));

            Assert.That(Vector3.Cross(pca.V(0), u1).Length(), Is.EqualTo(0).Within(1.0E-1f));
        });
    }
}
