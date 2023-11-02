namespace CadRevealComposer.Utils;

using g3;
using System;
using System.Collections.Generic;
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

        for (int vertexIndex = 0; vertexIndex < dMesh3.VertexCount; vertexIndex++)
        {
            verts[vertexIndex] = Vec3fToVec3(dMesh3.GetVertexf(vertexIndex));
        }

        var mesh = new Mesh(verts, dMesh3.Triangles().SelectMany(x => x.array).Select(x => (uint)x).ToArray(), 0.0f);
        return mesh;
    }

    public static int SimplificationBefore = 0;
    public static int SimplificationAfter = 0;
    public static int SimplificationBeforeTriangleCount = 0;
    public static int SimplificationAfterTriangleCount = 0;

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
        Interlocked.Add(ref SimplificationBeforeTriangleCount, mesh.TriangleCount);

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
            Interlocked.Add(ref SimplificationAfterTriangleCount, lastPassMesh.TriangleCount);
            return lastPassMesh;
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to optimize mesh: " + e);
            return mesh;
        }
    }
}
