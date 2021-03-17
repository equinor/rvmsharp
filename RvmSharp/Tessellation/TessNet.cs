using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static RvmSharp.Primitives.RvmFacetGroup;

namespace RvmSharp.Tessellation
{
    class TessNet
    {
        public class TessellateResult
        {
            public Vector3[] VertexData = Array.Empty<Vector3>();
            public Vector3[] NormalData = Array.Empty<Vector3>();
            public List<int> Indices = new List<int>();
        }

        public static TessellateResult Tessellate(RvmContour[] contours)
        {
            var result = new TessellateResult();
            var tess = new Tess();
            bool shouldTessellate = false;

            foreach (var contour in contours)
            {
                if (contour.Vertices.Length < 3)
                {
                    // Skip degenerate contour with less than 3 vertices
                    continue;
                }
                var cv = contour.Vertices.Select(v => new ContourVertex(new Vec3(v.v.X, v.v.Y, v.v.Z), v.n)).ToArray();
                tess.AddContour(cv);

                shouldTessellate = true;
            }

            if (shouldTessellate)
            {
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, VertexCombine);
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
            }
            return result;
        }

        private static object VertexCombine(Vec3 position, object[] normals, float[] weights)
        {
            var max = weights.Select((w, i) => (w, i)).OrderByDescending(p => p.w).Select(p => p.i).First();
            return normals[max];
        }
    }
}
