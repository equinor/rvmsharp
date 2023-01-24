namespace CadRevealFbxProvider;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;

public class FbxNodeToCadRevealNodeConverter
{
    public static IEnumerable<CadRevealNode> ConvertRecursive(FbxNode node,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        FbxImporter fbxSdk,
        Dictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup)
    {
        var id = treeIndexGenerator.GetNextId();
        List<APrimitive> geometries = new List<APrimitive>();
        BoundingBox nodeBoundingBox = null;

        var name = FbxNodeWrapper.GetNodeName(node);
        var nodeGeometryPtr = FbxMeshWrapper.GetMeshGeometryPtr(node);
        var transform = FbxNodeWrapper.GetTransform(node);

        if (nodeGeometryPtr != IntPtr.Zero)
        {
            if (meshInstanceLookup.TryGetValue(nodeGeometryPtr, out var instanceData))
            {
                var bb = instanceData.templateMesh.CalculateAxisAlignedBoundingBox(transform);
                var instancedMeshCopy = new InstancedMesh(instanceData.instanceId, instanceData.templateMesh,
                    transform, id, Color.Aqua, // TODO: Temp debug color to distinguish copies of an instanced mesh
                    bb);
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
                    var instancedMesh = new InstancedMesh(instanceId, mesh,
                        transform,
                        id,
                        Color.Magenta, // TODO: Temp debug color to distinguish first Instance
                        bb);

                    geometries.Add(instancedMesh);
                    if (nodeBoundingBox != null)
                        nodeBoundingBox = nodeBoundingBox.Encapsulate(bb);
                    else
                        nodeBoundingBox = bb;
                }
            }
        }

        yield return new CadRevealNode {
            TreeIndex = id,
            Name = name,
            Geometries = geometries.ToArray(),
            BoundingBoxAxisAligned = nodeBoundingBox
        };

        var childCount = FbxNodeWrapper.GetChildCount(node);
        for (var i = 0; i < childCount; i++)
        {
            var child = FbxNodeWrapper.GetChild(i, node);
            var childCadRevealNodes = ConvertRecursive(
                child,
                treeIndexGenerator,
                instanceIdGenerator,
                fbxSdk,
                meshInstanceLookup);
            foreach (CadRevealNode cadRevealNode in childCadRevealNodes)
            {
                yield return cadRevealNode;
            }
        }
    }
}
