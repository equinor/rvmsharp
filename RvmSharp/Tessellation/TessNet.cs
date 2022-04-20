using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static RvmSharp.Primitives.RvmFacetGroup;

namespace RvmSharp.Tessellation;

public static class TessNet
{
    public class TessellateResult
    {
        public Vector3[] VertexData = Array.Empty<Vector3>();
        public Vector3[] NormalData = Array.Empty<Vector3>();
        public readonly List<int> Indices = new List<int>();
    }

    public static TessellateResult Tessellate(RvmContour[] contours)
    {
        var tess = new Tess();
        Vec3 normal = default;
        bool shouldTessellate = false;

        foreach (var contour in contours)
        {
            if (contour.Vertices.Length < 3)
            {
                // Skip degenerate contour with less than 3 vertices
                continue;
            }
            var cv = contour.Vertices.Select(v => new ContourVertex(new Vec3(v.Vertex.X, v.Vertex.Y, v.Vertex.Z), v.Normal)).ToArray();
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

        for (var i = 0; i < tess.ElementCount; i++)
        {
            var t = new int[3];
            Array.Copy(tess.Elements, i * 3, t, 0, 3);
            if (t.Any(e => e == Tess.Undef))
                continue;
            result.Indices.AddRange(t);
        }
        return result;
    }

    private static object CombineNormals(Vec3 position, object[] data, float[] weights)
    {
        var max = weights.Select((w, i) => (w, i)).OrderByDescending(p => p.w).Select(p => p.i).First();
        return data[max];
    }
}