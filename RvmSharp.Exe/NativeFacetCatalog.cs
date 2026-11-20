namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Containers;
using Primitives;
using Tessellation;

/// <summary>
/// A model-wide library of facet-group templates. RVM files carry no instancing, so the catalog
/// is built once over every source and each per-file export then looks its facet groups up by
/// position. Equal parts in different files therefore share one template id.
/// </summary>
/// <remarks>
/// Facet groups are numbered in depth-first order, a node's own groups before its child nodes,
/// without regard to attribute exclusions; the export must number them the same way.
/// </remarks>
internal static class NativeFacetCatalog
{
    /// <summary>Tolerance for reducing one file to classes; the rest of the budget is spent across files.</summary>
    internal const double PerFileTolerance = 0.00002;

    internal const double DefaultTolerance = 0.0001;

    private const int FormatVersion = 1;
    private const uint SliceMagic = 0x4C534652; // "RFSL"
    private const uint IndexMagic = 0x49544652; // "RFTI"
    private const string IndexFileName = "facet-templates.idx";
    private const string DataFileName = "facet-templates.bin";

    internal readonly record struct Placement(int TemplateIndex, Matrix4x4 Matrix);

    internal static List<RvmFacetGroup> EnumerateFacetGroups(RvmFile rvmFile)
    {
        var facetGroups = new List<RvmFacetGroup>();
        foreach (var rootChild in rvmFile.Model.Children)
            Collect(rootChild);
        return facetGroups;

        void Collect(RvmGroup group)
        {
            if (group is not RvmNode node)
                return;
            facetGroups.AddRange(node.Children.OfType<RvmFacetGroup>());
            foreach (var child in node.Children.OfType<RvmNode>())
                Collect(child);
        }
    }

    internal static string SourceId(string rvmFilename) => Path.GetFileNameWithoutExtension(rvmFilename);

