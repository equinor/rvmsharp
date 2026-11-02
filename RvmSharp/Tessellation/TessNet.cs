namespace RvmSharp.Tessellation;

using System;
using System.Linq;
using System.Numerics;
using LibTessDotNet;
using static Primitives.RvmFacetGroup;

public static class TessNet
{
    public class TessellateResult
    {
        public Vector3[] VertexData = Array.Empty<Vector3>();
        public Vector3[] NormalData = Array.Empty<Vector3>();
        public int[] Indices = Array.Empty<int>();
    }

    public static TessellateResult Tessellate(ArraySegment<RvmContour> contours, Vector3 origin = default)
    {
        var tess = new Tess();
        Vec3 normal = default;
        bool shouldTessellate = false;

        foreach (var contour in contours)
        {
            if (contour.Vertices.Count < 3)
            {
                // Skip degenerate contour with less than 3 vertices
                continue;
            }

            // Recenter for float precision while building LibTess's input, avoiding translated contour copies.
            // The caller must add origin back to the output positions; shared input vertices remain unchanged.
            var cv = new ContourVertex[contour.Vertices.Count];
            for (var vertexIndex = 0; vertexIndex < cv.Length; vertexIndex++)
            {
                var (vertex, vertexNormal) = contour.Vertices[vertexIndex];
                var position = vertex - origin;
                cv[vertexIndex] = new ContourVertex(new Vec3(position.X, position.Y, position.Z), vertexNormal);
            }
            tess.AddContour(cv);
            var n = contour.Vertices[0].Normal;
            normal = new Vec3(n.X, n.Y, n.Z);
            shouldTessellate = true;
        }

        if (!shouldTessellate)
            return new TessellateResult();

        var result = new TessellateResult();

        tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, CombineNormals, normal);
        result.VertexData = tess.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)).ToArray();
        result.NormalData = tess.Vertices.Select(v => (Vector3)v.Data).ToArray();

        var indices = new int[tess.ElementCount * 3];
        var indexCount = 0;
        for (var elementIndex = 0; elementIndex < tess.ElementCount; elementIndex++)
        {
            var offset = elementIndex * 3;
            var first = tess.Elements[offset];
            var second = tess.Elements[offset + 1];
            var third = tess.Elements[offset + 2];
            if (first == Tess.Undef || second == Tess.Undef || third == Tess.Undef)
                continue;
            indices[indexCount++] = first;
            indices[indexCount++] = second;
            indices[indexCount++] = third;
        }

        if (indexCount != indices.Length) // If we had undefined elements, resize the array to the actual number of valid indices
            Array.Resize(ref indices, indexCount);
        result.Indices = indices;

        return result;
    }

    private static object CombineNormals(Vec3 position, object[] data, float[] weights)
    {
        var max = weights.Select((w, i) => (w, i)).OrderByDescending(p => p.w).Select(p => p.i).First();
        return data[max];
    }
}
