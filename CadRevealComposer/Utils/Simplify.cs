namespace CadRevealComposer.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using System.Threading;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils.MeshOptimization;
using g3;
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
        var vertices = new Vector3[dMesh3.VertexCount];

        for (int vertexIndex = 0; vertexIndex < dMesh3.VertexCount; vertexIndex++)
        {
            vertices[vertexIndex] = Vec3fToVec3(dMesh3.GetVertexf(vertexIndex));
        }

        var mesh = new Mesh(vertices, dMesh3.Triangles().SelectMany(x => x.array).Select(x => (uint)x).ToArray(), 0.0f);
        return mesh;
    }

    /// <summary>
    /// Lossy Simplification of the Mesh.
    /// This will reduce the quality of the mesh based on a threshold. It does not modify the original mesh object, so use the returned value.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="simplificationLogObject"></param>
    /// <param name="thresholdInMeshUnits">Usually meters</param>
    /// <returns>A simplified Copy of the mesh.</returns>
    [Pure]
    public static Mesh SimplifyMeshLossy(
        Mesh mesh,
        SimplificationLogObject simplificationLogObject,
        float thresholdInMeshUnits = 0.01f
    )
    {
        MeshTools.ReducePrecisionInPlace(mesh);

        var meshCopy = MeshTools.OptimizeMesh(mesh);

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
            var lastPassMesh = MeshTools.OptimizeMesh(reducedMesh);
            return lastPassMesh;
        }
        catch (Exception)
        {
            Interlocked.Add(ref simplificationLogObject.FailedOptimizations, 1);
            return mesh;
        }
    }

    public static List<APrimitive> OptimizeVertexCountInMeshes(IEnumerable<APrimitive> geometriesToProcess)
    {
        var meshCount = 0;
        var beforeOptimizationTotalVertices = 0;
        var afterOptimizationTotalVertices = 0;
        var timer = Stopwatch.StartNew();
        // Optimize TriangleMesh meshes for least memory use
        var processedGeometries = geometriesToProcess
            .AsParallel()
            .AsOrdered()
            .Select(primitive =>
            {
                if (primitive is not TriangleMesh triangleMesh)
                {
                    return primitive;
                }

                Mesh newMesh = MeshTools.DeduplicateVertices(triangleMesh.Mesh);
                Interlocked.Increment(ref meshCount);
                Interlocked.Add(ref beforeOptimizationTotalVertices, triangleMesh.Mesh.Vertices.Length);
                Interlocked.Add(ref afterOptimizationTotalVertices, newMesh.Vertices.Length);
                return triangleMesh with { Mesh = newMesh };
            })
            .ToList();

        using (new TeamCityLogBlock("Vertex Dedupe Stats"))
        {
            Console.WriteLine(
                $"Vertice Dedupe Stats (Vertex Count) for {meshCount} meshes:\nBefore: {beforeOptimizationTotalVertices, 11}\nAfter:  {afterOptimizationTotalVertices, 11}\nPercent: {(float)afterOptimizationTotalVertices / beforeOptimizationTotalVertices, 11:P2}\nTime: {timer.Elapsed}"
            );
        }

        return processedGeometries;
    }
}
