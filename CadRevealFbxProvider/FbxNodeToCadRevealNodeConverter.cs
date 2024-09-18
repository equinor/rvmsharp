namespace CadRevealFbxProvider;

using System.Drawing;
using System.Drawing;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions;
using BatchUtils;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using MIConvexHull;

public static class FbxNodeToCadRevealNodeConverter
{
    public static CadRevealNode? ConvertRecursive(
        FbxNode node,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering,
        Dictionary<string, Dictionary<string, string>?>? attributes,
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
            nodeNameFiltering,
            geometriesThatShouldBeInstanced,
            attributes
        );
    }

    private static CadRevealNode? ConvertRecursiveInternal(
        FbxNode node,
        CadRevealNode? parent,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        Dictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup,
        NodeNameFiltering nodeNameFiltering,
        IReadOnlySet<IntPtr> geometriesThatShouldBeInstanced,
        Dictionary<string, Dictionary<string, string>?>? attributes
    )
    {
        var name = node.GetNodeName();
        if (nodeNameFiltering.ShouldExcludeNode(name))
            return null;

        var id = treeIndexGenerator.GetNextId();
        var geometry = ReadGeometry(id, node, instanceIdGenerator, meshInstanceLookup, geometriesThatShouldBeInstanced);

        if (attributes != null)
            if (!ValidateNodeAttributes(attributes, name))
                return null;

        var triCount = geometry != null ? GetTriCountForGeometry(geometry) : null;
        var cadRevealNode = new CadRevealNode
        {
            TreeIndex = id,
            Name = name,
            Parent = parent,
            Geometries = geometry != null ? [geometry] : [],
            OptionalDiagnosticInfo = JsonSerializer.Serialize(
                new { triCount, geometryType = geometry?.GetType().ToString() }
            )
        };

        var childCount = node.GetChildCount();
        List<CadRevealNode> children = [];
        for (var i = 0; i < childCount; i++)
        {
            FbxNode child = node.GetChild(i);
            CadRevealNode? childCadRevealNode = ConvertRecursiveInternal(
                child,
                cadRevealNode,
                treeIndexGenerator,
                instanceIdGenerator,
                meshInstanceLookup,
                nodeNameFiltering,
                geometriesThatShouldBeInstanced,
                attributes
            );

            if (childCadRevealNode != null)
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

        int? GetTriCountForGeometry(APrimitive primitive)
        {
            return primitive switch
            {
                TriangleMesh triangleMesh => triangleMesh.Mesh.TriangleCount,
                InstancedMesh instancedMesh => instancedMesh.TemplateMesh.TriangleCount,
                _ => null
            };
        }
    }

    private static BoundingBox? ExtendBoundingBoxWithChildrenBounds(
        List<CadRevealNode> children,
        BoundingBox? optionalStartingBoundingBox
    )
    {
        // Does not need to be recursive since all child are expected to have run this method already.
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
        var worldTransform = node.WorldTransform;
        var nodeName = node.GetNodeName();
        if (nodeGeometryPtr == IntPtr.Zero)
        {
            return null;
        }

        if (meshInstanceLookup.TryGetValue(nodeGeometryPtr, out var instanceData))
        {
            var instancedMeshCopy = new InstancedMesh(
                instanceData.instanceId,
                instanceData.templateMesh,
                worldTransform,
                treeIndex,
                Color.Aqua, // TODO: Temp debug color to distinguish copies of an instanced mesh
                instanceData.templateMesh.CalculateAxisAlignedBoundingBox(worldTransform)
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

        string[] nodesToMakeConvex = ["Plank", "Board", "Pipe"];
        Mesh simplifiedMesh;
        if (nodesToMakeConvex.Any(s => nodeName.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            simplifiedMesh = ReduceMeshToConvexHull(mesh);
        }
        else
        {
            simplifiedMesh = Simplify.SimplifyMeshLossy(mesh, new SimplificationLogObject(), 0.03f);
        }

        if (geometriesThatShouldBeInstanced.Contains(meshData.Value.MeshPtr))
        {
            ulong instanceId = instanceIdGenerator.GetNextId();
            meshInstanceLookup.Add(meshPtr, (simplifiedMesh, instanceId));
            var instancedMesh = new InstancedMesh(
                instanceId,
                simplifiedMesh,
                worldTransform,
                treeIndex,
                Color.Magenta, // TODO: Temp debug color to distinguish first Instance
                mesh.CalculateAxisAlignedBoundingBox(worldTransform)
            );
            Console.WriteLine(
                $"Simplification stats for mesh of node {FbxNodeWrapper.GetNodeName(node), -50}. Percent: {((float)simplifiedMesh.TriangleCount / mesh.TriangleCount), 7:P2}. Orig: {mesh.TriangleCount, 8} After: {simplifiedMesh.TriangleCount, 8}"
            );
            return instancedMesh;
        }
        else
        {
            // Apply the nodes WorldSpace transform to the mesh data, as we don't have transforms for mesh data in reveal.
            simplifiedMesh.Apply(worldTransform);
            var triangleMesh = new TriangleMesh(
                simplifiedMesh,
                treeIndex,
                Color.Yellow, // TODO: Temp debug color to distinguish un-instanced
                simplifiedMesh.CalculateAxisAlignedBoundingBox()
            );

            Console.WriteLine(
                $"Simplification stats for mesh of node {FbxNodeWrapper.GetNodeName(node), -50}. Percent: {((float)simplifiedMesh.TriangleCount / mesh.TriangleCount), 7:P2}. Orig: {mesh.TriangleCount, 8} After: {simplifiedMesh.TriangleCount, 8}"
            );
            return triangleMesh;
        }
    }

    // Some models contain trash, i.e., objects that were intended to be removed were not deleted,
    // but landed somewhere far away from the model.
    // This is likely to happen in the future according to our domain expert.
    //
    // As a consequence, the bounding box becomes very big(encompasses the trash as well) and
    // becomes unusable for the GoTo functionality.
    //
    // Our domain expert confirmed that we can(hopefully) fix this issue by ignoring all parts that
    // do now have attributes(empty fields) in the attribute file.
    private static bool ValidateNodeAttributes(Dictionary<string, Dictionary<string, string>?> attributes, string name)
    {
        var fbxNameIdRegex = new Regex(@"\[(\d+)\]");

        var match = fbxNameIdRegex.Match(name);
        if (match.Success)
        {
            var idNode = match.Groups[1].Value;

            if (attributes.ContainsKey(idNode) && attributes[idNode] == null)
            {
                Console.WriteLine("Skipping node without valid attributes: " + idNode + " : " + name);
                return false;
            }

            if (!attributes.ContainsKey(idNode))
            {
                Console.WriteLine("Skipping node without existing attributes: " + idNode + " : " + name);
                return false;
            }
        }

        return true;
    }

    private static Mesh ReduceMeshToConvexHull(Mesh mesh)
    {
        // Build vertex list to hand to the convex hull algorithm
        var meshVertexCount = mesh.Vertices.Length;
        var meshVertices = new double[meshVertexCount][];
        for (int j = 0; j < meshVertexCount; j++)
        {
            var coordinate = new double[3];
            coordinate[0] = mesh.Vertices[j].X;
            coordinate[1] = mesh.Vertices[j].Y;
            coordinate[2] = mesh.Vertices[j].Z;
            meshVertices[j] = coordinate;
        }

        const float tolerance = 0.01f; // ~1cm
        // Create the convex hull
        var convexHullOfMesh = ConvexHull.Create(meshVertices, tolerance);

        // Create the convex hull vertices and indices in CadRevealComposer internal format
        uint index = 0;
        var cadRevealVertices = new List<Vector3>();
        var cadRevealIndices = new List<uint>();
        foreach (DefaultConvexFace<DefaultVertex> face in convexHullOfMesh.Result.Faces)
        {
            var facePoints = face.Vertices;
            foreach (var vertex in facePoints)
            {
                float x = (float)vertex.Position[0];
                float y = (float)vertex.Position[1];
                float z = (float)vertex.Position[2];
                cadRevealIndices.Add(index++);
                cadRevealVertices.Add(new Vector3(x, y, z));
            }
        }

        var reducedMesh = new Mesh(cadRevealVertices.ToArray(), cadRevealIndices.ToArray(), tolerance);

        // Simplify the convex hull if needed
        reducedMesh = Simplify.SimplifyMeshLossy(reducedMesh, new SimplificationLogObject(), 0.01f);
        return reducedMesh;
    }
}
