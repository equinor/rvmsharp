namespace CadRevealFbxProvider.Tests.BatchUtils;

using System.Drawing;
using System.Numerics;
using System.Security.Cryptography;
using CadRevealComposer;
using CadRevealComposer.Operations.Tessellating;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

static class MeshCreator
{
    public static Mesh Create(
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

    public static Mesh Create(int coordinateCount)
    {
        var randomizer = new Random();
        var coordinates = new List<Vector3>();
        var indices = new List<uint>();
        const int maxRandInt = 512;
        const float maxRandFloat = 10.0f;

        for (int i = 0; i < coordinateCount; i++)
        {
            float rndX = maxRandFloat * (float)randomizer.Next(maxRandInt) / maxRandInt;
            float rndY = maxRandFloat * (float)randomizer.Next(maxRandInt) / maxRandInt;
            float rndZ = maxRandFloat * (float)randomizer.Next(maxRandInt) / maxRandInt;
            coordinates.Add(new Vector3(rndX, rndY, rndZ));
            indices.Add((uint)i);
        }

        return new Mesh(coordinates.ToArray(), indices.ToArray(), 0.01f);
    }

    public static Mesh? CreateCone()
    {
        var cone = new Cone(
            (float)(2.0 * Math.PI),
            (float)(2.0 * Math.PI),
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 15.0f),
            new Vector3(0, 0, 1.0f),
            4.0f,
            2.5f,
            0,
            Color.Black,
            new BoundingBox(new Vector3(-4.0f, -4.0f, 0.0f), new Vector3(4.0f, 4.0f, 15.0f))
        );
        return ConeTessellator.Tessellate(cone)?.Mesh;
    }

    public static Mesh CreateValidAxisAlignedPlank()
    {
        var coordinates = new List<Vector3>();
        var indices = new List<uint>();

        coordinates.Add(new Vector3(0.0f, 0.0f, 0.0f));
        coordinates.Add(new Vector3(6.2f, 0.0f, 0.0f));
        coordinates.Add(new Vector3(0.0f, 0.7f, 0.0f));
        coordinates.Add(new Vector3(6.2f, 0.7f, 0.0f));

        coordinates.Add(new Vector3(0.0f, 0.0f, 0.3f));
        coordinates.Add(new Vector3(6.2f, 0.0f, 0.3f));
        coordinates.Add(new Vector3(0.0f, 0.7f, 0.3f));
        coordinates.Add(new Vector3(6.2f, 0.7f, 0.3f));

        for (int i = 0; i < coordinates.Count; i++)
        {
            indices.Add((uint)i);
        }

        return new Mesh(coordinates.ToArray(), indices.ToArray(), 0.01f);
    }

    public static void ValidateMesh(
        APrimitive primitive,
        float x1Truth,
        float y1Truth,
        float z1Truth,
        float x2Truth,
        float y2Truth,
        float z2Truth,
        float x3Truth,
        float y3Truth,
        float z3Truth,
        uint i1Truth,
        uint i2Truth,
        uint i3Truth
    )
    {
        const float tolerance = 1.0E-6f;
        List<Vector3> truthVertices =
        [
            new Vector3(x1Truth, y1Truth, z1Truth),
            new Vector3(x2Truth, y2Truth, z2Truth),
            new Vector3(x3Truth, y3Truth, z3Truth)
        ];
        List<uint> truthIndices = [i1Truth, i2Truth, i3Truth];
        Assert.Multiple(() =>
        {
            switch (primitive)
            {
                case InstancedMesh instancedMesh:
                    Assert.That(
                        instancedMesh.TemplateMesh.Vertices,
                        Is.EqualTo(truthVertices).Using<Vector3>((a, b) => a.EqualsWithinTolerance(b, tolerance))
                    );
                    Assert.That(instancedMesh.TemplateMesh.Indices, Is.EqualTo(truthIndices));
                    break;
                case TriangleMesh triangleMesh:
                    Assert.That(
                        triangleMesh.Mesh.Vertices,
                        Is.EqualTo(truthVertices).Using<Vector3>((a, b) => a.EqualsWithinTolerance(b, tolerance))
                    );
                    Assert.That(triangleMesh.Mesh.Indices, Is.EqualTo(truthIndices));
                    break;
            }
        });
    }
}

