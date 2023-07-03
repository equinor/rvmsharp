namespace CadRevealFbxProvider;

using BatchUtils;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System.Drawing;

public static class FbxNodeToCadRevealNodeConverter
{
    public static CadRevealNode ConvertRecursive(
        FbxNode node,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        int minInstanceCountThreshold = 2
    )
    {
        var meshInstanceLookup = new Dictionary<IntPtr, (Mesh templateMesh, ulong instanceId)>();
        IReadOnlySet<IntPtr> geometriesThatShouldBeInstanced = FbxGeometryUtils.GetAllGeomPointersWithXOrMoreUses(
            node,
            minInstanceCountThreshold
        );
        return ConvertRecursiveInternal(
            node,
            parent: null,
            treeIndexGenerator,
            instanceIdGenerator,
            meshInstanceLookup,
            geometriesThatShouldBeInstanced
        );
    }

    private static CadRevealNode ConvertRecursiveInternal(
        FbxNode node,
        CadRevealNode? parent,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        Dictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup,
        IReadOnlySet<IntPtr> geometriesThatShouldBeInstanced
    )
    {
        var id = treeIndexGenerator.GetNextId();
        List<APrimitive> geometries = new List<APrimitive>();

        var name = FbxNodeWrapper.GetNodeName(node);
        var nodeGeometryPtr = FbxMeshWrapper.GetMeshGeometryPtr(node);
        var transform = FbxNodeWrapper.GetTransform(node);

        BoundingBox? nodeBoundingBox = null;
        if (nodeGeometryPtr != IntPtr.Zero)
        {
            if (meshInstanceLookup.TryGetValue(nodeGeometryPtr, out var instanceData))
            {
                var bb = instanceData.templateMesh.CalculateAxisAlignedBoundingBox(transform);
                var instancedMeshCopy = new InstancedMesh(
                    instanceData.instanceId,
                    instanceData.templateMesh,
                    transform,
                    id,
                    Color.Aqua, // TODO: Temp debug color to distinguish copies of an instanced mesh
                    bb
                );
                geometries.Add(instancedMeshCopy);

                if (nodeBoundingBox != null)
                    nodeBoundingBox = nodeBoundingBox.Encapsulate(bb);
                else
                    nodeBoundingBox = bb;
            }
            else
            {
                var meshData = FbxMeshWrapper.GetGeometricData(node);
                if (meshData.HasValue)
                {
                    var mesh = meshData.Value.Mesh;
                    var meshPtr = meshData.Value.MeshPtr;

                    var bb = mesh.CalculateAxisAlignedBoundingBox(transform);
                    if (geometriesThatShouldBeInstanced.Contains(meshData.Value.MeshPtr))
                    {
                        ulong instanceId = instanceIdGenerator.GetNextId();
                        meshInstanceLookup.Add(meshPtr, (mesh, instanceId));
                        var instancedMesh = new InstancedMesh(
                            instanceId,
                            mesh,
                            transform,
                            id,
                            Color.Magenta, // TODO: Temp debug color to distinguish first Instance
                            bb
                        );
                        geometries.Add(instancedMesh);
                    }
                    else
                    {
                        var triangleMesh = new TriangleMesh(
                            mesh,
                            id,
                            Color.Yellow, // TODO: Temp debug color to distinguish un-instanced
                            bb
                        );

                        geometries.Add(triangleMesh);
                    }

                    if (nodeBoundingBox != null)
                        nodeBoundingBox = nodeBoundingBox.Encapsulate(bb);
                    else
                        nodeBoundingBox = bb;
                }
            }
        }

        var cadRevealNode = new CadRevealNode
        {
            TreeIndex = id,
            Name = name,
            Parent = parent,
            Geometries = geometries.ToArray(),
        };

        var childCount = FbxNodeWrapper.GetChildCount(node);
        List<CadRevealNode> children = new List<CadRevealNode>();
        for (var i = 0; i < childCount; i++)
        {
            FbxNode child = FbxNodeWrapper.GetChild(i, node);
            CadRevealNode childCadRevealNode = ConvertRecursiveInternal(
                child,
                cadRevealNode,
                treeIndexGenerator,
                instanceIdGenerator,
                meshInstanceLookup,
                geometriesThatShouldBeInstanced
            );
            children.Add(childCadRevealNode);
        }

        cadRevealNode.Children = children.ToArray();

        // Calculate bounding box based on child's bounds if any
        foreach (CadRevealNode childRevealNode in cadRevealNode.Children)
        {
            var childBoundingBox = childRevealNode.BoundingBoxAxisAligned;
            if (childBoundingBox != null)
            {
                nodeBoundingBox =
                    nodeBoundingBox != null ? nodeBoundingBox.Encapsulate(childBoundingBox) : childBoundingBox;
            }
        }

        cadRevealNode.BoundingBoxAxisAligned = nodeBoundingBox;
        return cadRevealNode;
    }
}
