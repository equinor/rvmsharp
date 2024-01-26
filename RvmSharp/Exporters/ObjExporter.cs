namespace RvmSharp.Exporters;

using Commons.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Tessellation;

public sealed class ObjExporter : IDisposable
{
    private readonly string _filename;
    private readonly StreamWriter _writer;
    private StreamWriter? _materialWriter;
    private readonly Dictionary<Color, string> _colors = new();
    private int _vertexCount;
    private int _normalCount;

    public ObjExporter(string filename)
    {
        _filename = filename;

        _writer = new StreamWriter(File.Create(filename), Encoding.ASCII);
        _writer.WriteLine("# rvmsharp OBJ export");
    }

    public void Dispose()
    {
        _writer.Close();
        _writer.Dispose();
        _materialWriter?.Close();
        _materialWriter?.Dispose();
    }

    public void StartGroup(string name)
    {
        _writer.WriteLine("g " + name);
    }

    public void StartMaterial(Color color)
    {
        if (_materialWriter == null)
        {
            var materialFullFilename = Path.ChangeExtension(_filename, "mtl");
            var materialFilename = Path.GetFileName(materialFullFilename);
            _writer.WriteLine($"mtllib {materialFilename}");
            _materialWriter = new StreamWriter(File.Create(materialFullFilename), Encoding.ASCII);
        }
        if (!_colors.TryGetValue(color, out var materialName))
        {
            // for complete list of parameters and explanation see http://paulbourke.net/dataformats/mtl/
            materialName = $"material_{_colors.Count + 1}";
            _colors.Add(color, materialName);
            _materialWriter.WriteLine($"newmtl {materialName}");
            // Ambient color
            _materialWriter.WriteLine("Ka 0.000000 0.000000 0.000000");
            // Diffuse color
            _materialWriter.WriteLine(
                $"Kd {FastToString((float)color.R / 255)} {FastToString((float)color.G / 255)} {FastToString((float)color.B / 255)}"
            );
            // Specular color
            _materialWriter.WriteLine("Ks 0.000000 0.000000 0.000000");
            // Emission color
            _materialWriter.WriteLine("Ke 0.000000 0.000000 0.000000");
            // Shininess parameter
            _materialWriter.WriteLine("Ns 1.000000");
            // Index of refraction
            _materialWriter.WriteLine("Ni 1.000000");
            // Dissolve (alpha)
            _materialWriter.WriteLine("d 1.000000");
            // Illumination model
            _materialWriter.WriteLine("illum 1");
        }
        _writer.WriteLine($"usemtl {materialName}");
    }

    public void StartObject(string name)
    {
        _writer.WriteLine("o " + name);
    }

    /// <summary>
    /// Add a mesh to the current Object.
    /// </summary>
    /// <param name="mesh">The mesh to serialize</param>
    public void WriteMesh(RvmMesh mesh)
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

        _vertexCount += mesh.Vertices.Length;
        _normalCount += mesh.Normals.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FastToString(float number)
    {
        if (!number.IsFinite())
        {
            // This is a development guard. Usually the tessellation needs improvement.
            // Need to figure out a nice way to handle this.
            // Consider ignoring. or serializing as "NaN"?
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"Expected {nameof(number)} to be finite. Was {number}."
            );
        }

        // Using Math.Round, and Decimal instead of "float.ToString("0.000000") as it is roughly 100% faster,
        // and produces (within our tolerances) identical results. And avoids E notation.
        // This is potentially lossy, but produces as-good or better results than float.ToString(0.000000)
        // in average, and with lower "max" differences.
        const int significantFigures = 6; // Arbitrary-ish, as the rounding here is not perfect.
        return Convert.ToDecimal(Math.Round(number, significantFigures)).ToString(CultureInfo.InvariantCulture);
    }
}