    internal static void Build(IReadOnlyList<string> rvmFilenames, string directory, double tolerance)
    {
        if (!(tolerance > PerFileTolerance))
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                $"Facet tolerance must exceed {PerFileTolerance.ToString(CultureInfo.InvariantCulture)} m."
            );
        var filenames = rvmFilenames.OrderBy(SourceId, StringComparer.Ordinal).ToArray();
        var duplicate = filenames
            .GroupBy(SourceId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            throw new ArgumentException($"Catalog sources need unique file names: {duplicate.Key}");

        var timer = Stopwatch.StartNew();
        var parallelism = Math.Max(1, Environment.ProcessorCount / 2);
        var sources = new Source[filenames.Length];
        var completed = 0;
        Parallel.For(
            0,
            filenames.Length,
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            index =>
            {
                sources[index] = ReadSource(index, filenames[index]);
                var done = Interlocked.Increment(ref completed);
                if (done % 25 == 0 || done == filenames.Length)
                    Console.WriteLine(
                        $"Catalog: read {done}/{filenames.Length} sources ({timer.Elapsed.TotalSeconds:F0}s)"
                    );
            }
        );

        var classes = NativeFacetMatcher.Cluster(
            sources.SelectMany(source => source.Representatives).ToList(),
            FacetMatchMode.Rigid,
            tolerance - PerFileTolerance,
            parallelism
        );
        Console.WriteLine(
            $"Catalog: {classes.Count:N0} templates for "
                + $"{sources.Sum(source => (long)source.Catalogued):N0} facet groups ({timer.Elapsed.TotalSeconds:F0}s)"
        );

        var slices = sources
            .Select(source => new (int Template, Linear3 Linear, Vector3d Center)[source.FacetGroups])
            .ToArray();
        foreach (var slice in slices)
            Array.Fill(slice, (-1, Linear3.Identity, default));
        var uses = new long[classes.Count];
        var sourceCounts = new int[classes.Count];
        double worst = 0;
        for (var template = 0; template < classes.Count; template++)
        {
            var members = classes[template].Members;
            sourceCounts[template] = members.Select(member => member.Shape.Source).Distinct().Count();
            foreach (var (shape, fit) in members)
            {
                uses[template] += shape.Weight;
                foreach (var member in sources[shape.Source].Members[shape.Ordinal])
                {
                    slices[shape.Source][member.Ordinal] = (template, member.Linear * fit.Linear, member.Center);
                    worst = Math.Max(worst, member.Residual + fit.MaxResidual);
                }
            }
        }

        var meshes = new RvmMesh[classes.Count];
        Parallel.For(
            0,
            classes.Count,
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            template =>
            {
                var representative = classes[template].Representative;
                meshes[template] = Tessellate(
                    sources[representative.Source].Groups[representative.Ordinal],
                    representative
                );
            }
        );

        Directory.CreateDirectory(directory);
        var keys = WriteTemplates(directory, meshes);
        for (var index = 0; index < sources.Length; index++)
            WriteSlice(Path.Combine(directory, sources[index].SourceId + ".rvm-facets.bin"), slices[index]);

        using (
            var writer = new StreamWriter(
                Path.Combine(directory, "facet-template-usage.csv"),
                false,
                new UTF8Encoding(false)
            )
        )
        {
            writer.WriteLine("templateId,useCount,sourceFileCount,vertexCount,triangleCount,exampleSource");
            foreach (
                var template in Enumerable
                    .Range(0, classes.Count)
                    .OrderByDescending(template => uses[template])
                    .ThenBy(template => keys[template], StringComparer.Ordinal)
            )
                writer.WriteLine(
                    string.Join(
                        ",",
                        "t-" + keys[template],
                        uses[template].ToString(CultureInfo.InvariantCulture),
                        sourceCounts[template].ToString(CultureInfo.InvariantCulture),
                        meshes[template].Vertices.Length.ToString(CultureInfo.InvariantCulture),
                        meshes[template].TriangleCount.ToString(CultureInfo.InvariantCulture),
                        sources[classes[template].Representative.Source].SourceId
                    )
                );
        }

        using (var stream = File.Create(Path.Combine(directory, "facet-catalog.json")))
        {
            JsonSerializer.Serialize(
                stream,
                new
                {
                    schemaVersion = FormatVersion,
                    format = "RvmFacetCatalog",
                    units = "metres",
                    toleranceMetres = tolerance,
                    maximumPlacementErrorMetres = worst,
                    sources = sources.Length,
                    facetGroups = sources.Sum(source => (long)source.FacetGroups),
                    cataloguedFacetGroups = sources.Sum(source => (long)source.Catalogued),
                    templates = classes.Count,
                    singleUseTemplates = uses.Count(count => count == 1),
                    templatesInSeveralSources = sourceCounts.Count(count => count > 1),
                    templateVertices = meshes.Sum(mesh => (long)mesh.Vertices.Length),
                    elapsedSeconds = timer.Elapsed.TotalSeconds,
                },
                new JsonSerializerOptions { WriteIndented = true }
            );
        }

        Console.WriteLine(
            $"Catalog: wrote {directory} ({timer.Elapsed.TotalSeconds:F0}s); "
                + $"{uses.Count(count => count == 1):N0} single-use templates, "
                + $"{sourceCounts.Count(count => count > 1):N0} shared between sources, "
                + $"largest placement error {worst * 1000:0.####} mm"
        );
    }

    private static Source ReadSource(int index, string filename)
    {
        RvmFile rvmFile;
        using (var stream = File.OpenRead(filename))
            rvmFile = RvmParser.ReadRvm(stream);
        var facetGroups = EnumerateFacetGroups(rvmFile);
        var shapes = new List<FacetShape>();
        for (var ordinal = 0; ordinal < facetGroups.Count; ordinal++)
        {
            // Groups the export cannot tessellate, and the ones it places as unit cubes, keep
            // their existing handling and are left out of the catalog.
            var matrix = facetGroups[ordinal].Matrix;
            if (!Matrix4x4.Decompose(matrix, out _, out _, out _) || !Matrix4x4.Invert(matrix, out _))
                continue;
            var shape = FacetShape.Create(facetGroups[ordinal], index, ordinal);
            if (shape != null && !(shape.VertexCount == 24 && NativeInstanceExporter.IsUnitBox(facetGroups[ordinal])))
                shapes.Add(shape);
        }

        var classes = NativeFacetMatcher.Cluster(shapes, FacetMatchMode.Rigid, PerFileTolerance, 1);
        var representatives = new FacetShape[classes.Count];
        var groups = new RvmFacetGroup[classes.Count];
        var members = new (int Ordinal, Linear3 Linear, Vector3d Center, double Residual)[classes.Count][];
        for (var position = 0; position < classes.Count; position++)
        {
            var representative = classes[position].Representative;
            groups[position] = facetGroups[representative.Ordinal];
            members[position] = classes[position]
                .Members.Select(member =>
                    (member.Shape.Ordinal, member.Fit.Linear, member.Shape.Center, member.Fit.MaxResidual)
                )
                .ToArray();
            representatives[position] = FacetShape.FromCentered(
                representative.StructureKey,
                representative.Points,
                representative.Center,
                index,
                position,
                members[position].Length
            );
        }
        return new Source(SourceId(filename), facetGroups.Count, shapes.Count, representatives, groups, members);
    }

