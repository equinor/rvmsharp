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
    private const uint PrimitivesMagic = 0x50544652; // "RFTP"
    private const string IndexFileName = "facet-templates.idx";
    private const string DataFileName = "facet-templates.bin";
    private const string PrimitivesFileName = "facet-template-primitives.bin";
    private const string PrimitivesReportFileName = "facet-template-primitives.csv";
    private const string HintsFileName = "facet-template-hints.csv";

    internal readonly record struct Placement(int TemplateIndex, Matrix4x4 Matrix);

    internal static List<RvmFacetGroup> EnumerateFacetGroups(RvmFile rvmFile, List<string?>? sectionReferences = null)
    {
        var facetGroups = new List<RvmFacetGroup>();
        foreach (var rootChild in rvmFile.Model.Children)
            Collect(rootChild, null);
        return facetGroups;

        void Collect(RvmGroup group, string? inherited)
        {
            if (group is not RvmNode node)
                return;
            // Each facet group takes the catalogue reference of its nearest section node.
            var reference =
                sectionReferences == null ? null : NativeSectionHints.SectionReference(node.Attributes) ?? inherited;
            foreach (var facetGroup in node.Children.OfType<RvmFacetGroup>())
            {
                facetGroups.Add(facetGroup);
                sectionReferences?.Add(reference);
            }
            foreach (var child in node.Children.OfType<RvmNode>())
                Collect(child, reference);
        }
    }

    internal static string SourceId(string rvmFilename) => Path.GetFileNameWithoutExtension(rvmFilename);

    /// <param name="primitives">Fit unit primitives to the templates; null leaves every template a mesh.</param>
    internal static void Build(
        IReadOnlyList<(string Rvm, string? Txt)> inputs,
        string directory,
        double tolerance,
        PrimitiveFitOptions? primitives = null
    )
    {
        if (!(tolerance > PerFileTolerance))
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                $"Facet tolerance must exceed {PerFileTolerance.ToString(CultureInfo.InvariantCulture)} m."
            );
        var ordered = inputs.OrderBy(input => SourceId(input.Rvm), StringComparer.Ordinal).ToArray();
        var filenames = ordered.Select(input => input.Rvm).ToArray();
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
                sources[index] = ReadSource(index, ordered[index].Rvm, primitives == null ? null : ordered[index].Txt);
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
        var sectionReferences = new string?[classes.Count];
        var hintedUses = new long[classes.Count];
        double worst = 0;
        for (var template = 0; template < classes.Count; template++)
        {
            var members = classes[template].Members;
            sourceCounts[template] = members.Select(member => member.Shape.Source).Distinct().Count();
            foreach (var (shape, fit) in members)
            {
                uses[template] += shape.Weight;
                var source = sources[shape.Source];
                hintedUses[template] += source.HintedCounts[shape.Ordinal];
                sectionReferences[template] ??= source.SectionReferences[shape.Ordinal];
                foreach (var member in source.Members[shape.Ordinal])
                {
                    slices[shape.Source][member.Ordinal] = (template, member.Linear * fit.Linear, member.Center);
                    worst = Math.Max(worst, member.Residual + fit.MaxResidual);
                }
            }
        }

        var meshes = new RvmMesh[classes.Count];
        var outcomes = new PrimitiveOutcome[classes.Count];
        Parallel.For(
            0,
            classes.Count,
            new ParallelOptions { MaxDegreeOfParallelism = parallelism },
            template =>
            {
                var representative = classes[template].Representative;
                meshes[template] = Tessellate(
                    sources[representative.Source].Groups[representative.Ordinal],
                    representative,
                    out var faces
                );
                if (primitives != null)
                    outcomes[template] = NativePrimitiveFitter.Fit(
                        faces,
                        meshes[template],
                        NativeSectionHints.Family(sectionReferences[template]),
                        primitives
                    );
            }
        );
        if (primitives != null)
            Console.WriteLine(
                $"Catalog: fitted primitives to {outcomes.Count(outcome => outcome.Fit != null):N0} templates "
                    + $"({timer.Elapsed.TotalSeconds:F0}s)"
            );

        Directory.CreateDirectory(directory);
        var keys = WriteTemplates(directory, meshes);
        for (var index = 0; index < sources.Length; index++)
            WriteSlice(Path.Combine(directory, sources[index].SourceId + ".rvm-facets.bin"), slices[index]);
        foreach (var stale in new[] { PrimitivesFileName, PrimitivesReportFileName, HintsFileName })
            File.Delete(Path.Combine(directory, stale));
        if (primitives != null)
        {
            WritePrimitives(Path.Combine(directory, PrimitivesFileName), outcomes);
            WritePrimitiveReport(
                directory,
                keys,
                uses,
                outcomes,
                sectionReferences,
                hintedUses
            );
        }

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
                    primitiveReplacement = primitives == null
                        ? null
                        : new
                        {
                            options = primitives,
                            replacedTemplates = outcomes.Count(outcome => outcome.Fit != null),
                            replacedPlacements = Enumerable
                                .Range(0, classes.Count)
                                .Where(template => outcomes[template].Fit != null)
                                .Sum(template => uses[template]),
                            parts = outcomes.Sum(outcome => (long)(outcome.Fit?.Parts.Length ?? 0)),
                            hintedTemplates = sectionReferences.Count(reference => reference != null),
                            byReplacement = outcomes
                                .Where(outcome => outcome.Fit != null)
                                .GroupBy(outcome => outcome.Fit!.Replacement)
                                .OrderBy(group => group.Key, StringComparer.Ordinal)
                                .ToDictionary(group => group.Key, group => group.Count()),
                            keptByReason = outcomes
                                .Where(outcome => outcome.Fit == null)
                                .GroupBy(outcome => outcome.Outcome)
                                .OrderBy(group => group.Key, StringComparer.Ordinal)
                                .ToDictionary(group => group.Key, group => group.Count()),
                        },
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

    private static Source ReadSource(int index, string filename, string? attributesFilename)
    {
        RvmFile rvmFile;
        using (var stream = File.OpenRead(filename))
            rvmFile = RvmParser.ReadRvm(stream);
        List<string?>? references = null;
        if (attributesFilename != null)
        {
            rvmFile.AttachAttributes(attributesFilename);
            references = new List<string?>();
        }
        var facetGroups = EnumerateFacetGroups(rvmFile, references);
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
        var classReferences = new string?[classes.Count];
        var hintedCounts = new int[classes.Count];
        for (var position = 0; position < classes.Count; position++)
        {
            var representative = classes[position].Representative;
            groups[position] = facetGroups[representative.Ordinal];
            members[position] = classes[position]
                .Members.Select(member =>
                    (member.Shape.Ordinal, member.Fit.Linear, member.Shape.Center, member.Fit.MaxResidual)
                )
                .ToArray();
            if (references != null)
            {
                foreach (var member in members[position])
                {
                    if (references[member.Ordinal] is not { } reference)
                        continue;
                    classReferences[position] ??= reference;
                    hintedCounts[position]++;
                }
            }
            representatives[position] = FacetShape.FromCentered(
                representative.StructureKey,
                representative.Points,
                representative.Center,
                index,
                position,
                members[position].Length
            );
        }
        return new Source(
            SourceId(filename),
            facetGroups.Count,
            shapes.Count,
            representatives,
            groups,
            members,
            classReferences,
            hintedCounts
        );
    }

    /// <summary>The representative's polygons about its centroid, in metres and world orientation.</summary>
    private static RvmMesh Tessellate(RvmFacetGroup facetGroup, FacetShape shape, out D3[][][] faces)
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
        offset = 0;
        faces = facetGroup
            .Polygons.Select(polygon =>
                polygon
                    .Contours.Select(contour =>
                        contour
                            .Vertices.Select(_ =>
                            {
                                var point = new D3(points[offset], points[offset + 1], points[offset + 2]);
                                offset += 3;
                                return point;
                            })
                            .ToArray()
                    )
                    .ToArray()
            )
            .ToArray();
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
        (int Ordinal, Linear3 Linear, Vector3d Center, double Residual)[][] Members,
        string?[] SectionReferences,
        int[] HintedCounts
    );

    private static void WritePrimitives(string path, PrimitiveOutcome[] outcomes)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(PrimitivesMagic);
        writer.Write(FormatVersion);
        writer.Write(outcomes.Length);
        foreach (var outcome in outcomes)
        {
            var parts = outcome.Fit?.Parts ?? [];
            writer.Write(parts.Length);
            if (parts.Length == 0)
                continue;
            writer.Write(outcome.Fit!.Replacement);
            foreach (var part in parts)
            {
                writer.Write((byte)part.Kind);
                var m = part.Matrix;
                foreach (
                    var value in new[]
                    {
                        m.M11,
                        m.M12,
                        m.M13,
                        m.M14,
                        m.M21,
                        m.M22,
                        m.M23,
                        m.M24,
                        m.M31,
                        m.M32,
                        m.M33,
                        m.M34,
                        m.M41,
                        m.M42,
                        m.M43,
                        m.M44,
                    }
                )
                    writer.Write(value);
            }
        }
    }

    private static void WritePrimitiveReport(
        string directory,
        string[] keys,
        long[] uses,
        PrimitiveOutcome[] outcomes,
        string?[] sectionReferences,
        long[] hintedUses
    )
    {
        var encoding = new UTF8Encoding(false);
        using (var writer = new StreamWriter(Path.Combine(directory, HintsFileName), false, encoding))
        {
            writer.WriteLine("templateId,profileFamily,sprefExample,hintedPlacements,placements");
            for (var template = 0; template < keys.Length; template++)
            {
                if (sectionReferences[template] is not { } reference)
                    continue;
                writer.WriteLine(
                    string.Join(
                        ",",
                        "t-" + keys[template],
                        NativeSectionHints.Family(reference).ToString(),
                        Csv(reference),
                        hintedUses[template].ToString(CultureInfo.InvariantCulture),
                        uses[template].ToString(CultureInfo.InvariantCulture)
                    )
                );
            }
        }

        using (var writer = new StreamWriter(Path.Combine(directory, PrimitivesReportFileName), false, encoding))
        {
            writer.WriteLine("templateId,useCount,outcome,replacement,parts,profileFamily,volumeError,profileIoU");
            for (var template = 0; template < keys.Length; template++)
            {
                var fit = outcomes[template].Fit;
                writer.WriteLine(
                    string.Join(
                        ",",
                        "t-" + keys[template],
                        uses[template].ToString(CultureInfo.InvariantCulture),
                        outcomes[template].Outcome,
                        fit?.Replacement ?? "",
                        (fit?.Parts.Length ?? 0).ToString(CultureInfo.InvariantCulture),
                        NativeSectionHints.Family(sectionReferences[template]).ToString(),
                        fit?.VolumeError.ToString("G6", CultureInfo.InvariantCulture) ?? "",
                        fit?.ProfileIoU.ToString("G6", CultureInfo.InvariantCulture) ?? ""
                    )
                );
            }
        }
    }

    private static string Csv(string value) =>
        value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? '"' + value.Replace("\"", "\"\"") + '"' : value;

    /// <summary>Read access for the per-file export.</summary>
    internal sealed class Reader : IDisposable
    {
        private readonly string _directory;
        private readonly FileStream _data;
        private readonly string[] _keys;
        private readonly long[] _offsets;
        private readonly int[] _vertexCounts;
        private readonly int[] _indexCounts;
        private readonly PrimitiveFit?[]? _primitives;

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
            var primitives = Path.Combine(directory, PrimitivesFileName);
            if (File.Exists(primitives))
                _primitives = ReadPrimitives(primitives, _keys.Length);
        }

        internal string TemplateKey(int template) => _keys[template];

        /// <summary>The unit primitives that replace a template, if the catalog fitted any.</summary>
        internal PrimitiveFit? Primitive(int template) => _primitives?[template];

        private static PrimitiveFit?[] ReadPrimitives(string path, int count)
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            if (reader.ReadUInt32() != PrimitivesMagic || reader.ReadInt32() != FormatVersion)
                throw new InvalidDataException($"Unsupported facet catalog primitives: {path}");
            if (reader.ReadInt32() != count)
                throw new InvalidDataException($"Facet catalog primitives disagree with the template index: {path}");
            var fits = new PrimitiveFit?[count];
            Span<float> values = stackalloc float[16];
            for (var template = 0; template < count; template++)
            {
                var partCount = reader.ReadInt32();
                if (partCount == 0)
                    continue;
                var replacement = reader.ReadString();
                var parts = new PrimitivePart[partCount];
                for (var part = 0; part < partCount; part++)
                {
                    var kind = (PrimitivePartKind)reader.ReadByte();
                    for (var index = 0; index < values.Length; index++)
                        values[index] = reader.ReadSingle();
                    parts[part] = new PrimitivePart(
                        kind,
                        new Matrix4x4(
                            values[0],
                            values[1],
                            values[2],
                            values[3],
                            values[4],
                            values[5],
                            values[6],
                            values[7],
                            values[8],
                            values[9],
                            values[10],
                            values[11],
                            values[12],
                            values[13],
                            values[14],
                            values[15]
                        )
                    );
                }
                fits[template] = new PrimitiveFit(replacement, parts, double.NaN, double.NaN);
            }
            return fits;
        }

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