class ScaffoldOptimizerOverride(
    List<Mesh?> trueNodeMeshes,
    string trueName,
    ScaffoldOptimizerOverride.OptimizationRoutine optimizationRoutine
) : ScaffoldOptimizerBase
{
    public enum OptimizationRoutine
    {
        CheckInputOptimizeToNull = 0,
        NoInputCheckOptimizeToMeshesAndPrimitiveAndCopy,
        NoInputCheckOptimizeToSplitMeshes
    }

    protected override List<ScaffoldOptimizerResult>? OptimizeNode(
        string nodeName,
        Mesh?[] meshes,
        APrimitive[] nodeGeometries,
        Func<ulong, int, ulong> requestChildMeshInstanceId
    )
    {
        if (optimizationRoutine == OptimizationRoutine.CheckInputOptimizeToNull)
        {
            Assert.Multiple(() =>
            {
                Assert.That(nodeName, Is.EqualTo(trueName));
                Assert.That(meshes, Has.Length.EqualTo(trueNodeMeshes.Count));
            });

            Assert.Multiple(() =>
            {
                Assert.That(meshes[0], Is.SameAs(trueNodeMeshes[0]));
                Assert.That(meshes[1], Is.SameAs(trueNodeMeshes[1]));
                Assert.That(meshes[2], Is.SameAs(trueNodeMeshes[2]));
                Assert.That(meshes[3], Is.SameAs(trueNodeMeshes[3]));
                Assert.That(meshes[4], Is.Null);
            });
        }

        var results = new List<ScaffoldOptimizerResult>();
        var mesh1 = MeshCreator.Create(52, 52, 52, 72, 72, 72, 32, 32, 32, 2, 12, 22);
        var mesh2 = MeshCreator.Create(13, 23, 33, 63, 73, 83, 93, 103, 113, 3, 13, 23);
        var mesh3 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh4 = MeshCreator.Create(52, 52, 52, 72, 72, 72, 32, 32, 32, 2, 12, 22);
        var mesh5 = MeshCreator.Create(13, 23, 33, 63, 73, 83, 93, 103, 113, 3, 13, 23);
        var mesh6 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh7 = MeshCreator.Create(13, 23, 33, 63, 73, 83, 93, 103, 113, 3, 13, 23);
        var mesh8 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh9 = MeshCreator.Create(13, 23, 33, 63, 73, 83, 93, 103, 113, 3, 13, 23);
        var mesh10 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh11 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh12 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh13 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh14 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh15 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        var mesh16 = MeshCreator.Create(64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
        APrimitive prim1 = mesh1.CalculateAxisAlignedBoundingBox().ToBoxPrimitive(3847, Color.Black);

        switch (optimizationRoutine)
        {
            case OptimizationRoutine.CheckInputOptimizeToNull:
                return null;
            case OptimizationRoutine.NoInputCheckOptimizeToMeshesAndPrimitiveAndCopy:
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[0], mesh1, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[1], mesh2, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(prim1));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[3], mesh3, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[4]));
                return results;
            case OptimizationRoutine.NoInputCheckOptimizeToSplitMeshes:
                // Split mesh 1 with Instance ID = 1 into three parts, remembering to assign an index for each new mesh in the split and use the correct base for the split
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[0], mesh1, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[0], mesh2, 1, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[0], mesh3, 2, requestChildMeshInstanceId));

                // Split mesh 2 with Instance ID = 1 into three parts, remembering to assign an index for each new mesh in the split and use the correct base for the split
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[1], mesh4, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[1], mesh5, 1, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[1], mesh6, 2, requestChildMeshInstanceId));

                // Split mesh 3 with Instance ID = 2 into two parts, remembering to assign an index for each new mesh in the split and use the correct base for the split
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[2], mesh7, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[2], mesh8, 1, requestChildMeshInstanceId));

                // Split mesh 4 with Instance ID = 2 into two parts, remembering to assign an index for each new mesh in the split and use the correct base for the split
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[3], mesh9, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[3], mesh10, 1, requestChildMeshInstanceId));

                // Split mesh 5 with Instance ID = 2 into two parts, remembering to assign an index for each new mesh in the split and use the correct base for the split
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[4], mesh11, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[4], mesh12, 1, requestChildMeshInstanceId));

                // Split mesh 6 with Instance ID = 3 into four parts, remembering to assign an index for each new mesh in the split and use the correct base for the split
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[5], mesh13, 0, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[5], mesh14, 1, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[5], mesh15, 2, requestChildMeshInstanceId));
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[5], mesh16, 3, requestChildMeshInstanceId));

                // We make no changes to the last entry of the node, which is non-mesh geometry, and simply return it
                results.Add(new ScaffoldOptimizerResult(nodeGeometries[6]));
                return results;
            default:
                ArgumentOutOfRangeException? exception = new ArgumentOutOfRangeException();
                exception.HelpLink = null;
                exception.HResult = 0;
                exception.Source = null;
                throw exception;
        }
    }
}