    /// <summary>The representative's polygons about its centroid, in metres and world orientation.</summary>
    private static RvmMesh Tessellate(RvmFacetGroup facetGroup, FacetShape shape)
    {
        if (!Matrix4x4.Invert(facetGroup.Matrix, out var inverse))
            throw new ArgumentException("Facet group matrix cannot be inverted.");
        var normalMatrix = Matrix4x4.Transpose(inverse);
        var points = shape.Points;
        var offset = 0;
        var minimum = new Vector3(float.MaxValue);
        var maximum = new Vector3(float.MinValue);
        var polygons = facetGroup
            .Polygons.Select(polygon => new RvmFacetGroup.RvmPolygon(
                polygon
                    .Contours.Select(contour => new RvmFacetGroup.RvmContour(
                        contour
                            .Vertices.Select(vertex =>
                            {
                                var position = new Vector3(points[offset], points[offset + 1], points[offset + 2]);
                                offset += 3;
                                minimum = Vector3.Min(minimum, position);
                                maximum = Vector3.Max(maximum, position);
                                var normal = Vector3.TransformNormal(vertex.Normal, normalMatrix);
                                var length = normal.Length();
                                return (position, length > 0 ? normal / length : normal);
                            })
                            .ToArray()
                    ))
                    .ToArray()
            ))
            .ToArray();
        var centred = new RvmFacetGroup(
            facetGroup.Version,
            Matrix4x4.Identity,
            new RvmBoundingBox(minimum, maximum),
            polygons
        );
        return TessellatorBridge.TessellateWithoutApplyingMatrix(centred, 1, 0)
            ?? throw new InvalidOperationException("Facet group produced no mesh.");
    }

    private static string[] WriteTemplates(string directory, RvmMesh[] meshes)
    {
        var keys = new string[meshes.Length];
        using var data = new BinaryWriter(File.Create(Path.Combine(directory, DataFileName)));
        using var index = new BinaryWriter(File.Create(Path.Combine(directory, IndexFileName)));
        index.Write(IndexMagic);
        index.Write(FormatVersion);
        index.Write(meshes.Length);
        for (var template = 0; template < meshes.Length; template++)
        {
            var mesh = meshes[template];
            keys[template] = NativeInstanceExporter.CalculateTemplateKey(RvmPrimitiveKind.FacetGroup, mesh);
            index.Write(Convert.FromHexString(keys[template]));
            index.Write(data.BaseStream.Position);
            index.Write(mesh.Vertices.Length);
            index.Write(mesh.Triangles.Length);
            foreach (var vertex in mesh.Vertices)
                Write(data, vertex);
            foreach (var normal in mesh.Normals)
                Write(data, normal);
            foreach (var triangle in mesh.Triangles)
                data.Write(triangle);
        }
        return keys;
    }

    private static void Write(BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }

