namespace CadRevealFbxProvider.Tests.BatchUtils;

using System.Drawing;
using System.Numerics;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealFbxProvider.BatchUtils.ScaffoldOptimizer;

public class GeometryOptimizerTests
{
    private static ulong? GetInstanceId(APrimitive primitive)
    {
        return primitive switch
        {
            InstancedMesh instancedMesh => instancedMesh.InstanceId,
            _ => null
        };
    }

    private void AssertMeshReferences(APrimitive a, APrimitive b, bool equal)
    {
        switch (a)
        {
            case TriangleMesh t1 when b is TriangleMesh t2:
                Assert.That(t1.Mesh, equal ? Is.SameAs(t2.Mesh) : Is.Not.SameAs(t2.Mesh));
                break;
            case InstancedMesh i1 when b is InstancedMesh i2:
                Assert.That(i1.TemplateMesh, equal ? Is.SameAs(i2.TemplateMesh) : Is.Not.SameAs(i2.TemplateMesh));
                break;
            case TriangleMesh t when b is InstancedMesh i:
                Assert.That(t.Mesh, equal ? Is.SameAs(i.TemplateMesh) : Is.Not.SameAs(i.TemplateMesh));
                break;
            case InstancedMesh i when b is TriangleMesh t:
                Assert.That(i.TemplateMesh, equal ? Is.SameAs(t.Mesh) : Is.Not.SameAs(t.Mesh));
                break;
            default:
                throw new Exception("No valid combination of TriangleMesh and InstanceMesh in a and b could be found");
        }
    }

    private void CheckThatInstancedMeshesAreDifferentInPattern1(CadRevealNode node)
    {
        // Verify that the instanced objects with same instance ID do not share the same meshes
        for (var i = 3; i <= 4; i++)
        {
            AssertMeshReferences(node.Geometries[2], node.Geometries[i], false);
        }
        AssertMeshReferences(node.Geometries[3], node.Geometries[4], false);
    }

    private APrimitive[] PrepareNodePattern1(CadRevealNode node, ulong[] instanceIds)
    {
        var dummyVertices = new List<Vector3>();
        dummyVertices.Add(new Vector3());
        dummyVertices.Add(new Vector3());
        dummyVertices.Add(new Vector3());

        var dummyIndices = new List<uint>();
        dummyIndices.Add(0);
        dummyIndices.Add(1);
        dummyIndices.Add(2);

        var primitives = new List<APrimitive>();
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[0],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[0],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[0],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.03f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );

        node.Geometries = primitives.ToArray();
        CheckThatInstancedMeshesAreDifferentInPattern1(node);

