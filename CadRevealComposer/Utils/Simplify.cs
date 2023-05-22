namespace CadRevealComposer.Utils;

using g3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using Tessellation;

public static class Simplify
{
    private static DMesh3 ConvertMeshToDMesh3(Mesh mesh)
    {
        return DMesh3Builder.Build<
            Vector3d,
            int,
            object /* Normals */
        >(mesh.Vertices.Select(Vec3ToVec3d), mesh.Indices.Select(x => (int)x));

        // Alternative method?
        // var dMesh = new DMesh3();
        // dMesh.BeginUnsafeTrianglesInsert();
        // dMesh.BeginUnsafeVerticesInsert();
        // dMesh.TrianglesBuffer.Add(mesh.Triangles.Select(x => (int)x).ToArray());
        // dMesh.VerticesBuffer.Add(mesh.Vertices.SelectMany(x => x.AsEnumerable()).Select(x => (double)x).ToArray());
        // dMesh.NormalsBuffer.Add(mesh.Normals.SelectMany(x => x.AsEnumerable()).ToArray());
        // dMesh.EndUnsafeTrianglesInsert();
        // dMesh.EndUnsafeVerticesInsert();
        // return dMesh;
    }

    static Vector3d Vec3ToVec3d(Vector3 vec3f)
    {
        return new Vector3d(vec3f.X, vec3f.Y, vec3f.Z);
    }

    static Vector3 Vec3fToVec3(Vector3f vec3f)
    {
        return new Vector3(vec3f.x, vec3f.y, vec3f.z);
    }

    private static Mesh ConvertDMesh3ToMesh(DMesh3 dMesh3)
    {
        var verts = new Vector3[dMesh3.VertexCount];
        var normals = new Vector3[dMesh3.VertexCount];

        for (int vertexIndex = 0; vertexIndex < dMesh3.VertexCount; vertexIndex++)
        {
            verts[vertexIndex] = Vec3fToVec3(dMesh3.GetVertexf(vertexIndex));
            normals[vertexIndex] = Vec3fToVec3(dMesh3.GetVertexNormal(vertexIndex));
        }

        var mesh = new Mesh(verts, dMesh3.Triangles().SelectMany(x => x.array).Select(x => (uint)x).ToArray(), 0.0f);
        return mesh;
    }

    /// <summary>
    /// Remove re-used vertices, and remap the Triangle indices to the new unique table.
    /// Saves memory but assumes the mesh ONLY has Position and Index data and that you will not add normals later
    /// </summary>
    public static Mesh RemapDuplicatedVertices(Mesh input)
    {
        var alreadyFoundVertices = new Dictionary<Vector3, uint>();

        var newVertices = new List<Vector3>();
        var indicesCopy = input.Indices.ToArray();

        // The index in the oldVertexIndexToNewIndexRemap array is the old index, and the value is the new index. (Think of it as a dict)
        var oldVertexIndexToNewIndexRemap = new uint[input.Vertices.Length];

        for (uint i = 0; i < input.Vertices.Length; i++)
        {
            var vertex = input.Vertices[i];
            if (!alreadyFoundVertices.TryGetValue(vertex, out uint newIndex))
            {
                newIndex = (uint)newVertices.Count;
                newVertices.Add(vertex);
                alreadyFoundVertices.Add(vertex, newIndex);
            }

            oldVertexIndexToNewIndexRemap[i] = newIndex;
        }

        // Explicitly clear to unload memory as soon as possible.
        alreadyFoundVertices.Clear();

        for (int i = 0; i < indicesCopy.Length; i++)
        {
            var originalIndex = indicesCopy[i];
            var vertexIndex = oldVertexIndexToNewIndexRemap[originalIndex];
            indicesCopy[i] = vertexIndex;
        }

        return new Mesh(newVertices.ToArray(), indicesCopy, 0);
    }

    public static int SimplificationBefore = 0;
    public static int SimplificationAfter = 0;

    public static Stopwatch sw = new Stopwatch();

    /// <summary>
    /// Lossy Simplification of the Mesh.
    /// This will reduce the quality of the mesh based on a threshold
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="thresholdInMeshUnits">Usually meters</param>
    /// <returns></returns>
    public static Mesh SimplifyMeshLossy(Mesh mesh, float thresholdInMeshUnits = 0.01f)
    {
        Interlocked.Add(ref SimplificationBefore, mesh.Vertices.Length);

        MeshTools.MeshTools.ReducePrecisionInPlace(mesh);

        var meshCopy = MeshTools.MeshTools.OptimizeMesh(mesh);

        var dMesh = ConvertMeshToDMesh3(meshCopy);

        var reducer = new Reducer(dMesh)
        {
#if DEBUG
            // Remark, veery slow Consider enabling if needed
            // ENABLE_DEBUG_CHECKS = true,
#endif
            MinimizeQuadricPositionError = true,
            PreserveBoundaryShape = true,
            AllowCollapseFixedVertsWithSameSetID = true,
        };

        try
        {
            reducer.ReduceToEdgeLength(thresholdInMeshUnits);
            // Remove optimized stuff from the mesh. This is important or the exporter will fail.
            if (!dMesh.IsCompact)
                dMesh.CompactInPlace();

            var reducedMesh = ConvertDMesh3ToMesh(dMesh);
            var lastPassMesh = MeshTools.MeshTools.OptimizeMesh(reducedMesh);
            Interlocked.Add(ref SimplificationAfter, lastPassMesh.Vertices.Length);
            return lastPassMesh;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to optimize mesh: " + e);
            return mesh;
        }
    }
}