    private static void WriteSlice(string path, (int Template, Linear3 Linear, Vector3d Center)[] slice)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(SliceMagic);
        writer.Write(FormatVersion);
        writer.Write(slice.Length);
        foreach (var (template, linear, center) in slice)
        {
            writer.Write(template);
            writer.Write(linear.M11);
            writer.Write(linear.M12);
            writer.Write(linear.M13);
            writer.Write(linear.M21);
            writer.Write(linear.M22);
            writer.Write(linear.M23);
            writer.Write(linear.M31);
            writer.Write(linear.M32);
            writer.Write(linear.M33);
            writer.Write(center.X);
            writer.Write(center.Y);
            writer.Write(center.Z);
        }
    }

    private sealed record Source(
        string SourceId,
        int FacetGroups,
        int Catalogued,
        FacetShape[] Representatives,
        RvmFacetGroup[] Groups,
        (int Ordinal, Linear3 Linear, Vector3d Center, double Residual)[][] Members
    );

    /// <summary>Read access for the per-file export.</summary>
    internal sealed class Reader : IDisposable
    {
        private readonly string _directory;
        private readonly FileStream _data;
        private readonly string[] _keys;
        private readonly long[] _offsets;
        private readonly int[] _vertexCounts;
        private readonly int[] _indexCounts;

        internal Reader(string directory)
        {
            _directory = directory;
            using (var index = new BinaryReader(File.OpenRead(Path.Combine(directory, IndexFileName))))
            {
                if (index.ReadUInt32() != IndexMagic || index.ReadInt32() != FormatVersion)
                    throw new InvalidDataException($"Unsupported facet catalog index in {directory}");
                var count = index.ReadInt32();
                _keys = new string[count];
                _offsets = new long[count];
                _vertexCounts = new int[count];
                _indexCounts = new int[count];
                for (var template = 0; template < count; template++)
                {
                    _keys[template] = Convert.ToHexString(index.ReadBytes(32)).ToLowerInvariant();
                    _offsets[template] = index.ReadInt64();
                    _vertexCounts[template] = index.ReadInt32();
                    _indexCounts[template] = index.ReadInt32();
                }
            }
            _data = File.OpenRead(Path.Combine(directory, DataFileName));
        }

        internal string TemplateKey(int template) => _keys[template];

        internal RvmMesh ReadMesh(int template)
        {
            _data.Position = _offsets[template];
            var reader = new BinaryReader(_data, Encoding.UTF8, leaveOpen: true);
            var vertices = new Vector3[_vertexCounts[template]];
            var normals = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
                vertices[index] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            for (var index = 0; index < normals.Length; index++)
                normals[index] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var triangles = new uint[_indexCounts[template]];
            for (var index = 0; index < triangles.Length; index++)
                triangles[index] = reader.ReadUInt32();
            return new RvmMesh(vertices, normals, triangles, 0);
        }

        /// <summary>Catalog placements of one source's facet groups, keyed by the group itself.</summary>
        internal Dictionary<RvmFacetGroup, Placement> Placements(RvmFile rvmFile, string sourceId)
        {
            var path = Path.Combine(_directory, sourceId + ".rvm-facets.bin");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Source '{sourceId}' is not in the facet catalog; rebuild the catalog over every source.",
                    path
                );
            var facetGroups = EnumerateFacetGroups(rvmFile);
            using var reader = new BinaryReader(File.OpenRead(path));
            if (reader.ReadUInt32() != SliceMagic || reader.ReadInt32() != FormatVersion)
                throw new InvalidDataException($"Unsupported facet catalog slice: {path}");
            if (reader.ReadInt32() != facetGroups.Count)
                throw new InvalidDataException(
                    $"Facet catalog for '{sourceId}' was built from a different version of the source."
                );

            var placements = new Dictionary<RvmFacetGroup, Placement>(
                facetGroups.Count,
                ReferenceEqualityComparer.Instance
            );
            Span<double> values = stackalloc double[12];
            foreach (var facetGroup in facetGroups)
            {
                var template = reader.ReadInt32();
                for (var index = 0; index < values.Length; index++)
                    values[index] = reader.ReadDouble();
                if (template < 0)
                    continue;
                if (template >= _keys.Length)
                    throw new InvalidDataException($"Facet catalog slice refers to a missing template: {path}");
                // The slice holds a column-vector rotation; System.Numerics multiplies row vectors.
                placements[facetGroup] = new Placement(
                    template,
                    new Matrix4x4(
                        (float)values[0],
                        (float)values[3],
                        (float)values[6],
                        0,
                        (float)values[1],
                        (float)values[4],
                        (float)values[7],
                        0,
                        (float)values[2],
                        (float)values[5],
                        (float)values[8],
                        0,
                        (float)values[9],
                        (float)values[10],
                        (float)values[11],
                        1
                    )
                );
            }
            return placements;
        }

        public void Dispose() => _data.Dispose();
    }
}