public class ScaffoldOptimizerTests
{
    public enum TestPurpose
    {
        TestGeometryAssignment = 0,
        TestWithOnlyNonMeshPrimitives,
        TestInstancing,
        TestOptimizationSteps,
        TestOptimizationOnTessellatedObject,
        TestPlankOptimization
    }

    private static (
        CadRevealNode node,
        List<Mesh?> nodeMeshes,
        List<BoundingBox> boundingBoxes,
        List<(int i1, int i2)> instancePairs
    ) CreateCadRevealNode(string partName, TestPurpose testPurpose)
    {
        var mesh1 = MeshCreator.Create(5, 5, 5, 7, 7, 7, 3, 3, 3, 0, 1, 2);
        var mesh2 = MeshCreator.Create(1, 2, 3, 6, 7, 8, 9, 10, 11, 0, 1, 2);
        var mesh3 = MeshCreator.Create(6, 5, 4, 1, 3, 2, 14, 15, 16, 0, 1, 2);

        var bbox1 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var bbox2 = new BoundingBox(new Vector3(1, 2, 3), new Vector3(4, 5, 6));

        var mesh4 = MeshCreator.Create(1000);
        var mesh5 = MeshCreator.CreateCone();

        var mesh6 = MeshCreator.CreateValidAxisAlignedPlank();
        var bbox6 = mesh6.CalculateAxisAlignedBoundingBox();
        Assert.That(mesh5, Is.Not.Null);

        switch (testPurpose)
        {
            case TestPurpose.TestGeometryAssignment:
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
            case TestPurpose.TestWithOnlyNonMeshPrimitives:
                var node3 = new CadRevealNode
                {
                    TreeIndex = 0,
                    Name = partName,
                    Parent = null,
                    Geometries =
                    [
                        new Circle(Matrix4x4.Identity, new Vector3(1.0f, 8.0f, 2.0f), 5, Color.Black, bbox2),
                        new Circle(Matrix4x4.Identity, new Vector3(1.2f, 8.2f, 2.2f), 5, Color.Black, bbox2),
                        new Circle(Matrix4x4.Identity, new Vector3(1.4f, 8.4f, 2.4f), 5, Color.Black, bbox2),
                        new Circle(Matrix4x4.Identity, new Vector3(1.6f, 8.6f, 2.6f), 5, Color.Black, bbox2)
                    ]
                };
                return (node3, [null, null, null, null], [bbox1, bbox1, bbox1, bbox1], []);
            case TestPurpose.TestInstancing:
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
            case TestPurpose.TestOptimizationSteps:
                var node4 = new CadRevealNode
                {
                    TreeIndex = 0,
                    Name = partName,
                    Parent = null,
                    Geometries =
                    [
                        new InstancedMesh(
                            1,
                            mesh4,
                            Matrix4x4.Identity,
                            1,
                            Color.Black,
                            mesh4.CalculateAxisAlignedBoundingBox()
                        )
                    ]
                };
                return (node4, [mesh4], [mesh4.CalculateAxisAlignedBoundingBox()], []);
            case TestPurpose.TestOptimizationOnTessellatedObject:
                var node5 = new CadRevealNode
                {
                    TreeIndex = 0,
                    Name = partName,
                    Parent = null,
                    Geometries =
                    [
                        new InstancedMesh(
                            1,
                            mesh5,
                            Matrix4x4.Identity,
                            1,
                            Color.Black,
                            mesh4.CalculateAxisAlignedBoundingBox()
                        )
                    ]
                };
                return (node5, [mesh5], [mesh5.CalculateAxisAlignedBoundingBox()], []);
            case TestPurpose.TestPlankOptimization:
                var node6 = new CadRevealNode
                {
                    TreeIndex = 0,
                    Name = partName,
                    Parent = null,
                    Geometries =
                    [
                        new InstancedMesh(1, mesh6, Matrix4x4.Identity, 1, Color.Black, bbox6),
                        new InstancedMesh(2, mesh2, Matrix4x4.Identity, 2, Color.Black, bbox1),
                    ]
                };
                return (node6, [mesh6, mesh2], [bbox6, bbox1], []);
            default:
                throw new ArgumentOutOfRangeException(nameof(testPurpose), testPurpose, null);
        }
    }

