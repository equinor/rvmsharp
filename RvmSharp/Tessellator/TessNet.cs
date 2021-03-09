using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static RvmSharp.Primitives.RvmFacetGroup;

namespace rvmsharp.Tessellator
{
    class TessNet
    {
        public class JobData
        {
            public Vector3[] VertexData = Array.Empty<Vector3>();
            public Vector3[] NormalData = Array.Empty<Vector3>();
            public List<int> Indices = new List<int>();
        }

        public static JobData Tessellate(RvmContour[] contours)
        {
            var Job = new JobData();
            var tess = new LibTessDotNet.Tess();
            bool bContourFound = false;

            foreach (var contour in contours)
            {
                if (contour._vertices.Length < 3)
                {
                    // Skip degenerate contour with less than 3 vertices
                    continue;
                }
                var cv = contour._vertices.Select(v => new ContourVertex(new Vec3(v.v.X, v.v.Y, v.v.Z), v.n)).ToArray();
                tess.AddContour(cv);

                bContourFound = true;
            }

            if (bContourFound)
            {
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, VertexCombine);
                int TessellatedVertexParameterCount = tess.VertexCount;
                Job.VertexData = tess.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, v.Position.Z)).ToArray();
                Job.NormalData = tess.Vertices.Select(v => (Vector3)v.Data).ToArray();

                for (var i = 0; i < tess.ElementCount; i++)
                {
                    var t = new int[3];
                    Array.Copy(tess.Elements, i * 3, t, 0, 3);
                    if (t.Any(e => e == Tess.Undef))
                        continue;
                    Job.Indices.AddRange(t);
                }
            }
            return Job;
        }

        private static object VertexCombine(Vec3 position, object[] data, float[] weights)
        {
            var max = weights.Select((w, i) => (w, i)).OrderByDescending(p => p.w).Select(p => p.i).First();
            return data[max];
        }
    }
}
