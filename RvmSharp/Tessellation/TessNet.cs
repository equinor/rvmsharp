namespace RvmSharp.Tessellation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibTessDotNet;
using static Primitives.RvmFacetGroup;

public static class TessNet
{
    public class TessellateResult
    {
        public Vector3[] VertexData = [];
        public Vector3[] NormalData = [];
        public readonly List<int> Indices = [];
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

            var cv = new ContourVertex[contour.Vertices.Length];
            for (int i = 0; i < contour.Vertices.Length; i++)
            {
                var v = contour.Vertices[i];
                cv[i] = new ContourVertex(new Vec3(v.Vertex.X, v.Vertex.Y, v.Vertex.Z), v.Normal);
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

        result.VertexData = new Vector3[tess.Vertices.Length];
        result.NormalData = new Vector3[tess.Vertices.Length];

        int index = 0;
        foreach (var v in tess.Vertices)
        {
            result.VertexData[index] = new Vector3(v.Position.X, v.Position.Y, v.Position.Z);
            result.NormalData[index] = (Vector3)v.Data;
            index++;
        }

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
        var max = Array.IndexOf(weights, weights.Max());
        return data[max];
    }
}
