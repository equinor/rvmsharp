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
    private enum ETestPurpose
    {
        TestGeometryAssignment = 0,
        TestInstancing
    }

    private enum EOptimizers
    {
        None = -1,
        A = 0,
        B = 1,
        C = 2
    }

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

    private static (
        CadRevealNode node,
        List<Mesh?> nodeMeshes,
        List<BoundingBox> boundingBoxes,
        List<(int i1, int i2)> instancePairs
    ) CreateCadRevealNode(string partName, ETestPurpose testPurpose)
    {
        var mesh1 = CreateMesh(5, 5, 5, 7, 7, 7, 3, 3, 3, 0, 1, 2);
        var mesh2 = CreateMesh(1, 2, 3, 6, 7, 8, 9, 10, 11, 0, 1, 2);
        var mesh3 = CreateMesh(6, 5, 4, 1, 3, 2, 14, 15, 16, 0, 1, 2);

        var bbox1 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var bbox2 = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));

        switch (testPurpose)
        {
            case ETestPurpose.TestGeometryAssignment:
                var node1 = new CadRevealNode
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
                return (node1, [mesh1, mesh2, mesh3, mesh1, null], [bbox1, bbox1, bbox1, bbox1, bbox2], []);
            case ETestPurpose.TestInstancing:
                var node2 = new CadRevealNode
                {
                    TreeIndex = 0,
                    Name = partName,
                    Parent = null,
                    Geometries =
                    [
                        new InstancedMesh(1, mesh1, Matrix4x4.Identity, 1, Color.Black, bbox1),
                        new InstancedMesh(1, mesh1, Matrix4x4.Identity, 2, Color.Black, bbox1),
                        new InstancedMesh(2, mesh2, Matrix4x4.Identity, 1, Color.Black, bbox1),
                        new InstancedMesh(2, mesh2, Matrix4x4.Identity, 2, Color.Black, bbox1),
                        new InstancedMesh(2, mesh2, Matrix4x4.Identity, 1, Color.Black, bbox1),
                        new InstancedMesh(3, mesh3, Matrix4x4.Identity, 2, Color.Black, bbox1),
                        new Circle(Matrix4x4.Identity, new Vector3(1.0f, 8.0f, 2.0f), 5, Color.Black, bbox2)
                    ]
                };
                return (
                    node2,
                    [mesh1, mesh1, mesh2, mesh2, mesh2, mesh3, null],
                    [bbox1, bbox1, bbox1, bbox1, bbox1, bbox1, bbox2],
                    [(0, 1), (2, 3), (2, 4)]
                );
            default:
                throw new ArgumentOutOfRangeException(nameof(testPurpose), testPurpose, null);
        }
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
        List<List<Vector3>> truthVertices,
        List<List<uint>> truthIndices,
        List<BoundingBox> originalBoundingBoxes
    )
    {
        Assert.Multiple(() =>
        {
            Assert.That(originalBoundingBoxes.ToArray(), Has.Length.EqualTo(primitives.Length));
            Assert.That(truthVertices.ToArray(), Has.Length.EqualTo(primitives.Length));
            Assert.That(truthIndices.ToArray(), Has.Length.EqualTo(primitives.Length));
        });
        for (int i = 0; i < primitives.Length; i++)
        {
            APrimitive? primitive = primitives[i];
            Mesh? mesh = ToMesh(primitive);
            if (mesh != null)
            {
                CheckMeshList(mesh.Vertices, truthVertices[i], mesh.Indices, truthIndices[i]);
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

    private static List<List<Vector3>> GenVerticesTruth(List<ScaffoldPartOptimizerTest?> optimizersExpectedToRun)
    {
        var verticesTruth = new List<List<Vector3>>();
        foreach (var item in optimizersExpectedToRun)
        {
            verticesTruth.AddRange(
                (item != null)
                    ? item.GetVerticesTruth()
                    :
                    [
                        []
                    ]
            );
        }

        return verticesTruth;
    }

    private static List<List<uint>> GenIndicesTruth(List<ScaffoldPartOptimizerTest?> optimizersExpectedToRun)
    {
        var indicesTruth = new List<List<uint>>();
        foreach (var item in optimizersExpectedToRun)
        {
            indicesTruth.AddRange(
                (item != null)
                    ? item.GetIndicesTruth()
                    :
                    [
                        []
                    ]
            );
        }

        return indicesTruth;
    }

    private static List<BoundingBox> GenBoundingBoxesTruth(
        List<(BoundingBox bbox, int copies)> boundingBoxAndNumCopiesList
    )
    {
        var listOfExpectedBoundingBoxes = new List<BoundingBox>();
        foreach (var entry in boundingBoxAndNumCopiesList)
        {
            for (int i = 0; i < entry.copies; i++)
            {
                listOfExpectedBoundingBoxes.Add(entry.bbox);
            }
        }

        return listOfExpectedBoundingBoxes;
    }

    private static List<(int i1, int i2)> GenInstancePairsTruth(
        List<(int i1, int i2)> instancePairsBeforeOptimization,
        int numPrimitivesPerMeshSplit
    )
    {
        var instancePairs = new List<(int i1, int i2)>();

        foreach (var pair in instancePairsBeforeOptimization)
        {
            for (int i = 0; i < numPrimitivesPerMeshSplit; i++)
            {
                //
                // For example, if we have the following situation
                //
                // Instance ID before optimization           Instance ID form after optimization
                // -----------------------------------------------------------------------------
                //                1                             X(A1)(B1)XX(C1)
                //                1                             X(A2)(B2)XX(C2)
                //                2                             X(A3)(B3)XX(C3)
                //                2                             X(A4)(B4)XX(C4)
                //                2                             X(A5)(B5)XX(C5)
                //                3                             X(A6)(B6)XX(C6)
                //                X                             X
                //
                // X: non-instancing primitive, An, Bn, Cn: instance IDs of first, second, and third instance primitives, respectively
                //
                // We then want A1 = A2, B1 = B2, C1 = C2. Further, we want A3 = A4, etc., and A3 = A5, etc. Hence, we create pairs
                // between A's, between B's, and between C's, where instances before optimization pair up. Since the instance IDs
                // after optimization become a flat array, we need to calculate the indices of An that correspond to indices for
                // Am, by:
                //         In = i1 * N + i,
                //         Im = i2 * N + i,
                // where i1 and i2 are instances of meshes with same instance ID, before optimization, N is the number of primitives generated
                // for each instanced mesh, and i is the local index into Instance IDs after optimization. Similar calculations hold for B
                // and C.
                //
                // We will also include connections between non-instanced primitives and just ignore those later.
                //
                int I1 = pair.i1 * numPrimitivesPerMeshSplit + i;
                int I2 = pair.i2 * numPrimitivesPerMeshSplit + i;
                instancePairs.Add((I1, I2));
            }
        }

        return instancePairs;
    }

    private static void CheckInstanceIdAssignment(
        List<(int i1, int i2)> instancePairsAfterOptimization,
        CadRevealNode nodeToCheck,
        int preOptimizationPrimitivesCount,
        List<int> numPrimitivesPerMeshSplit
    )
    {
        // Check that the split parts that belong to the same instance have the same instance IDs
        foreach (var instancePair in instancePairsAfterOptimization)
        {
            APrimitive g1 = nodeToCheck.Geometries[instancePair.i1];
            APrimitive g2 = nodeToCheck.Geometries[instancePair.i2];
            if (g1 is InstancedMesh m1 && g2 is InstancedMesh m2)
            {
                Assert.That(m1.InstanceId, Is.EqualTo(m2.InstanceId));
            }
        }

        // Check that the mesh parts that belonged to one part before optimization do NOT have the same instance IDs
        int startIndex = 0;
        for (int i = 0; i < preOptimizationPrimitivesCount; i++)
        {
            for (int i1 = 0; i1 < numPrimitivesPerMeshSplit[i]; i1++)
            {
                for (int i2 = i1 + 1; i2 < numPrimitivesPerMeshSplit[i]; i2++)
                {
                    APrimitive g1 = nodeToCheck.Geometries[startIndex + i1];
                    APrimitive g2 = nodeToCheck.Geometries[startIndex + i2];
                    if (g1 is InstancedMesh m1 && g2 is InstancedMesh m2)
                    {
                        Assert.That(m1.InstanceId, Is.Not.EqualTo(m2.InstanceId));
                    }
                }
            }

            startIndex += numPrimitivesPerMeshSplit[i];
        }
    }

    private (
        ScaffoldOptimizer optimizer,
        List<ScaffoldPartOptimizerTest> scaffoldOptimizers
    ) ConfigureOptimizerForTesting()
    {
        // Create optimizers
        var scaffoldOptimizers = new List<ScaffoldPartOptimizerTest>
        {
            new ScaffoldPartOptimizerTestPartA(),
            new ScaffoldPartOptimizerTestPartB(),
            new ScaffoldPartOptimizerTestPartC()
        };

        // Configure the optimizer for testing
        var optimizer = new ScaffoldOptimizer();
        foreach (var scaffoldOptimizer in scaffoldOptimizers)
        {
            optimizer.AddPartOptimizer(scaffoldOptimizer);
        }

        return (optimizer, scaffoldOptimizers);
    }

    [Test]
    [TestCase(
        "TestNode, Test A test",
        new int[]
        {
            (int)EOptimizers.A,
            (int)EOptimizers.A,
            (int)EOptimizers.A,
            (int)EOptimizers.A,
            (int)EOptimizers.None
        },
        new int[] { 1, 1, 1, 1, 1 }
    )]
    [TestCase(
        "TestNode, Test B test",
        new int[]
        {
            (int)EOptimizers.B,
            (int)EOptimizers.B,
            (int)EOptimizers.B,
            (int)EOptimizers.B,
            (int)EOptimizers.None
        },
        new int[] { 1, 1, 1, 1, 1 }
    )]
    [TestCase(
        "TestNode, Another BTest test",
        new int[]
        {
            (int)EOptimizers.B,
            (int)EOptimizers.B,
            (int)EOptimizers.B,
            (int)EOptimizers.B,
            (int)EOptimizers.None
        },
        new int[] { 1, 1, 1, 1, 1 }
    )]
    [TestCase(
        "Test C",
        new int[]
        {
            (int)EOptimizers.C,
            (int)EOptimizers.C,
            (int)EOptimizers.C,
            (int)EOptimizers.C,
            (int)EOptimizers.None
        },
        new int[] { 6, 6, 6, 6, 1 }
    )]
    public void CheckScaffoldOptimizerActivation_GivenTestPartOptimizers_VerifyingTheReturnedNode(
        string partName,
        int[] indicesOfOptimizersExpectedToRun,
        int[] boundingBoxCopiesInOptimization
    )
    {
        ulong currentInstanceId = 100;

        // Set up the input
        var input = CreateCadRevealNode(partName, ETestPurpose.TestGeometryAssignment);

        // Configure optimizer for testing
        var testOptimizer = ConfigureOptimizerForTesting();

        // Invoke the optimizer
        testOptimizer.optimizer.OptimizeNode(input.node, OnRequestNewInstanceId);

        // Generate the expected results
        List<ScaffoldPartOptimizerTest?> optimizersExpectedToRun = indicesOfOptimizersExpectedToRun
            .Select(index => (index >= 0) ? testOptimizer.scaffoldOptimizers[index] : null)
            .ToList();
        var optimizedNodeAVerticesTruth = GenVerticesTruth(optimizersExpectedToRun);
        var optimizedNodeAIndicesTruth = GenIndicesTruth(optimizersExpectedToRun);
        var boundingBoxCopiesInOptimizationDef = boundingBoxCopiesInOptimization.Select(
            (n, i) => (input.boundingBoxes[i], n)
        );
        var optimizedNodeEBoundingBoxesTruth = GenBoundingBoxesTruth(boundingBoxCopiesInOptimizationDef.ToList());

        // Check that the geometries match expected results
        CheckGeometries(
            input.node.Geometries,
            optimizedNodeAVerticesTruth,
            optimizedNodeAIndicesTruth,
            optimizedNodeEBoundingBoxesTruth
        );

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }

    [Test]
    public void CheckScaffoldOptimizer_GivenTestPartOptimizers_VerifyingThatNonInstancePrimitivesAreNotAltered()
    {
        ulong currentInstanceId = 100;

        // Set up the input
        var input = CreateCadRevealNode("TestNode test", ETestPurpose.TestGeometryAssignment);

        // Configure optimizer for testing
        var testOptimizer = ConfigureOptimizerForTesting();

        // Invoke the optimizer
        testOptimizer.optimizer.OptimizeNode(input.node, OnRequestNewInstanceId);

        // Check that the geometries match expected results
        CheckThatPrimitivesHaveNotChanged(input.nodeMeshes, input.boundingBoxes, input.node.Geometries);

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }

    [Test]
    public void CheckScaffoldOptimizer_GivenTestPartOptimizers_VerifyingInstanceIds()
    {
        ulong currentInstanceId = 100;

        // Set up the input
        var input = CreateCadRevealNode("Test C", ETestPurpose.TestInstancing);
        int preOptimizationPrimitivesCount = input.node.Geometries.Length;

        // Configure optimizer for testing
        var testOptimizer = ConfigureOptimizerForTesting();

        // Invoke the optimizer
        testOptimizer.optimizer.OptimizeNode(input.node, OnRequestNewInstanceId);

        // Generate expected results
        var optimizedNodeInstancePairsTruth = GenInstancePairsTruth(input.instancePairs, 6);

        // Check that instance IDs are correctly assigned
        CheckInstanceIdAssignment(
            optimizedNodeInstancePairsTruth,
            input.node,
            preOptimizationPrimitivesCount,
            [6, 6, 6, 6, 6, 6, 1]
        );

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }
}
