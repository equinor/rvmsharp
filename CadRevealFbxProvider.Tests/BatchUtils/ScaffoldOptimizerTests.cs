using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils;
using System.Drawing;
using System.Numerics;

public class ScaffoldOptimizerTests
{
    private Mesh CreateMesh(float x1, float y1, float z1, float x2, float y2, float z2, float x3, float y3, float z3, uint i1, uint i2, uint i3)
    {
        return new Mesh
        (
            [
                new Vector3(x1, y1, z1), new Vector3(x2, y2, z2), new Vector3(x3, y3, z3),
            ],
            [i1, i2, i3],
            0.0f
        );
    }

    private CadRevealNode CreateCadRevealNode(string partName)
    {
        var mesh1 = CreateMesh(5, 5, 5, 7, 7, 7, 3, 3, 3,0, 1, 2);
        var mesh2 = CreateMesh(1, 2, 3, 6, 7, 8, 9, 10, 11,0, 1, 2);
        var mesh3 = CreateMesh(6, 5, 4, 1, 3, 2, 14, 15, 16,0, 1, 2);

        return new CadRevealNode { TreeIndex = 0, Name = partName, Parent = null, Geometries =
            [
                new InstancedMesh(1, mesh1, Matrix4x4.Identity, 1, Color.Black, new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))),
                new InstancedMesh(2, mesh2, Matrix4x4.Identity, 2, Color.Black, new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))),
                new TriangleMesh(mesh3, 3, Color.Black, new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1))),
                new TriangleMesh(mesh1, 4, Color.Black, new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1)))
            ]
        };
    }

    private void CheckMeshList(Vector3[] resultVertices, List<Vector3> truthVertices, uint[] resultIndices, List<uint> truthIndices)
    {
        Assert.That(resultVertices.Length, Is.EqualTo(truthVertices.Count));
        Assert.That(resultIndices.Length, Is.EqualTo(truthIndices.Count));
        for (uint i = 0; i < resultVertices.Length; i++)
        {
            const float tolerance = 1.0E-6f;
            Assert.That(resultVertices[i].X, Is.EqualTo(truthVertices.ToArray()[i].X).Within(tolerance));
            Assert.That(resultVertices[i].Y, Is.EqualTo(truthVertices.ToArray()[i].Y).Within(tolerance));
            Assert.That(resultVertices[i].Z, Is.EqualTo(truthVertices.ToArray()[i].Z).Within(tolerance));
            Assert.That(resultIndices[i], Is.EqualTo(truthIndices.ToArray()[i]));
        }
    }

    private void CheckGeometries(APrimitive[] primitives, ScaffoldPartOptimizerTest optimizer)
    {
        foreach (var primitive in primitives)
        {
            switch (primitive)
            {
                case TriangleMesh triangleMesh:
                    CheckMeshList(triangleMesh.Mesh.Vertices, optimizer.GetVerticesTruth(),
                        triangleMesh.Mesh.Indices, optimizer.GetIndicesTruth());
                    break;
                case InstancedMesh instancedMesh:
                    CheckMeshList(instancedMesh.TemplateMesh.Vertices, optimizer.GetVerticesTruth(),
                        instancedMesh.TemplateMesh.Indices, optimizer.GetIndicesTruth());
                    break;
            }
        }
    }

    [Test]
    public void CheckScaffoldOptimizerActivation_GivenTestPartOptimizers_VerifyingTheReturnedNode()
    {
        // Set up the input
        CadRevealNode nodeA = CreateCadRevealNode("TestNode, Test A test");
        CadRevealNode nodeB = CreateCadRevealNode("TestNode, Test B test");
        CadRevealNode nodeC = CreateCadRevealNode("TestNode, Another Test test");
        CadRevealNode nodeD = CreateCadRevealNode("TestNode test");

        // Configure the optimizer for testing
        ScaffoldOptimizer.AddPartOptimizer(new ScaffoldPartOptimizerTestPartA());
        ScaffoldOptimizer.AddPartOptimizer(new ScaffoldPartOptimizerTestPartB());

        // Invoke the optimizer
        ScaffoldOptimizer.OptimizeNode(nodeA);
        ScaffoldOptimizer.OptimizeNode(nodeB);
        ScaffoldOptimizer.OptimizeNode(nodeC);

        // Check the results
        CheckGeometries(nodeA.Geometries, new ScaffoldPartOptimizerTestPartA());
        CheckGeometries(nodeB.Geometries, new ScaffoldPartOptimizerTestPartB());
        CheckGeometries(nodeC.Geometries, new ScaffoldPartOptimizerTestPartB());
        // :TODO: Continue to check NodeD
    }
}