    private static CadRevealNode CloneCadRevealNode(CadRevealNode node)
    {
        return new CadRevealNode
        {
            TreeIndex = node.TreeIndex,
            Name = node.Name,
            Parent = node.Parent,
            Geometries = node.Geometries
        };
    }

    [Test]
    public void TestMeshExtraction_GivenMixedNodeContent_CheckThatMeshesAreCorrectlyExtracted()
    {
        ulong currentInstanceId = 0;

        // Prepare data
        var nodeData = CreateCadRevealNode("Test A", TestPurpose.TestGeometryAssignment);

        // Make copy of the node while keeping the node contents and references in its tables the same
        CadRevealNode nodeCpy = CloneCadRevealNode(nodeData.node);

        // Perform action to test and validate input to the OptimizeNode() call
        var optimizer = new ScaffoldOptimizerOverride(
            nodeData.nodeMeshes,
            "Test A",
            ScaffoldOptimizerOverride.OptimizationRoutine.CheckInputOptimizeToNull
        );
        optimizer.OptimizeNode(nodeData.node, OnRequestNewInstanceId);

        // Validate the returned values from Optimize. Since we return null from ScaffoldOptimizerOverride1.OptimizeNode()
        // above, we expect that NO changes are done to the node contents.
        Assert.Multiple(() =>
        {
            Assert.That(nodeData.node.Geometries[0], Is.SameAs(nodeCpy.Geometries[0]));
            Assert.That(nodeData.node.Geometries[1], Is.SameAs(nodeCpy.Geometries[1]));
            Assert.That(nodeData.node.Geometries[2], Is.SameAs(nodeCpy.Geometries[2]));
            Assert.That(nodeData.node.Geometries[3], Is.SameAs(nodeCpy.Geometries[3]));
            Assert.That(nodeData.node.Geometries[4], Is.SameAs(nodeCpy.Geometries[4]));
        });

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }

    [Test]
    public void TestMeshOutput_GivenMixedNodeContent_CheckThatMeshesAreCorrectlyOutput()
    {
        ulong currentInstanceId = 0;

        // Prepare data
        var nodeData = CreateCadRevealNode("Test B", TestPurpose.TestGeometryAssignment);

        // Make copy of the node while keeping the node contents and references in its tables the same
        CadRevealNode nodeCpy = CloneCadRevealNode(nodeData.node);

        // Perform action to test
        var optimizer = new ScaffoldOptimizerOverride(
            nodeData.nodeMeshes,
            "Test B",
            ScaffoldOptimizerOverride.OptimizationRoutine.NoInputCheckOptimizeToMeshesAndPrimitiveAndCopy
        );
        optimizer.OptimizeNode(nodeData.node, OnRequestNewInstanceId);

        // Validate the returned values from Optimize. Since we return null from ScaffoldOptimizerOverride1.OptimizeNode()
        // above, we expect that NO changes are done to the node contents.
        Assert.Multiple(() =>
        {
            MeshCreator.ValidateMesh(nodeData.node.Geometries[0], 52, 52, 52, 72, 72, 72, 32, 32, 32, 2, 12, 22);
            MeshCreator.ValidateMesh(nodeData.node.Geometries[1], 13, 23, 33, 63, 73, 83, 93, 103, 113, 3, 13, 23);
            Assert.That(nodeData.node.Geometries[2].TreeIndex, Is.EqualTo(3847));
            MeshCreator.ValidateMesh(nodeData.node.Geometries[3], 64, 54, 44, 14, 34, 24, 144, 154, 164, 4, 14, 24);
            Assert.That(nodeData.node.Geometries[4], Is.SameAs(nodeCpy.Geometries[4]));
        });

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }

