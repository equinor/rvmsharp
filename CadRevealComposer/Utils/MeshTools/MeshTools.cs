namespace CadRevealComposer.Utils.MeshTools;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using Tessellation;

public static class MeshTools
{
    /// <summary>
    /// Reduce a meshes precision (in place) (to easier do vertex squashing later)
    /// </summary>
    public static void ReducePrecisionInPlace(Mesh mesh, int precisionDigits = 5)
    {
        void Round(ref Vector3 v)
        {
            v.X = MathF.Round(v.X, precisionDigits);
            v.Y = MathF.Round(v.Y, precisionDigits);
            v.Z = MathF.Round(v.Z, precisionDigits);
        }

        for (int index = 0; index < mesh.Vertices.Length; index++)
        {
            Round(ref mesh.Vertices[index]);
        }
    }

    /// <summary>
    /// Remove re-used vertices, and remap the Triangle indices to the new unique table.
    /// Saves memory but assumes the mesh ONLY has Position and Index data. Discards any normals!
    ///
    /// Returns a new Mesh
    /// </summary>
    [Pure]
    public static Mesh DeduplicateVertices(Mesh input)
    {
        var newVertices = input.Vertices.ToArray();
        var indicesCopy = input.Indices.ToArray();
        DeduplicateVerticesInPlace(ref newVertices, ref indicesCopy);
        return new Mesh(newVertices, indicesCopy, 0);
    }

    /// <summary>
    /// Remove re-used vertices, and remap the Triangle indices to the new unique table.
    /// Saves memory but assumes the mesh ONLY has Position and Index data. Discards any normals!
    /// </summary>
    private static void DeduplicateVerticesInPlace<T>(
        ref T[] vertices,
        ref uint[] indices,
        IEqualityComparer<T>? equalityComparer = null
    )
        where T : notnull
    {
        var alreadyFoundVerticesToIndexMap = new Dictionary<T, uint>(equalityComparer);

        var newVertices = new List<T>();

        // The index in the oldVertexIndexToNewIndexRemap array is the old index, and the value is the new index. (Think of it as a dict)
        var oldVertexIndexToNewIndexRemap = new uint[vertices.Length];

        for (uint i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[(int)i];
            if (!alreadyFoundVerticesToIndexMap.TryGetValue(vertex, out uint newIndex))
            {
                newIndex = (uint)newVertices.Count;
                newVertices.Add(vertex);
                alreadyFoundVerticesToIndexMap.Add(vertex, newIndex);
            }

            oldVertexIndexToNewIndexRemap[i] = newIndex;
        }
        // Explicitly clear to unload memory as soon as possible.
        alreadyFoundVerticesToIndexMap.Clear();

        for (int i = 0; i < indices.Length; i++)
        {
            var originalIndex = indices[i];
            var vertexIndex = oldVertexIndexToNewIndexRemap[originalIndex];
            indices[i] = vertexIndex;
        }

        Array.Resize(ref vertices, newVertices.Count);
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] = newVertices[i];
        }
    }

    /// <summary>
    /// <inheritdoc cref="OptimizeInPlace{T}" type="/summary"/>
    /// Returns a new mesh.
    /// </summary>
    public static Mesh OptimizeMesh(Mesh m)
    {
        var indices = m.Indices.ToArray();
        var verts = m.Vertices.ToArray();
        OptimizeInPlace(ref verts, ref indices);
        return new Mesh(verts, indices, m.Error);
    }

    /// <summary>
    /// Optimizes for best rendering performance without losing precision.
    /// Removes duplicate vertices, reorders indices and vertices, and optimizes for GPU cache.
    ///
    /// Probably a minor improvement to rendering performance for most meshes, but usually really quick!
    /// </summary>
    private static void OptimizeInPlace<T>(
        ref T[] vertices,
        ref uint[] indices,
        IEqualityComparer<T>? vertexComparer = null
    )
        where T : notnull
    {
        DeduplicateVerticesInPlace(ref vertices, ref indices, vertexComparer);
        VertexCacheOptimizer.OptimizeVertexCache(indices, indices, (uint)vertices.Length);
        VertexFetchOptimizer.OptimizeVertexFetch(vertices.AsSpan(), indices, vertices);
    }
}
