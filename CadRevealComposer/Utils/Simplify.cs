namespace CadRevealComposer.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils.MeshOptimization;
using g3;
using MIConvexHull;
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
    /// This will reduce the quality of the mesh based on a threshold
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="simplificationLogObject"></param>
    /// <param name="thresholdInMeshUnits">Usually meters</param>
    /// <returns></returns>
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

    /// <summary>
    /// Generates a convex hull as output from a mesh input. The resulting hull consists of new
    /// points distributed on or close to the surface of the original mesh in three-dimensional space.
    /// </summary>
    /// <param name="inputMesh">The input mesh.</param>
    /// <param name="tolerance">Maximum distance that a point can be from a best-fit convex hull plane and still be considered part of that plane.</param>
    /// <returns>Mesh instance containing the generated convex hull points.</returns>
    public static Mesh ConvertToConvexHull(Mesh inputMesh, float tolerance = 1.0E-1f)
    {
        // Build vertex list to hand to the convex hull algorithm
        var meshVertices = inputMesh.Vertices.Select(v => new double[] { v.X, v.Y, v.Z });

        // Create the convex hull
        var convexHullOfMesh = ConvexHull.Create(meshVertices.ToArray(), tolerance);
        if (convexHullOfMesh.Outcome != ConvexHullCreationResultOutcome.Success)
        {
            Console.WriteLine($"Convex hull simplification failed with error : {convexHullOfMesh.ErrorMessage}");
            return inputMesh;
        }

        // Create the convex hull vertices and indices in CadRevealComposer internal format
        var cadRevealVertices = convexHullOfMesh.Result.Faces.SelectMany(face =>
            face.Vertices.Select(r => new Vector3((float)r.Position[0], (float)r.Position[1], (float)r.Position[2]))
        );
        IEnumerable<Vector3> cadRevealVerticesEnumerable = cadRevealVertices.ToArray();
        var cadRevealIndices = cadRevealVerticesEnumerable.Select((item, index) => (uint)index);

        return new Mesh(cadRevealVerticesEnumerable.ToArray(), cadRevealIndices.ToArray(), inputMesh.Error);
    }
}
