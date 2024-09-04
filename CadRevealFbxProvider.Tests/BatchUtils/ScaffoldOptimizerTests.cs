namespace CadRevealFbxProvider.Tests.BatchUtils;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using CadRevealFbxProvider.BatchUtils;
using ScaffoldPartOptimizers;

public class ScaffoldOptimizerTests
{
    private static Mesh CreateMesh(
        float x1,
        float y1,
        float z1,
        float x2,
        float y2,
        float z2,
        float x3,
        float y3,
        float z3,
        uint i1,
        uint i2,
        uint i3
    )
    {
        return new Mesh(
            [new Vector3(x1, y1, z1), new Vector3(x2, y2, z2), new Vector3(x3, y3, z3)],
            [i1, i2, i3],
            0.0f
        );
    }

    private static (CadRevealNode node, List<Mesh?> nodeMeshes, List<BoundingBox> boundingBoxes) CreateCadRevealNode(
        string partName
    )
    {
        var mesh1 = CreateMesh(5, 5, 5, 7, 7, 7, 3, 3, 3, 0, 1, 2);
        var mesh2 = CreateMesh(1, 2, 3, 6, 7, 8, 9, 10, 11, 0, 1, 2);
        var mesh3 = CreateMesh(6, 5, 4, 1, 3, 2, 14, 15, 16, 0, 1, 2);

        var bbox1 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var bbox2 = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));

        var node = new CadRevealNode
        {
            TreeIndex = 0,
            Name = partName,
            Parent = null,
            Geometries =
            [
                new InstancedMesh(1, mesh1, Matrix4x4.Identity, 1, Color.Black, bbox1),
                new InstancedMesh(2, mesh2, Matrix4x4.Identity, 2, Color.Black, bbox1),
                new TriangleMesh(mesh3, 3, Color.Black, bbox1),
                new TriangleMesh(mesh1, 4, Color.Black, bbox1),
                new Circle(Matrix4x4.Identity, new Vector3(1.0f, 8.0f, 2.0f), 5, Color.Black, bbox2)
            ]
        };

        return (node, [mesh1, mesh2, mesh3, mesh1, null], [bbox1, bbox1, bbox1, bbox1, bbox2]);
    }

    private static void CheckMeshList(
        Vector3[] resultVertices,
        List<Vector3> truthVertices,
        uint[] resultIndices,
        List<uint> truthIndices
    )
    {
        const float tolerance = 1.0E-6f;
        Assert.Multiple(() =>
        {
            Assert.That(resultVertices, Has.Length.EqualTo(truthVertices.Count));
            Assert.That(resultIndices, Has.Length.EqualTo(truthIndices.Count));
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                resultVertices,
                Is.EqualTo(truthVertices).Using<Vector3>((a, b) => a.EqualsWithinTolerance(b, tolerance))
            );
            Assert.That(resultIndices, Is.EqualTo(truthIndices));
        });
    }

    private static void CheckPrimitive(APrimitive primitive, BoundingBox originalBoundingBox)
    {
        Assert.That(primitive.AxisAlignedBoundingBox, Is.EqualTo(originalBoundingBox));
    }

    private static Mesh? ToMesh(APrimitive primitive)
    {
        return primitive switch
        {
            TriangleMesh triangleMesh => triangleMesh.Mesh,
            InstancedMesh instancedMesh => instancedMesh.TemplateMesh,
            _ => null
        };
    }

    private static void CheckGeometries(
        APrimitive[] primitives,
        List<Vector3> truthVertices,
        List<uint> truthIndices,
        List<BoundingBox> originalBoundingBoxes
    )
    {
        Assert.That(originalBoundingBoxes.ToArray(), Has.Length.EqualTo(primitives.Length));
        for (int i = 0; i < primitives.Length; i++)
        {
            APrimitive? primitive = primitives[i];
            Mesh? mesh = ToMesh(primitive);
            if (mesh != null)
            {
                CheckMeshList(mesh.Vertices, truthVertices, mesh.Indices, truthIndices);
            }
            else
            {
                CheckPrimitive(primitive, originalBoundingBoxes[i]);
            }
        }
    }

    private static void CheckThatPrimitivesHaveNotChanged(
        List<Mesh?> originalMeshList,
        List<BoundingBox> originalBoundingBoxes,
        APrimitive[] primitives
    )
    {
        Assert.Multiple(() =>
        {
            Assert.That(originalMeshList.ToArray(), Has.Length.EqualTo(primitives.Length));
            Assert.That(originalBoundingBoxes.ToArray(), Has.Length.EqualTo(primitives.Length));
        });

        for (int i = 0; i < primitives.Length; i++)
        {
            Mesh? mesh = ToMesh(primitives[i]);
            List<Vector3>? truthVertices = originalMeshList[i]?.Vertices.ToList();
            List<uint>? truthIndices = originalMeshList[i]?.Indices.ToList();
            if (mesh != null && truthVertices != null && truthIndices != null)
            {
                CheckMeshList(mesh.Vertices, truthVertices, mesh.Indices, truthIndices);
            }
            else
            {
                CheckPrimitive(primitives[i], originalBoundingBoxes[i]);
            }
        }
    }

    [Test]
    public void CheckScaffoldOptimizerActivation_GivenTestPartOptimizers_VerifyingTheReturnedNode()
    {
        // Set up the input
        CadRevealNode nodeA = CreateCadRevealNode("TestNode, Test A test").node;
        CadRevealNode nodeB = CreateCadRevealNode("TestNode, Test B test").node;
        CadRevealNode nodeC = CreateCadRevealNode("TestNode, Another BTest test").node;
        (CadRevealNode nodeD, List<Mesh?> nodeDMeshes, List<BoundingBox> boundingBoxes) = CreateCadRevealNode(
            "TestNode test"
        );

        // Create two optimizers
        var optimizerA = new ScaffoldPartOptimizerTestPartA();
        var optimizerB = new ScaffoldPartOptimizerTestPartB();

        // Configure the optimizer for testing
        var optimizer = new ScaffoldOptimizer();
        optimizer.AddPartOptimizer(optimizerA);
        optimizer.AddPartOptimizer(optimizerB);

        // Invoke the optimizer
        optimizer.OptimizeNode(nodeA);
        optimizer.OptimizeNode(nodeB);
        optimizer.OptimizeNode(nodeC);
        optimizer.OptimizeNode(nodeD);

        // Check the results
        CheckGeometries(nodeA.Geometries, optimizerA.GetVerticesTruth(), optimizerA.GetIndicesTruth(), boundingBoxes);
        CheckGeometries(nodeB.Geometries, optimizerB.GetVerticesTruth(), optimizerB.GetIndicesTruth(), boundingBoxes);
        CheckGeometries(nodeC.Geometries, optimizerB.GetVerticesTruth(), optimizerB.GetIndicesTruth(), boundingBoxes);
        CheckThatPrimitivesHaveNotChanged(nodeDMeshes, boundingBoxes, nodeD.Geometries);
    }
}
