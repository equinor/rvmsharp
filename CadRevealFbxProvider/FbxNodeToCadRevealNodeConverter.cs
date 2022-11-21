namespace CadRevealFbxProvider;

using Utils;

using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;

using System;
using System.Collections.Generic;
using System.Drawing;


public class FbxNodeToCadRevealNodeConverter
{
    public static IEnumerable<CadRevealNode> ConvertRecursive(FbxImporter.FbxNode node,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        FbxImporter fbxSdk,
        Dictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup)
    {
        var id = treeIndexGenerator.GetNextId();
        List<APrimitive> geometries = new List<APrimitive>();

        var name = fbxSdk.GetNodeName(node);
        var nodeGeometryPtr = fbxSdk.GetMeshGeometryPtr(node);
        var fbxTransform = fbxSdk.GetTransform(node);
        var transform = FbxTransformConverter.ToMatrix4x4(fbxTransform);

        if (nodeGeometryPtr != IntPtr.Zero)
        {
            if (meshInstanceLookup.TryGetValue(nodeGeometryPtr, out var instanceData))
            {
                var bb = instanceData.templateMesh.CalculateAxisAlignedBoundingBox(transform);
                var instancedMeshCopy = new InstancedMesh(instanceData.instanceId, instanceData.templateMesh,
                    transform, id, Color.Aqua,
                    bb);
                geometries.Add(instancedMeshCopy);
            }
            else
            {
                var meshData = fbxSdk.GetGeometricData(node);
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
                        Color.Magenta, // Temp debug color to distinguish first Instance
                        bb);

                    geometries.Add(instancedMesh);
                }
            }
        }

        yield return new CadRevealNode { TreeIndex = id, Name = name, Geometries = geometries.ToArray() };

        var childCount = fbxSdk.GetChildCount(node);
        for (var i = 0; i < childCount; i++)
        {
            var child = fbxSdk.GetChild(i, node);
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
