using rvmsharp.Tessellator;
using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;

namespace Equinor.MeshOptimizationPipeline
{
    public sealed class OBJExporter : IDisposable
    {
        private readonly StreamWriter _writer;
        private int _vertexCount;
        private int _normalCount;
        //private int mUvCount;
        private int _meshCount;

        public OBJExporter(string filename)
        {
            _writer = new StreamWriter(File.Create(filename), Encoding.ASCII);
        }

        public void Dispose()
        {
            _writer?.Close();
            _writer?.Dispose();
        }

        public void WriteMesh(Mesh mesh, string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = _meshCount.ToString();
            _meshCount++;
            //var faceOrder = data.FaceOrder; // TODO
            //var hasUv = false;
            _writer.WriteLine("o " + name);
            _writer.WriteLine("g " + name);
            foreach (var v in mesh.vertices)
            {
                _writer.WriteLine("v " + v.X.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Z.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Y.ToString("0.000000", CultureInfo.InvariantCulture));
            }
            foreach (var v in mesh.normals)
            {
                _writer.WriteLine("vn " + v.X.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Z.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Y.ToString("0.000000", CultureInfo.InvariantCulture));
            }
            /*foreach (var v in mesh.uv)
            {
                writer.WriteLine("vt " + v.x.ToString(CultureInfo.InvariantCulture) + " " + v.y.ToString(CultureInfo.InvariantCulture));
            }*/

            _writer.WriteLine("s off");

            var tris = mesh.indices;
            for (var t = 0; t < tris.Length; t += 3)
            {
                var i2 = tris[t] + 1;
                var v2 = _vertexCount + i2;
                var n2 = _normalCount + i2;
                //var uv2 = mUvCount + i2;

                var i1 = tris[t + 1] + 1;
                var v1 = _vertexCount + i1;
                var n1 = _normalCount + i1;
                //var uv1 = mUvCount + i1;

                var i0 = tris[t + 2] + 1;
                var v0 = _vertexCount + i0;
                var n0 = _normalCount + i0;
                //var uv0 = mUvCount + i0;
                /*if (faceOrder < 0)
                {
                mWriter.WriteLine(string.Format("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8}",
                       //                             v2, hasUv ? uv2.ToString() : "", n2,
                       //                             v1, hasUv ? uv1.ToString() : "", n1,
                       //                             v0, hasUv ? uv0.ToString() : "", n0));
                /*}
                else
                {*/
                _writer.WriteLine(string.Format("f {0}/{1}/{2} {3}/{4}/{5} {6}/{7}/{8}",
                                                v1, /*hasUv ? uv0.ToString() :*/ "", n1,
                                                v0, /*hasUv ? uv1.ToString() :*/ "", n0,
                                                v2, /*hasUv ? uv2.ToString() :*/ "", n2));
                /*}*/
            }

            _vertexCount += mesh.vertices.Length / 3;
            //if (mesh.uv != null)
            //    _uvCounter += mesh.uv.Length;
            _normalCount += mesh.normals.Length / 3;
        }
    }
}