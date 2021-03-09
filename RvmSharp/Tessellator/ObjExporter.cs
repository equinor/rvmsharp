using rvmsharp.Tessellator;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Equinor.MeshOptimizationPipeline
{
    public sealed class OBJExporter : IDisposable
    {
        private readonly StreamWriter _writer;
        private int _vertexCount;
        private int _normalCount;
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
            _writer.WriteLine("o " + name);
            _writer.WriteLine("g " + name);
            foreach (var v in mesh.Vertices)
            {
                _writer.WriteLine("v " + v.X.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Z.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Y.ToString("0.000000", CultureInfo.InvariantCulture));
            }
            foreach (var v in mesh.Normals)
            {
                _writer.WriteLine("vn " + v.X.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Z.ToString("0.000000", CultureInfo.InvariantCulture) + " " + v.Y.ToString("0.000000", CultureInfo.InvariantCulture));
            }

            _writer.WriteLine("s off");

            var tris = mesh.Triangles;
            for (var t = 0; t < tris.Length; t += 3)
            {
                var i2 = tris[t] + 1;
                var v2 = _vertexCount + i2;
                var n2 = _normalCount + i2;

                var i1 = tris[t + 1] + 1;
                var v1 = _vertexCount + i1;
                var n1 = _normalCount + i1;

                var i0 = tris[t + 2] + 1;
                var v0 = _vertexCount + i0;
                var n0 = _normalCount + i0;
                _writer.WriteLine($"f {v1}//{n1} {v0}//{n0} {v2}//{n2}");
            }

            _vertexCount += mesh.Vertices.Length / 3;
            _normalCount += mesh.Normals.Length / 3;
        }
    }
}