        return new APrimitive[] { primitives[2] }; // Primitives to be instanced
    }

    private APrimitive[] PrepareNodePattern2(CadRevealNode node, ulong[] instanceIds)
    {
        var dummyVertices = new List<Vector3>();
        dummyVertices.Add(new Vector3());
        dummyVertices.Add(new Vector3());
        dummyVertices.Add(new Vector3());

        var dummyIndices = new List<uint>();
        dummyIndices.Add(0);
        dummyIndices.Add(1);
        dummyIndices.Add(2);

        var primitives = new List<APrimitive>();
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[0],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[0],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new TriangleMesh(
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[1],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[1],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );
        primitives.Add(
            new InstancedMesh(
                instanceIds[1],
                new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
                Matrix4x4.Identity,
                1,
                new Color(),
                new BoundingBox(new Vector3(), new Vector3())
            )
        );

        node.Geometries = primitives.ToArray();

        return new APrimitive[] { primitives[3], primitives[7] }; // Primitives to be instanced
    }

    private void CheckNodePattern1(CadRevealNode node)
    {
        // Check that the non-instanced objects are still all distinct
        foreach (var j in new[] { 0, 1, 5, 6 })
        {
            for (var i = 0; i < node.Geometries.Length; i++)
            {
                if (i == j)
                    continue;
                AssertMeshReferences(node.Geometries[j], node.Geometries[i], false);
            }
        }

        // Check that the instanced objects with same instance ID now share the same meshes
        for (var i = 3; i <= 4; i++)
        {
            AssertMeshReferences(node.Geometries[2], node.Geometries[i], true);
        }
    }

    private void CheckNodePattern2(CadRevealNode node)
    {
        // Check that the non-instanced objects are still all distinct
        foreach (int j in new[] { 0, 1, 2, 5, 6 })
        {
            for (int i = 0; i < node.Geometries.Length; i++)
            {
                if (i == j)
                    continue;
                AssertMeshReferences(node.Geometries[j], node.Geometries[i], false);
            }
        }

        // Check that the instanced objects with same instance ID now share the same meshes
        AssertMeshReferences(node.Geometries[3], node.Geometries[4], true);
        for (var i = 8; i <= 9; i++)
        {
            AssertMeshReferences(node.Geometries[7], node.Geometries[i], true);
        }

        // Check that the two instance IDs produced distinct instances
        AssertMeshReferences(node.Geometries[3], node.Geometries[7], false);
    }

    private void CrossCheckBetweenNodesPattern1ToPattern1(
        CadRevealNode node1WithPattern1,
        CadRevealNode node2WithPattern1,
        bool checkThatInstancesAreUnequal = false
    )
    {
        // Check that the non-instanced meshes are all distinct across nodes
        for (var i = 0; i < node1WithPattern1.Geometries.Length; i++)
        {
            if (i is >= 2 and <= 4)
                continue;
            for (var j = 0; j < node2WithPattern1.Geometries.Length; j++)
            {
                if (j is >= 2 and <= 4)
                    continue;
                AssertMeshReferences(node1WithPattern1.Geometries[i], node2WithPattern1.Geometries[j], false);
            }
        }

        // Check that the instanced meshes are the same across nodes
        for (var i = 0; i < node1WithPattern1.Geometries.Length; i++)
        {
            if (i is < 2 or > 4)
                continue;
            for (var j = 0; j < node2WithPattern1.Geometries.Length; j++)
            {
                if (j is < 2 or > 4)
                    continue;
                AssertMeshReferences(
                    node1WithPattern1.Geometries[i],
                    node2WithPattern1.Geometries[j],
                    !checkThatInstancesAreUnequal
                );
            }
        }
    }

    private void CrossCheckBetweenNodesPattern2ToPattern2(
        CadRevealNode node1WithPattern2,
        CadRevealNode node2WithPattern2
    )
    {
        // Check that the non-instanced meshes are all distinct across nodes
        for (var i = 0; i < node1WithPattern2.Geometries.Length; i++)
        {
            if ((i is >= 3 and <= 4) || (i is >= 7 and <= 9))
                continue;
            for (var j = 0; j < node2WithPattern2.Geometries.Length; j++)
            {
                if ((j is >= 3 and <= 4) || (j is >= 7 and <= 9))
                    continue;
                AssertMeshReferences(node1WithPattern2.Geometries[i], node2WithPattern2.Geometries[j], false);
            }
        }

        // Check that the instanced meshes are the same across nodes
        for (var i = 0; i < node1WithPattern2.Geometries.Length; i++)
        {
            var instanceId1 = GetInstanceId(node1WithPattern2.Geometries[i]);

            if ((i is >= 0 and <= 2) || (i is >= 5 and <= 6))
                continue;
            for (var j = 0; j < node2WithPattern2.Geometries.Length; j++)
            {
                var instanceId2 = GetInstanceId(node2WithPattern2.Geometries[j]);

                if ((j is >= 0 and <= 2) || (j is >= 5 and <= 6))
                    continue;
                if ((i is >= 3 and <= 4) && (j is >= 7 and <= 9))
                    continue;
                if ((i is >= 7 and <= 9) && (j is >= 3 and <= 4))
                    continue;
                if (instanceId1 != instanceId2)
                    continue;
                AssertMeshReferences(node1WithPattern2.Geometries[i], node2WithPattern2.Geometries[j], true);
            }
        }
    }

    private void CrossCheckBetweenNodesPattern1ToPattern2(
        CadRevealNode nodeWithPattern1,
        CadRevealNode nodeWithPattern2
    )
    {
        // Check that the non-instanced meshes are all distinct across nodes
        for (var i = 0; i < nodeWithPattern1.Geometries.Length; i++)
        {
            if (i is >= 2 and <= 4)
                continue;
            for (var j = 0; j < nodeWithPattern2.Geometries.Length; j++)
            {
                if ((j is >= 3 and <= 4) || (j is >= 7 and <= 9))
                    continue;
                AssertMeshReferences(nodeWithPattern1.Geometries[i], nodeWithPattern2.Geometries[j], false);
            }
        }

        // Check that the instanced meshes are the same across nodes
        for (var i = 0; i < nodeWithPattern1.Geometries.Length; i++)
        {
            var instanceId1 = GetInstanceId(nodeWithPattern1.Geometries[i]);

            if (i is < 2 or > 4)
                continue;
            for (var j = 0; j < nodeWithPattern2.Geometries.Length; j++)
            {
                var instanceId2 = GetInstanceId(nodeWithPattern2.Geometries[j]);

                if ((j is >= 0 and <= 2) || (j is >= 5 and <= 6))
                    continue;
                if (instanceId1 != instanceId2)
                    continue;
                AssertMeshReferences(nodeWithPattern1.Geometries[i], nodeWithPattern2.Geometries[j], true);
            }
        }
    }

    private void CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade(
        Func<CadRevealNode, ulong[], APrimitive[]> prepareNodePattern,
        Action<CadRevealNode> checkNodePattern,
        ulong[] instanceIDs
    )
    {
        // Prepare a list of CadRevealNodes containing various InstanceMesh objects with same instance ID, but not sharing meshes
        var nodes = new List<CadRevealNode>();
        nodes.Add(
            new CadRevealNode
            {
                TreeIndex = 0,
                Name = "First",
                Parent = null
            }
        );

        prepareNodePattern(nodes[0], instanceIDs);

        // Reconnect the instances
        GeometryInstancer.Invoke(nodes);

        // Check node
        checkNodePattern(nodes[0]);
    }

    [Test]
    public void GeometryInstancerInvoke_WithSingleNodeGivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade_1()
    {
        CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade(
            PrepareNodePattern1,
            CheckNodePattern1,
            new ulong[] { 123 }
        );
    }

    [Test]
    public void GeometryInstancerInvoke_WithSingleNodeGivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade_2()
    {
        CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade(
            PrepareNodePattern2,
            CheckNodePattern2,
            new ulong[] { 123, 456 }
        );
    }

    [Test]
    public void GeometryInstancerInvoke_WithMultipleNodesGivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade()
    {
        // Prepare a list of CadRevealNodes containing various InstanceMesh objects with same instance ID, but not sharing meshes
        var nodes = new List<CadRevealNode>();
        nodes.Add(
            new CadRevealNode
            {
                TreeIndex = 0,
                Name = "First",
                Parent = null
            }
        );
        nodes.Add(
            new CadRevealNode
            {
                TreeIndex = 0,
                Name = "Second",
                Parent = null
            }
        );
        nodes.Add(
            new CadRevealNode
            {
                TreeIndex = 0,
                Name = "Third",
                Parent = null
            }
        );
        nodes.Add(
            new CadRevealNode
            {
                TreeIndex = 0,
                Name = "Fourth",
                Parent = null
            }
        );
        nodes.Add(
            new CadRevealNode
            {
                TreeIndex = 0,
                Name = "Fifth",
                Parent = null
            }
        );

        PrepareNodePattern1(nodes[0], new ulong[] { 123 });
        PrepareNodePattern2(nodes[1], new ulong[] { 123, 456 });
        PrepareNodePattern1(nodes[2], new ulong[] { 123 });
        PrepareNodePattern2(nodes[3], new ulong[] { 123, 456 });
        PrepareNodePattern1(nodes[4], new ulong[] { 789 });

        // Reconnect the instances
        GeometryInstancer.Invoke(nodes);

        // Check nodes
        CheckNodePattern1(nodes[0]);
        CheckNodePattern2(nodes[1]);
        CheckNodePattern1(nodes[2]);
        CheckNodePattern2(nodes[3]);
        CrossCheckBetweenNodesPattern1ToPattern1(nodes[0], nodes[2]);
        CrossCheckBetweenNodesPattern2ToPattern2(nodes[1], nodes[3]);
        CrossCheckBetweenNodesPattern1ToPattern2(nodes[0], nodes[1]);
        CrossCheckBetweenNodesPattern1ToPattern2(nodes[0], nodes[3]);
        CrossCheckBetweenNodesPattern1ToPattern2(nodes[2], nodes[3]);
        CrossCheckBetweenNodesPattern1ToPattern2(nodes[2], nodes[1]);
        CheckNodePattern1(nodes[4]);
        CrossCheckBetweenNodesPattern1ToPattern1(nodes[0], nodes[4], true);
        CrossCheckBetweenNodesPattern1ToPattern1(nodes[2], nodes[4], true);
    }
}
