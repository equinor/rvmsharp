namespace CadRevealFbxProvider;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System.Drawing;

public class FbxNodeToCadRevealNodeConverter
{
    public static CadRevealNode? ConvertRecursive(
        FbxNode node,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering,
        Dictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup
    )
    {
        List<APrimitive> geometries = new List<APrimitive>();
        BoundingBox? nodeBoundingBox = null;

        var name = FbxNodeWrapper.GetNodeName(node);
        if (nodeNameFiltering.ShouldExcludeNode(name))
            return null;

        var id = treeIndexGenerator.GetNextId();
        var nodeGeometryPtr = FbxMeshWrapper.GetMeshGeometryPtr(node);
        var transform = FbxNodeWrapper.GetTransform(node);

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
                    ulong instanceId = instanceIdGenerator.GetNextId();

                    var bb = mesh.CalculateAxisAlignedBoundingBox(transform);

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
                    if (nodeBoundingBox != null)
                        nodeBoundingBox = nodeBoundingBox.Encapsulate(bb);
                    else
                        nodeBoundingBox = bb;
                }
            }
        }

        var childCount = FbxNodeWrapper.GetChildCount(node);
        List<CadRevealNode> children = new List<CadRevealNode>();
        for (var i = 0; i < childCount; i++)
        {
            FbxNode child = FbxNodeWrapper.GetChild(i, node);
            CadRevealNode? childCadRevealNode = ConvertRecursive(
                child,
                treeIndexGenerator,
                instanceIdGenerator,
                nodeNameFiltering,
                meshInstanceLookup
            );
            if (childCadRevealNode == null)
                continue;
            children.Add(childCadRevealNode);

            if (childCadRevealNode.Children != null)
            {
                foreach (CadRevealNode cadRevealNode in childCadRevealNode.Children)
                {
                    var childBoundingBox = cadRevealNode.BoundingBoxAxisAligned;
                    if (childBoundingBox != null)
                    {
                        if (nodeBoundingBox != null)
                        {
                            nodeBoundingBox = nodeBoundingBox.Encapsulate(childBoundingBox);
                        }
                        else
                        {
                            nodeBoundingBox = childBoundingBox;
                        }
                    }
                }
            }
        }

        return new CadRevealNode
        {
            TreeIndex = id,
            Name = name,
            Geometries = geometries.ToArray(),
            BoundingBoxAxisAligned = nodeBoundingBox,
            Children = children.ToArray()
        };
    }
}
