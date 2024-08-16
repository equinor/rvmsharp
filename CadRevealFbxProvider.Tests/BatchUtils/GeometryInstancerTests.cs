using CadRevealFbxProvider.BatchUtils;
using CadRevealComposer;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System.Drawing;
using System.Numerics;

public class GeometryOptimizerTests
{
    private void AssertMeshReferences(APrimitive a, APrimitive b, bool equal)
    {
        switch (a)
        {
            case TriangleMesh t1 when b is TriangleMesh t2:
                Assert.That(Object.ReferenceEquals(t1.Mesh, t2.Mesh), Is.EqualTo(equal));
                break;
            case InstancedMesh i1 when b is InstancedMesh i2:
                Assert.That(Object.ReferenceEquals(i1.TemplateMesh, i2.TemplateMesh), Is.EqualTo(equal));
                break;
            case TriangleMesh t when b is InstancedMesh i:
                Assert.That(Object.ReferenceEquals(t.Mesh, i.TemplateMesh), Is.EqualTo(equal));
                break;
            case InstancedMesh i when b is TriangleMesh t:
                Assert.That(Object.ReferenceEquals(i.TemplateMesh, t.Mesh), Is.EqualTo(equal));
                break;
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
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[0], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[0], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[0], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.03f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));

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
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[0], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[0], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new TriangleMesh(new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.01f),
            1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[1], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[1], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));
        primitives.Add(new InstancedMesh(instanceIds[1], new Mesh(dummyVertices.ToArray(), dummyIndices.ToArray(), 0.02f),
            Matrix4x4.Identity, 1, new Color(), new BoundingBox(new Vector3(), new Vector3())));

        node.Geometries = primitives.ToArray();

        return new APrimitive[] { primitives[3], primitives[7] }; // Primitives to be instanced
    }

    private void CheckNodePattern1(CadRevealNode node)
    {
        // Check that the non-instanced objects are still all distinct
        for (var i = 1; i < node.Geometries.Length; i++)
        {
            AssertMeshReferences(node.Geometries[0], node.Geometries[i], false);
        }
        for (var i = 2; i < node.Geometries.Length; i++)
        {
            AssertMeshReferences(node.Geometries[1], node.Geometries[i], false);
        }
        for (var i = 2; i < node.Geometries.Length; i++)
        {
            if (i != 5) AssertMeshReferences(node.Geometries[5], node.Geometries[i], false);
        }
        for (var i = 3; i < node.Geometries.Length; i++)
        {
            if (i != 6) AssertMeshReferences(node.Geometries[6], node.Geometries[i], false);
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
        for (var i = 1; i < node.Geometries.Length; i++)
        {
            AssertMeshReferences(node.Geometries[0], node.Geometries[i], false);
        }
        for (var i = 2; i < node.Geometries.Length; i++)
        {
            AssertMeshReferences(node.Geometries[1], node.Geometries[i], false);
        }
        for (var i = 3; i < node.Geometries.Length; i++)
        {
            AssertMeshReferences(node.Geometries[2], node.Geometries[i], false);
        }
        for (var i = 2; i < node.Geometries.Length; i++)
        {
            if (i != 5) AssertMeshReferences(node.Geometries[5], node.Geometries[i], false);
        }
        for (var i = 2; i < node.Geometries.Length; i++)
        {
            if (i != 6) AssertMeshReferences(node.Geometries[6], node.Geometries[i], false);
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
        CadRevealNode node2WithPattern1)
    {
        // Check that the non-instanced meshes are all distinct across nodes
        for (var i = 0; i < node1WithPattern1.Geometries.Length; i++)
        {
            if (i is >= 2 and <= 4) continue;
            for (var j = 0; j < node2WithPattern1.Geometries.Length; j++)
            {
                if (j is >= 2 and <= 4) continue;
                AssertMeshReferences(node1WithPattern1.Geometries[i], node2WithPattern1.Geometries[j], false);
            }
        }

        // Check that the instanced meshes are the same across nodes
        for (var i = 0; i < node1WithPattern1.Geometries.Length; i++)
        {
            if (i is < 2 or > 4) continue;
            for (var j = 0; j < node2WithPattern1.Geometries.Length; j++)
            {
                if (j is < 2 or > 4) continue;
                AssertMeshReferences(node1WithPattern1.Geometries[i], node2WithPattern1.Geometries[j], true);
            }
        }
    }

    private void CrossCheckBetweenNodesPattern2ToPattern2(
        CadRevealNode node1WithPattern2,
        CadRevealNode node2WithPattern2)
    {
        // Check that the non-instanced meshes are all distinct across nodes
        for (var i = 0; i < node1WithPattern2.Geometries.Length; i++)
        {
            if ((i is >= 3 and <= 4) || (i is >= 7 and <= 9)) continue;
            for (var j = 0; j < node2WithPattern2.Geometries.Length; j++)
            {
                if ((j is >= 3 and <= 4) || (j is >= 7 and <= 9)) continue;
                AssertMeshReferences(node1WithPattern2.Geometries[i], node2WithPattern2.Geometries[j], false);
            }
        }

        // Check that the instanced meshes are the same across nodes
        for (var i = 0; i < node1WithPattern2.Geometries.Length; i++)
        {
            if ((i is >= 0 and <= 2) || (i is >= 5 and <= 6)) continue;
            for (var j = 0; j < node2WithPattern2.Geometries.Length; j++)
            {
                if ((j is >= 0 and <= 2) || (j is >= 5 and <= 6)) continue;
                if ((i is >= 3 and <= 4) && (j is >= 7 and <= 9)) continue;
                if ((i is >= 7 and <= 9) && (j is >= 3 and <= 4)) continue;
                AssertMeshReferences(node1WithPattern2.Geometries[i], node2WithPattern2.Geometries[j], true);
            }
        }
    }

    private void CrossCheckBetweenNodesPattern1ToPattern2(
        CadRevealNode nodeWithPattern1,
        CadRevealNode nodeWithPattern2)
    { // 1112211333
        /*
        // Check that the non-instanced meshes are all distinct across nodes
        for (var i = 0; i < nodeWithPattern1.Geometries.Length; i++)
        {
            if (i is >= 2 and <= 4) continue;
            for (var j = 0; j < nodeWithPattern2.Geometries.Length; j++)
            {
                if ((j is >= 3 and <= 4) || (j is >= 7 and <= 9)) continue;
                AssertMeshReferences(nodeWithPattern1.Geometries[i], nodeWithPattern2.Geometries[j], false);
            }
        }

        // Check that the instanced meshes are the same across nodes
        for (var i = 0; i < nodeWithPattern1.Geometries.Length; i++)
        {
            if (i is < 2 or > 4) continue;
            for (var j = 0; j < nodeWithPattern2.Geometries.Length; j++)
            {
                if ((j is >= 0 and <= 2) || (j is >= 5 and <= 6)) continue;
                AssertMeshReferences(nodeWithPattern1.Geometries[i], nodeWithPattern2.Geometries[j], true);
            }
        }*/
    }

    private void CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade(
        Func<CadRevealNode, ulong[], APrimitive[]> prepareNodePattern,
        Action<CadRevealNode> checkNodePattern,
        ulong[] instanceIDs)
    {
        // Prepare a list of CadRevealNodes containing various InstanceMesh objects with same instance ID, but not sharing meshes
        var nodes = new List<CadRevealNode>();
        nodes.Add(new CadRevealNode{ TreeIndex = 0, Name = "First", Parent = null });

        prepareNodePattern(nodes[0], instanceIDs);

        // Reconnect the instances
        GeometryInstancer.Invoke(nodes);

        // Check node
        checkNodePattern(nodes[0]);
    }

    [Test]
    public void CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade_1()
    {
        CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade(
            PrepareNodePattern1, CheckNodePattern1, new ulong[] { 123 });
    }
    [Test]
    public void CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade_2()
    {
        CheckCorrectInstancingSingleNode_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade(
            PrepareNodePattern2, CheckNodePattern2, new ulong[] { 123, 456 });
    }

    [Test]
    public void CheckCorrectInstancingMultipleNodes_GivenNodesWithUnconnectedInstancedMeshes_VerifyConnectionsMade()
    {
        // Prepare a list of CadRevealNodes containing various InstanceMesh objects with same instance ID, but not sharing meshes
        var nodes = new List<CadRevealNode>();
        nodes.Add(new CadRevealNode{ TreeIndex = 0, Name = "First", Parent = null });
        nodes.Add(new CadRevealNode{ TreeIndex = 0, Name = "Second", Parent = null });
        nodes.Add(new CadRevealNode{ TreeIndex = 0, Name = "Third", Parent = null });
        nodes.Add(new CadRevealNode{ TreeIndex = 0, Name = "Fourth", Parent = null });

        PrepareNodePattern1(nodes[0], new ulong[] {123});
        PrepareNodePattern2(nodes[1], new ulong[] {123, 456});
        PrepareNodePattern1(nodes[2], new ulong[] {123});
        PrepareNodePattern2(nodes[3], new ulong[] {123, 456});

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
    }
}
