namespace RvmSharp.Exporters
{
    using Operations;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Tessellation;

    public sealed class ObjExporter : IDisposable
    {
        private readonly StreamWriter _writer;
        private int _vertexCount;
        private int _normalCount;

        public ObjExporter(string filename)
        {
            _writer = new StreamWriter(File.Create(filename), Encoding.ASCII);
        }

        public void Dispose()
        {
            _writer.Close();
            _writer.Dispose();
        }

        public void StartGroup(string name)
        {
            _writer.WriteLine("g " + name);
        }

        public void StartObject(string name)
        {
            _writer.WriteLine("o " + name);
        }

        /// <summary>
        /// Add a mesh to the current Object.
        /// </summary>
        /// <param name="mesh">The mesh to serialize</param>
        public void WriteMesh(Mesh mesh)
        {
            foreach (var vertex in mesh.Vertices)
            {
                _writer.WriteLine($"v {FastToString(vertex.X)} {FastToString(vertex.Z)} {FastToString(-vertex.Y)}");
            }

            foreach (var normal in mesh.Normals)
            {
                _writer.WriteLine($"vn {FastToString(normal.X)} {FastToString(normal.Z)} {FastToString(-normal.Y)}");
            }

            _writer.WriteLine("s off");

            var tris = mesh.Triangles;
            for (var t = 0; t < tris.Count; t += 3)
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

            _vertexCount += mesh.Vertices.Count;
            _normalCount += mesh.Normals.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string FastToString(float number)
        {
            if (!number.IsFinite())
            {
                // This is a development guard. Usually the tessellation needs improvement.
                // Need to figure out a nice way to handle this.
                // Consider ignoring. or serializing as "NaN"?
                throw new ArgumentOutOfRangeException(nameof(number), $"Expected {nameof(number)} to be finite. Was {number}.");
            }

            // Using Math.Round, and Decimal instead of "float.ToString("0.000000") as it is roughly 100% faster,
            // and produces (within our tolerances) identical results. And avoids E notation.
            // This is potentially lossy, but produces as-good or better results than float.ToString(0.000000)
            // in average, and with lower "max" differences.
            const int significantFigures = 6; // Arbitrary-ish, as the rounding here is not perfect.
            return Convert.ToDecimal(Math.Round(number, significantFigures)).ToString(CultureInfo.InvariantCulture);
        }
    }
}