    [Test]
    public void TestMeshOutput_GivenOnlyNonMeshPrimitiveNodeContent_CheckThatMeshesAreCorrectlyOutput()
    {
        ulong currentInstanceId = 0;

        // Prepare data
        var nodeData = CreateCadRevealNode("Test C", TestPurpose.TestWithOnlyNonMeshPrimitives);

        // Make copy of the node while keeping the node contents and references in its tables the same
        CadRevealNode nodeCpy = CloneCadRevealNode(nodeData.node);

        // Perform action to test
        var optimizer = new ScaffoldOptimizerOverride(
            nodeData.nodeMeshes,
            "Test C",
            ScaffoldOptimizerOverride.OptimizationRoutine.NoInputCheckOptimizeToMeshesAndPrimitiveAndCopy
        );
        optimizer.OptimizeNode(nodeData.node, OnRequestNewInstanceId);

        // Validate the returned values from Optimize. Since we extract only non-mesh primitives in ScaffoldOptimizerOverride1.OptimizeNode()
        // above, we expect that NO changes are done to the node contents.
        Assert.Multiple(() =>
        {
            Assert.That(nodeData.node.Geometries[0], Is.SameAs(nodeCpy.Geometries[0]));
            Assert.That(nodeData.node.Geometries[1], Is.SameAs(nodeCpy.Geometries[1]));
            Assert.That(nodeData.node.Geometries[2], Is.SameAs(nodeCpy.Geometries[2]));
            Assert.That(nodeData.node.Geometries[3], Is.SameAs(nodeCpy.Geometries[3]));
        });

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }

    [Test]
    public void TestInstanceIDAssignmentWithoutMeshSplit_GivenMeshesWithInstanceIDsAndANonMesh_CheckThatInstanceIDsAreCorrectlyAssigned()
    {
        ulong currentInstanceId = 0;

        // Prepare data
        var nodeData1 = CreateCadRevealNode("Test D", TestPurpose.TestGeometryAssignment);
        var nodeData2 = CreateCadRevealNode("Test D", TestPurpose.TestGeometryAssignment);
        CadRevealNode[] nodeData = [nodeData1.node, nodeData2.node];

        // Make copy of the nodes while keeping the node contents and references in its tables the same
        CadRevealNode nodeCpy1 = CloneCadRevealNode(nodeData1.node);
        CadRevealNode nodeCpy2 = CloneCadRevealNode(nodeData2.node);

        // Perform action to test
        var optimizer = new ScaffoldOptimizerOverride(
            nodeData1.nodeMeshes,
            "Test D",
            ScaffoldOptimizerOverride.OptimizationRoutine.NoInputCheckOptimizeToMeshesAndPrimitiveAndCopy
        );
        optimizer.OptimizeNodes(nodeData.ToList(), OnRequestNewInstanceId);

        // Validate the returned instance ID values assigned to the objects returned from OptimizeNode
        // Input to optimizer for both nodes:
        //   Indices into Geometry:      0 1 2 3 4
        //   Instance IDs node 1:        1 2 X X X
        // Output from optimizer for nodes should be
        //   Indices into Geometry:       0   1  2 3 4
        //   Child mesh indices node 1:  (0) (0) X X X
        //   Instance IDs node 1:        (A) (B) X X X
        //   Child mesh indices node 1:  (0) (0) X X X
        //   Instance IDs node 1:        (A) (B) X X X
        //
        // X - non mesh primitive, () - signifies one mesh split
        Assert.Multiple(() =>
        {
            Assert.That(ToId(nodeData1.node, 0), Is.Not.EqualTo(ToId(nodeData1.node, 1)));
            Assert.That(ToId(nodeData2.node, 0), Is.Not.EqualTo(ToId(nodeData2.node, 1)));
            Assert.That(ToId(nodeData1.node, 0), Is.EqualTo(ToId(nodeData2.node, 0)));
            Assert.That(ToId(nodeData1.node, 1), Is.EqualTo(ToId(nodeData2.node, 1)));
            Assert.That(nodeData1.node.Geometries[4], Is.SameAs(nodeCpy1.Geometries[4]));
            Assert.That(nodeData2.node.Geometries[4], Is.SameAs(nodeCpy2.Geometries[4]));
        });

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
        ulong? ToId(CadRevealNode node, int index) => (node.Geometries[index] as InstancedMesh)?.InstanceId;
    }

