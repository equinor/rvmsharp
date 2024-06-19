namespace CadRevealFbxProvider;

using BatchUtils;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using MIConvexHull;
using System.Drawing;
using System.Numerics;
using System.Text.RegularExpressions;

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
        var name = FbxNodeWrapper.GetNodeName(node);
        if (nodeNameFiltering.ShouldExcludeNode(name))
            return null;

        var id = treeIndexGenerator.GetNextId();
        var geometry = ReadGeometry(id, node, instanceIdGenerator, meshInstanceLookup, geometriesThatShouldBeInstanced);

        if (attributes != null)
            if (!validateNodeAttributes(attributes, name))
                return null;

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
        SimplificationLogObject simplificationLogObject = new();
        Mesh simplifiedMesh = Simplify.SimplifyMeshLossy(mesh, simplificationLogObject, 0.03f);

        if (geometriesThatShouldBeInstanced.Contains(meshData.Value.MeshPtr))
        {
            ulong instanceId = instanceIdGenerator.GetNextId();
            meshInstanceLookup.Add(meshPtr, (simplifiedMesh, instanceId));
            var instancedMesh = new InstancedMesh(
                instanceId,
                simplifiedMesh,
                transform,
                treeIndex,
                Color.Magenta, // TODO: Temp debug color to distinguish first Instance
                bb
            );
            Console.WriteLine(
                $"Simplification stats for mesh of node {FbxNodeWrapper.GetNodeName(node), -50}. Percent: {((float)simplifiedMesh.TriangleCount / mesh.TriangleCount):P2}. Orig: {mesh.TriangleCount, 8} After: {simplifiedMesh.TriangleCount, 8}"
            );
            return instancedMesh;
        }

        var triangleMesh = new TriangleMesh(
            simplifiedMesh,
            treeIndex,
            Color.Yellow, // TODO: Temp debug color to distinguish un-instanced
            bb
        );

        Console.WriteLine(
            $"Simplification stats for mesh of node {FbxNodeWrapper.GetNodeName(node), -50}. Percent: {((float)simplifiedMesh.TriangleCount / mesh.TriangleCount):P2}. Orig: {mesh.TriangleCount, 8} After: {simplifiedMesh.TriangleCount, 8}"
        );
        return triangleMesh;
    }

    // Some models contain trash, i.e., objects that were intended to be removed were not deleted,
    // but landed somewhere far away from the model).
    // This is likely to happen in the future according to our domain expert.
    //
    // As a consequence, the bounding box becomes very big(encompasses the trash as well) and
    // becomes unusable for the GoTo functionality.
    //
    // Our domain expert confirmed that we can(hopefully) fix this issue by ignoring all parts that
    // do now have attributes(empty fields) in the attribute file.
    private static bool validateNodeAttributes(Dictionary<string, Dictionary<string, string>?> attributes, string name)
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

    public static APrimitive[] ConvertToConvexHull(APrimitive[] geometries, bool onlyOptimizeSmallVolumes)
    {
        // TODO: Move me to separate file!
        APrimitive[] geometriesOut = new APrimitive[geometries.Length];
        int triangleCount = 0;
        for (int i = 0; i < geometries.Length; i++)
        {
            var tG = geometries[i];
            Console.WriteLine($"Type is {tG.GetType()}");
            if (tG is InstancedMesh)
            {
                var t = (InstancedMesh)tG;
                var originalMeshCount = t.TemplateMesh.TriangleCount;

                Mesh reducedMesh;

                BoundingBox bbox = t.TemplateMesh.CalculateAxisAlignedBoundingBox(Matrix4x4.Identity);
                float dX = bbox.Max.X - bbox.Min.X;
                float dY = bbox.Max.Y - bbox.Min.Y;
                float dZ = bbox.Max.Z - bbox.Min.Z;
                float V = dX * dY * dZ;
                if (V < 8.0f || !onlyOptimizeSmallVolumes)
                {
                    reducedMesh = ReduceMeshToConvexHull(t.TemplateMesh);
                    Console.WriteLine($"{t.GetType().Name}: {t.TemplateMesh.Vertices.Length}");
                    Console.WriteLine($"Reduced from {originalMeshCount} to {reducedMesh.TriangleCount}");

                    triangleCount += reducedMesh.TriangleCount;
                }
                else
                {
                    var t2 = (InstancedMesh)geometries[i];
                    reducedMesh = t2.TemplateMesh;

                    /*
                    // The below code does not work due to failure of the first CheckValidity()
                    var meshCopy = MeshTools.OptimizeMesh(t2.TemplateMesh);
                    var dMesh = ConvertMeshToDMesh3(meshCopy);
                    dMesh.CheckValidity();
                    var reducer = new Reducer(dMesh);
                    reducer.ReduceToTriangleCount(50);
                    dMesh.CheckValidity();
                    reducedMesh = ConvertDMesh3ToMesh(dMesh);
                    */

                    reducedMesh = Simplify.SimplifyMeshLossy(reducedMesh, new SimplificationLogObject(), 0.05f);

                    triangleCount += reducedMesh.TriangleCount;
                }

                // Replace the instanced mesh
                geometriesOut[i] = new InstancedMesh(
                    t.InstanceId,
                    reducedMesh,
                    t.InstanceMatrix,
                    t.TreeIndex,
                    t.Color,
                    t.AxisAlignedBoundingBox
                );
            }
            else if (tG is TriangleMesh)
            {
                var t = (TriangleMesh)tG;
                t = t with { Mesh = ReduceMeshToConvexHull(t.Mesh) };
                triangleCount += t.Mesh.TriangleCount;
                geometriesOut[i] = geometries[i];
            }
            else
            {
                geometriesOut[i] = geometries[i];
            }
        }

        Console.WriteLine($"TriCount: {triangleCount}");
        return geometriesOut;
    }

    private static Mesh ReduceMeshToConvexHull(Mesh mesh)
    { // Build vertex list to hand to the convex hull algorithm
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
            var facePoints = face.Vertices.ToArray();
            foreach (var r in facePoints)
            {
                double x = r.Position[0];
                double y = r.Position[1];
                double z = r.Position[2];
                cadRevealIndices.Add(index++);
                cadRevealVertices.Add(new Vector3((float)x, (float)y, (float)z));
            }
        }

        var reducedMesh = new Mesh(cadRevealVertices.ToArray(), cadRevealIndices.ToArray(), tolerance);

        reducedMesh = Simplify.SimplifyMeshLossy(reducedMesh, new SimplificationLogObject(), 0.03f);
        return reducedMesh;
    }
}
