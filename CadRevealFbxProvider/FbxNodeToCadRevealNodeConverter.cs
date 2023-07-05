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
        var name = FbxNodeWrapper.GetNodeName(node);

        var geometry = ReadGeometry(id, node, instanceIdGenerator, meshInstanceLookup, geometriesThatShouldBeInstanced);

        var cadRevealNode = new CadRevealNode
        {
            TreeIndex = id,
            Name = name,
            Parent = parent,
            Geometries = geometry != null ? new[] { geometry } : Array.Empty<APrimitive>(),
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

        // Calculate bounding box based on child's bounds if any
        var axisAlignedBoundingBoxIncludingChildNodes = ExtendBoundingBoxWithChildrenBounds(
            children,
            geometry?.AxisAlignedBoundingBox
        );

        cadRevealNode.Children = children.ToArray();
        cadRevealNode.BoundingBoxAxisAligned = axisAlignedBoundingBoxIncludingChildNodes;
        return cadRevealNode;
    }

    private static BoundingBox? ExtendBoundingBoxWithChildrenBounds(
        List<CadRevealNode> children,
        BoundingBox? optionalStartingBoundingBox
    )
    {
        // Does not need to be recursive since all child are expected to have ran this method already.
        foreach (CadRevealNode childRevealNode in children)
        {
            var childBoundingBox = childRevealNode.BoundingBoxAxisAligned;
            if (childBoundingBox != null)
            {
                optionalStartingBoundingBox =
                    optionalStartingBoundingBox != null
                        ? optionalStartingBoundingBox.Encapsulate(childBoundingBox)
                        : childBoundingBox;
            }
        }

        return optionalStartingBoundingBox;
    }

    private static APrimitive? ReadGeometry(
        ulong treeIndex,
        FbxNode node,
        InstanceIdGenerator instanceIdGenerator,
        IDictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup,
        IReadOnlySet<IntPtr> geometriesThatShouldBeInstanced
    )
    {
        var nodeGeometryPtr = FbxMeshWrapper.GetMeshGeometryPtr(node);
        var transform = FbxNodeWrapper.GetTransform(node);

        if (nodeGeometryPtr == IntPtr.Zero)
        {
            return null;
        }

        if (meshInstanceLookup.TryGetValue(nodeGeometryPtr, out var instanceData))
        {
            var instancedMeshCopy = new InstancedMesh(
                instanceData.instanceId,
                instanceData.templateMesh,
                transform,
                treeIndex,
                Color.Aqua, // TODO: Temp debug color to distinguish copies of an instanced mesh
                instanceData.templateMesh.CalculateAxisAlignedBoundingBox(transform)
            );
            return instancedMeshCopy;
        }

        var meshData = FbxMeshWrapper.GetGeometricData(node);
        if (!meshData.HasValue)
        {
            throw new Exception("IntPtr" + nodeGeometryPtr + " was expected to have a mesh, but we found none.");
        }

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
                treeIndex,
                Color.Magenta, // TODO: Temp debug color to distinguish first Instance
                bb
            );
            return instancedMesh;
        }

        var triangleMesh = new TriangleMesh(
            mesh,
            treeIndex,
            Color.Yellow, // TODO: Temp debug color to distinguish un-instanced
            bb
        );

        return triangleMesh;
    }
}