    [Test]
    public void TestInstanceIDAssignmentAfterMeshSplit_GivenMeshesWithInstanceIDsAndANonMesh_CheckThatInstanceIDsAreCorrectlyAssigned()
    {
        ulong currentInstanceId = 0;

        // Prepare data
        var nodeData1 = CreateCadRevealNode("Test E", TestPurpose.TestInstancing);
        var nodeData2 = CreateCadRevealNode("Test E", TestPurpose.TestInstancing);
        CadRevealNode[] nodeData = [nodeData1.node, nodeData2.node];

        // Make copy of the nodes while keeping the node contents and references in its tables the same
        CadRevealNode nodeCpy1 = CloneCadRevealNode(nodeData1.node);
        CadRevealNode nodeCpy2 = CloneCadRevealNode(nodeData2.node);

        // Perform action to test
        var optimizer = new ScaffoldOptimizerOverride(
            nodeData1.nodeMeshes,
            "Test E",
            ScaffoldOptimizerOverride.OptimizationRoutine.NoInputCheckOptimizeToSplitMeshes
        );
        optimizer.OptimizeNodes(nodeData.ToList(), OnRequestNewInstanceId);

        // Validate the returned instance ID values assigned to the objects returned from OptimizeNode
        // Input to optimizer for both nodes:
        //   Indices into Geometry:      0 1 2 3 4 5 6
        //   Instance IDs node 1:        1 1 2 2 2 3 X
        // Output from optimizer for nodes should be
        //   Indices into Geometry:       0 1 2   3 4 5   6 7   8 9   10  11  12 13 14 15 16
        //   Child mesh indices node 1:  (0 1 2) (0 1 2) (0 1) (0 1) (0   1) (0  1  2  3) X
        //   Instance IDs node 1:        (A B C) (A B C) (D E) (D E) (D   E) (F  G  H  I) X
        //   Child mesh indices node 2:  (0 1 2) (0 1 2) (0 1) (0 1) (0   1) (0  1  2  3) X
        //   Instance IDs node 2:        (A B C) (A B C) (D E) (D E) (D   E) (F  G  H  I) X
        //
        // X - non mesh primitive, () - signifies one mesh split
        Assert.Multiple(() =>
        {
            Assert.That(ToId(nodeData1.node, 0), Is.EqualTo(ToId(nodeData1.node, 3)));
            Assert.That(ToId(nodeData1.node, 1), Is.EqualTo(ToId(nodeData1.node, 4)));
            Assert.That(ToId(nodeData1.node, 2), Is.EqualTo(ToId(nodeData1.node, 5)));
            Assert.That(ToId(nodeData1.node, 0), Is.Not.EqualTo(ToId(nodeData1.node, 1)));
            Assert.That(ToId(nodeData1.node, 0), Is.Not.EqualTo(ToId(nodeData1.node, 2)));
            Assert.That(ToId(nodeData1.node, 1), Is.Not.EqualTo(ToId(nodeData1.node, 2)));

            Assert.That(ToId(nodeData1.node, 6), Is.EqualTo(ToId(nodeData1.node, 8)));
            Assert.That(ToId(nodeData1.node, 8), Is.EqualTo(ToId(nodeData1.node, 10)));
            Assert.That(ToId(nodeData1.node, 7), Is.EqualTo(ToId(nodeData1.node, 9)));
            Assert.That(ToId(nodeData1.node, 9), Is.EqualTo(ToId(nodeData1.node, 11)));
            Assert.That(ToId(nodeData1.node, 6), Is.Not.EqualTo(ToId(nodeData1.node, 7)));
            Assert.That(ToId(nodeData1.node, 8), Is.Not.EqualTo(ToId(nodeData1.node, 9)));
            Assert.That(ToId(nodeData1.node, 10), Is.Not.EqualTo(ToId(nodeData1.node, 11)));

            Assert.That(ToId(nodeData1.node, 12), Is.Not.EqualTo(ToId(nodeData1.node, 13)));
            Assert.That(ToId(nodeData1.node, 12), Is.Not.EqualTo(ToId(nodeData1.node, 14)));
            Assert.That(ToId(nodeData1.node, 12), Is.Not.EqualTo(ToId(nodeData1.node, 15)));
            Assert.That(ToId(nodeData1.node, 13), Is.Not.EqualTo(ToId(nodeData1.node, 14)));
            Assert.That(ToId(nodeData1.node, 13), Is.Not.EqualTo(ToId(nodeData1.node, 15)));
            Assert.That(ToId(nodeData1.node, 14), Is.Not.EqualTo(ToId(nodeData1.node, 15)));

            Assert.That(nodeData1.node.Geometries[16], Is.SameAs(nodeCpy1.Geometries[6]));
            Assert.That(nodeData2.node.Geometries[16], Is.SameAs(nodeCpy2.Geometries[6]));
        });

        for (int i = 0; i < 16; i++)
        {
            Assert.That(ToId(nodeData1.node, i), Is.EqualTo(ToId(nodeData2.node, i)));
        }

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
        ulong? ToId(CadRevealNode node, int index) => (node.Geometries[index] as InstancedMesh)?.InstanceId;
    }

