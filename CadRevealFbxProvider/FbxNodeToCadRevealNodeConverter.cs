namespace CadRevealFbxProvider;

using System.Text.RegularExpressions;
using BatchUtils;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using CadRevealFbxProvider.UserFriendlyLogger;
using Commons.Utils;

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

        var cadRevealNode = new CadRevealNode
        {
            TreeIndex = id,
            Name = name,
            Parent = parent,
            Geometries = geometry != null ? [geometry] : [],
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
        uint treeIndex,
        FbxNode node,
        InstanceIdGenerator instanceIdGenerator,
        IDictionary<IntPtr, (Mesh templateMesh, ulong instanceId)> meshInstanceLookup,
        IReadOnlySet<IntPtr> geometriesThatShouldBeInstanced
    )
    {
        var nodeGeometryPtr = FbxMeshWrapper.GetMeshGeometryPtr(node);
        if (nodeGeometryPtr == IntPtr.Zero)
        {
            return null;
        }

        var meshTransform = node.WorldGeometricTransform;
        if (!meshTransform.IsDecomposable())
        {
            Console.Error.WriteLine(
                "Failed to decompose transform for node: "
                    + node.GetNodeName()
                    + ". (ignoring). Had transform "
                    + meshTransform
            );
            return null;
        }
        var color = FbxMaterialWrapper.GetMaterialColor(node);

        if (meshInstanceLookup.TryGetValue(nodeGeometryPtr, out var instanceData))
        {
            var instancedMeshCopy = new InstancedMesh(
                instanceData.instanceId,
                instanceData.templateMesh,
                meshTransform,
                treeIndex,
                color,
                instanceData.templateMesh.CalculateAxisAlignedBoundingBox(meshTransform)
            );
            return instancedMeshCopy;
        }

        var mesh = FbxMeshWrapper.GetGeometricData(nodeGeometryPtr);
        if (mesh == null)
        {
            throw new UserFriendlyLogException(
                "Import of the FBX file failed. Did the FBX export report any issues?",
                new Exception("IntPtr" + nodeGeometryPtr + " was expected to have a mesh, but we found none.")
            );
        }

        if (geometriesThatShouldBeInstanced.Contains(nodeGeometryPtr))
        {
            ulong instanceId = instanceIdGenerator.GetNextId();
            meshInstanceLookup.Add(nodeGeometryPtr, (mesh, instanceId));
            var instancedMesh = new InstancedMesh(
                instanceId,
                mesh,
                meshTransform,
                treeIndex,
                color,
                mesh.CalculateAxisAlignedBoundingBox(meshTransform)
            );
            return instancedMesh;
        }

        if (mesh.Vertices.Length == 0)
        {
            Console.Error.WriteLine("Found mesh with zero vertices: " + node.GetNodeName() + ". (ignoring). ");
            return null;
        }

        // Apply the nodes WorldSpace transform to the mesh data, as we don't have transforms for mesh data in reveal.
        mesh.Apply(meshTransform);
        var triangleMesh = new TriangleMesh(mesh, treeIndex, color, mesh.CalculateAxisAlignedBoundingBox());

        return triangleMesh;
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
}