    [Test]
    public void TestMeshOutput_GivenAPartNameNotAssignedForOptimization_CheckThatTheOptimizationWasNotDone()
    {
        ulong currentInstanceId = 0;

        // Prepare
        var nodeDataNoAssignedForOptimization = CreateCadRevealNode(
            "___NonExistentRandomPartName___",
            TestPurpose.TestGeometryAssignment
        );

        // Act
        var optimizer = new CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ScaffoldOptimizer();
        optimizer.OptimizeNode(nodeDataNoAssignedForOptimization.node, OnRequestNewInstanceId);
        CadRevealNode nodeDataNoAssignedForOptimizationClone = CloneCadRevealNode(
            nodeDataNoAssignedForOptimization.node
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(nodeDataNoAssignedForOptimization.node.Geometries[0], Is.InstanceOf<InstancedMesh>());
            Assert.That(
                nodeDataNoAssignedForOptimization.nodeMeshes[0]?.TriangleCount,
                Is.EqualTo(
                    (nodeDataNoAssignedForOptimizationClone.Geometries[0] as InstancedMesh)?.TemplateMesh.TriangleCount
                )
            );
        });

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }

    [Test]
    [TestCase("some plank")]
    public void TestMeshOutput_GivenAPartForAxisAlignedBoundingBoxOptimization_CheckThatTheOptimizationWasDone(
        string partName
    )
    {
        ulong currentInstanceId = 0;

        // Prepare
        var nodeData = CreateCadRevealNode(partName, TestPurpose.TestPlankOptimization);
        CadRevealNode nodeOptimized = CloneCadRevealNode(nodeData.node);

        // Act
        var optimizer = new CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ScaffoldOptimizer();
        optimizer.OptimizeNode(nodeOptimized, OnRequestNewInstanceId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(nodeOptimized.Geometries, Has.Length.GreaterThan(0));
            Assert.That(nodeData.node.Geometries[0], Is.InstanceOf<InstancedMesh>());
            Assert.That(nodeOptimized.Geometries[0], Is.InstanceOf<Box>());
        });

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }

    [Test]
    [TestCase("some kick board", TestPurpose.TestOptimizationSteps)]
    [TestCase("some brm", TestPurpose.TestOptimizationSteps)]
    [TestCase("StairwayGuard", TestPurpose.TestOptimizationOnTessellatedObject)]
    public void TestMeshOutput_GivenAPartForConvexHullOrDecimationOptimization_CheckThatTheOptimizationWasDone(
        string partName,
        TestPurpose testPurpose
    )
    {
        ulong currentInstanceId = 0;

        // Prepare
        var nodeData = CreateCadRevealNode(partName, testPurpose);
        CadRevealNode nodeOptimized = CloneCadRevealNode(nodeData.node);

        // Act
        var optimizer = new CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer.ScaffoldOptimizer();
        optimizer.OptimizeNode(nodeOptimized, OnRequestNewInstanceId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(nodeOptimized.Geometries, Has.Length.GreaterThan(0));
            Assert.That(nodeOptimized.Geometries[0], Is.InstanceOf<InstancedMesh>());
            Assert.That(
                nodeData.nodeMeshes[0]?.TriangleCount,
                Is.Not.EqualTo((nodeOptimized.Geometries[0] as InstancedMesh)?.TemplateMesh.TriangleCount)
            );
        });

        return;
        ulong OnRequestNewInstanceId() => currentInstanceId++;
    }
}
