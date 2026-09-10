namespace RvmSharp.Exe;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using CadRevealRvmProvider;
using CadRevealRvmProvider.Converters;
using CadRevealRvmProvider.Operations;
using Containers;
using Exporters;
using Primitives;
using Tessellation;

/// <summary>
/// Exports the RVM representation before <see cref="TessellatorBridge"/> bakes
/// primitive transforms into OBJ vertices. Templates contain local-space mesh
/// data; each placement retains its source transform and RVM/TXT metadata.
/// </summary>
internal static class NativeInstanceExporter
{
    private const int SchemaVersion = 5;
    private const float NativeTemplateUnitScale = 0.001f;
    private const float CircularTorusVertexMergeTolerance = 0.001f;
    private const float CircularTorusNormalMergeTolerance = 0.002f;
    private const float CircularTorusBoundsBucketSize = 0.005f;

    internal static void Export(
        RvmStore rvmStore,
        float tolerance,
        IReadOnlyList<(string Key, Regex ValuePattern)>? attributeExclusions,
        string? manifestOutput,
        string? inventoryOutput,
        string? instancesCsvOutput,
        string? inventoryCsvOutput,
        string? remainsObjOutput,
        string? uniqueObjOutput,
        string? templateUsageCsvOutput)
    {
        if (manifestOutput == null && inventoryOutput == null && instancesCsvOutput == null && inventoryCsvOutput == null
            && remainsObjOutput == null && uniqueObjOutput == null && templateUsageCsvOutput == null)
            return;

        var templatesByKey = new Dictionary<string, NativeTemplate>();
        var instances = new List<NativeInstance>();
        var warnings = new List<string>();
        var facetGroupMatches = MatchFacetGroups(rvmStore, attributeExclusions);

        for (var fileIndex = 0; fileIndex < rvmStore.RvmFiles.Count; fileIndex++)
        {
            var rootChildren = rvmStore.RvmFiles[fileIndex].Model.Children;
            for (var childIndex = 0; childIndex < rootChildren.Count; childIndex++)
            {
                CollectInstances(
                    rootChildren[childIndex],
                    $"root[{childIndex}]",
                    new SortedDictionary<string, string>(StringComparer.Ordinal),
                    fileIndex,
                    tolerance,
                    attributeExclusions,
                    facetGroupMatches,
                    templatesByKey,
                    instances,
                    warnings);
            }
        }

        MergeCircularTorusRoundingTemplates(templatesByKey, instances, warnings);
        var templates = templatesByKey.Values.OrderBy(template => template.Id, StringComparer.Ordinal).ToArray();
        var orderedInstances = instances.OrderBy(instance => instance.Id, StringComparer.Ordinal).ToArray();
        var templatesById = templates.ToDictionary(template => template.Id, StringComparer.Ordinal);
        var summary = new NativeSummary(
            templates.Length,
            orderedInstances.Length,
            templates.Sum(template => (long)template.VertexCount),
            templates.Sum(template => (long)template.TriangleCount),
            templates.Sum(template => (long)template.VertexCount * template.UseCount),
            templates.Sum(template => (long)template.TriangleCount * template.UseCount),
            templates.Sum(template => template.EstimatedBinaryBytes),
            templates.Sum(template => template.EstimatedBinaryBytes * template.UseCount),
            orderedInstances.Count(instance => !instance.TransformDecomposable));

        if (manifestOutput != null)
        {
            var manifest = new NativeManifest(
                SchemaVersion,
                "RvmNativeInstances",
                "RVM world-space as read by RvmSharp",
                "metres",
                "System.Numerics Matrix4x4 row-vector; local vertex is multiplied by matrix",
                tolerance,
                summary,
                templates,
                orderedInstances,
                warnings);
            WriteJson(manifestOutput, manifest);
        }

        if (inventoryOutput != null)
        {
            var inventory = new NativeInventory(
                SchemaVersion,
                "RvmNativeInventory",
                tolerance,
                summary,
                templates
                    .GroupBy(template => template.Kind)
                    .OrderBy(group => group.Key, StringComparer.Ordinal)
                    .Select(group => new KindInventory(
                        group.Key,
                        group.Count(),
                        group.Sum(template => template.UseCount),
                        group.Sum(template => (long)template.VertexCount),
                        group.Sum(template => (long)template.TriangleCount),
                        group.Sum(template => template.EstimatedBinaryBytes),
                        group.Sum(template => (long)template.VertexCount * template.UseCount),
                        group.Sum(template => (long)template.TriangleCount * template.UseCount),
                        group.Sum(template => template.EstimatedBinaryBytes * template.UseCount)))
                    .ToArray(),
                templates
                    .Select(template => new TemplateInventory(
                        template.Id,
                        template.Kind,
                        template.ConverterRepresentation,
                        template.VertexCount,
                        template.TriangleCount,
                        template.EstimatedBinaryBytes,
                        (long)template.VertexCount * template.UseCount,
                        (long)template.TriangleCount * template.UseCount,
                        template.EstimatedBinaryBytes * template.UseCount,
                        template.UseCount,
                        template.LocalBounds))
                    .ToArray(),
                CreateSemanticGroups(orderedInstances, templatesById),
                manifestOutput != null && File.Exists(manifestOutput) ? new FileInfo(manifestOutput).Length : null,
                warnings);
            WriteJson(inventoryOutput, inventory);
        }

        if (instancesCsvOutput != null)
            WriteInstancesCsv(instancesCsvOutput, orderedInstances);

        if (inventoryCsvOutput != null)
            WriteInventoryCsv(inventoryCsvOutput, templates, summary);

        if (remainsObjOutput != null)
            WriteRemainsObj(remainsObjOutput, templatesById, orderedInstances);

        if (uniqueObjOutput != null)
            WriteUniqueObj(uniqueObjOutput, templates.Where(template => template.UseCount > 1));

        if (templateUsageCsvOutput != null)
            WriteTemplateUsageCsv(templateUsageCsvOutput, templates);
    }

    private static IReadOnlyDictionary<RvmFacetGroup, FacetGroupMatch> MatchFacetGroups(
        RvmStore rvmStore,
        IReadOnlyList<(string Key, Regex ValuePattern)>? attributeExclusions)
    {
        var facetGroups = new List<RvmFacetGroup>();
        foreach (var rvmFile in rvmStore.RvmFiles)
        {
            foreach (var rootChild in rvmFile.Model.Children)
                CollectFacetGroups(rootChild, attributeExclusions, facetGroups);
        }

        if (facetGroups.Count < 2)
            return new Dictionary<RvmFacetGroup, FacetGroupMatch>(FacetGroupReferenceComparer.Instance);

        return RvmFacetGroupMatcher.MatchAll(
                facetGroups.ToArray(),
                static group => group.Length > 1,
                uint.MaxValue)
            .OfType<RvmFacetGroupMatcher.InstancedResult>()
            .ToDictionary(
                result => result.FacetGroup,
                result => new FacetGroupMatch(result.Template, result.Transform),
                FacetGroupReferenceComparer.Instance);
    }

    private static void CollectFacetGroups(
        RvmGroup group,
        IReadOnlyList<(string Key, Regex ValuePattern)>? attributeExclusions,
        ICollection<RvmFacetGroup> facetGroups)
    {
        if (group is not RvmNode node || IsExcluded(node, attributeExclusions))
            return;

        foreach (var facetGroup in node.Children.OfType<RvmFacetGroup>())
            facetGroups.Add(facetGroup);

        foreach (var childNode in node.Children.OfType<RvmNode>())
            CollectFacetGroups(childNode, attributeExclusions, facetGroups);
    }

    /// <summary>
    /// Groups placements into a PIPE-oriented commodity catalog. Geometry
    /// templates remain the rendering-reuse identity; these groups make the
    /// inherited PDMS/TXT component metadata useful for reporting.
    /// </summary>
    private static SemanticGroup[] CreateSemanticGroups(
        IReadOnlyList<NativeInstance> instances,
        IReadOnlyDictionary<string, NativeTemplate> templatesById)
    {
        return instances
            .GroupBy(instance => CreateCatalogKey(instance.Attributes).Value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var catalogKey = CreateCatalogKey(group.First().Attributes);
                var variants = group
                    .GroupBy(instance => instance.TemplateId, StringComparer.Ordinal)
                    .OrderBy(variant => variant.Key, StringComparer.Ordinal)
                    .Select(variant =>
                    {
                        var template = templatesById[variant.Key];
                        var instanceCount = variant.Count();
                        return new SemanticVariant(
                            template.Id,
                            template.Kind,
                            template.VertexCount,
                            template.TriangleCount,
                            template.EstimatedBinaryBytes,
                            (long)template.VertexCount * instanceCount,
                            (long)template.TriangleCount * instanceCount,
                            template.EstimatedBinaryBytes * instanceCount,
                            instanceCount);
                    })
                    .ToArray();
                var representative = group.OrderBy(instance => instance.Id, StringComparer.Ordinal).First();

                return new SemanticGroup(
                    catalogKey.Value,
                    catalogKey.Type,
                    catalogKey.Specification,
                    catalogKey.NominalDiameterMm,
                    group.Count(),
                    variants.Length,
                    variants.Sum(variant => (long)variant.SingleVertexCount),
                    variants.Sum(variant => (long)variant.SingleTriangleCount),
                    variants.Sum(variant => variant.SingleEstimatedBinaryBytes),
                    variants.Sum(variant => variant.TotalVertexCount),
                    variants.Sum(variant => variant.TotalTriangleCount),
                    variants.Sum(variant => variant.TotalEstimatedBinaryBytes),
                    variants,
                    new RepresentativePlacement(
                        representative.SourceFileIndex,
                        representative.NodePath,
                        representative.NodeName,
                        GetAttribute(representative.Attributes, "RefNo"),
                        GetAttribute(representative.Attributes, "Tag")));
            })
            .ToArray();
    }

    private static SemanticCatalogKey CreateCatalogKey(IReadOnlyDictionary<string, string> attributes)
    {
        var type = NormalizeCatalogValue(GetAttribute(attributes, "Type"));
        var specification = NormalizeCatalogValue(
            GetAttribute(attributes, "Spec") ?? GetAttribute(attributes, "Ispec"));
        var nominalDiameterMm = NormalizeNominalDiameter(GetAttribute(attributes, "Nom.diam (mm)"));
        return new SemanticCatalogKey(
            $"Type={type}|Specification={specification}|NominalDiameterMm={nominalDiameterMm}",
            type,
            specification,
            nominalDiameterMm);
    }

    private static string? GetAttribute(IReadOnlyDictionary<string, string> attributes, string key) =>
        attributes.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static string NormalizeCatalogValue(string? value) => string.IsNullOrWhiteSpace(value)
        ? "(missing)"
        : Regex.Replace(value.Trim(), @"\s+", " ").ToUpperInvariant();

    private static string NormalizeNominalDiameter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(missing)";

        var normalized = Regex.Replace(value.Trim(), @"\s+", " ");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var diameter)
            ? diameter.ToString("0.#############################", CultureInfo.InvariantCulture)
            : normalized.ToUpperInvariant();
    }

    private static void CollectInstances(
        RvmGroup group,
        string path,
        SortedDictionary<string, string> inheritedAttributes,
        int fileIndex,
        float tolerance,
        IReadOnlyList<(string Key, Regex ValuePattern)>? attributeExclusions,
        IReadOnlyDictionary<RvmFacetGroup, FacetGroupMatch> facetGroupMatches,
        Dictionary<string, NativeTemplate> templatesByKey,
        List<NativeInstance> instances,
        List<string> warnings)
    {
        if (group is not RvmNode node)
            return;

        if (IsExcluded(node, attributeExclusions))
            return;

        var attributes = new SortedDictionary<string, string>(inheritedAttributes, StringComparer.Ordinal);
        foreach (var (key, value) in node.Attributes)
            attributes[key] = value;

        var nodePath = $"{path}/{node.Name}";
        var primitiveIndex = 0;
        foreach (var primitive in node.Children.OfType<RvmPrimitive>())
        {
            var instanceId = $"rvm-{fileIndex}:{nodePath}:primitive-{primitiveIndex++}";
            try
            {
                var templateKind = primitive.Kind;
                var emittedKind = primitive.Kind.ToString();
                var templateUnitScale = primitive is RvmFacetGroup ? 1f : NativeTemplateUnitScale;
                var localBoundsScale = templateUnitScale;
                var converterRepresentation = GetConverterRepresentation(primitive, node, instanceId, warnings);
                string? uniqueObjName = null;
                var localMesh = TessellateLocal(primitive, tolerance);
                var instanceMatrix = NormalizeMatrixForMetreTemplate(primitive.Matrix, templateUnitScale);
                if (primitive is RvmFacetGroup facetGroup
                    && facetGroupMatches.TryGetValue(facetGroup, out var facetGroupMatch))
                {
                    localMesh = TessellateLocal(facetGroupMatch.Template, tolerance);
                    instanceMatrix = NormalizeMatrixForMetreTemplate(facetGroupMatch.Transform, templateUnitScale);
                }
                else if (primitive is RvmCircularTorus circularTorus)
                    localMesh = TessellateLocal(circularTorus.WithSampleStartAngle(0), tolerance);
                if (primitive is RvmFacetGroup && localMesh != null
                    && TryCreateUnitBoxInstanceMatrix(localMesh, instanceMatrix, out var facetGroupBoxMatrix))
                {
                    localMesh = CreateUnitBoxMesh();
                    instanceMatrix = facetGroupBoxMatrix;
                    templateKind = RvmPrimitiveKind.Box;
                    emittedKind = RvmPrimitiveKind.Box.ToString();
                    templateUnitScale = NativeTemplateUnitScale;
                    uniqueObjName = "CUBE";
                }
                if (primitive is RvmBox box && TryCreateUnitBoxInstanceMatrix(box, out var unitBoxInstanceMatrix))
                {
                    localMesh = CreateUnitBoxMesh();
                    instanceMatrix = unitBoxInstanceMatrix;
                    templateKind = RvmPrimitiveKind.Box;
                    uniqueObjName = "CUBE";
                }
                else if (primitive is RvmPyramid pyramid && RvmPyramidConverter.IsBoxShaped(pyramid)
                    && TryCreateUnitBoxInstanceMatrix(pyramid, out unitBoxInstanceMatrix))
                {
                    localMesh = CreateUnitBoxMesh();
                    instanceMatrix = unitBoxInstanceMatrix;
                    templateKind = RvmPrimitiveKind.Box;
                    uniqueObjName = "CUBE";
                }
                else if (primitive is RvmCylinder cylinder
                    && TryCreateUnitCylinderInstanceMatrix(cylinder, out var unitCylinderInstanceMatrix)
                    && localMesh != null)
                {
                    var isPipe = IsPipeCylinder(converterRepresentation, localMesh);
                    var canonicalCylinderMesh = isPipe ? null : CreateUnitCylinderMesh(tolerance);
                    if (canonicalCylinderMesh != null && IsFourSidedCappedCylinder(canonicalCylinderMesh)
                        && TryCreateUnitBoxInstanceMatrix(cylinder, out var unitBoxMatrix))
                    {
                        localMesh = CreateUnitBoxMesh();
                        instanceMatrix = unitBoxMatrix;
                        templateKind = RvmPrimitiveKind.Box;
                        emittedKind = RvmPrimitiveKind.Box.ToString();
                        uniqueObjName = "CUBE";
                    }
                    else
                    {
                        localMesh = isPipe ? CreateUnitPipeMesh(tolerance) : canonicalCylinderMesh!;
                        instanceMatrix = unitCylinderInstanceMatrix;
                        uniqueObjName = isPipe ? "PIPE" : "CYLINDER";
                    }
                }

                if (!Matrix4x4.Decompose(instanceMatrix, out var scale, out var rotation, out var translation))
                {
                    warnings.Add($"{instanceId}: transform could not be decomposed; matrix retained.");
                    scale = Vector3.One;
                    rotation = Quaternion.Identity;
                    translation = Vector3.Zero;
                }

                if (localMesh == null)
                {
                    warnings.Add($"{instanceId}: {primitive.Kind} has no tessellated mesh and was omitted.");
                    continue;
                }

                var templateKey = CalculateTemplateKey(templateKind, localMesh);
                if (!templatesByKey.TryGetValue(templateKey, out var template))
                {
                    template = CreateTemplate(
                        templateKey,
                        templateKind,
                        converterRepresentation,
                        uniqueObjName,
                        localMesh,
                        templateUnitScale);
                    templatesByKey.Add(templateKey, template);
                }
                template = template with { UseCount = template.UseCount + 1 };
                templatesByKey[templateKey] = template;

                instances.Add(new NativeInstance(
                    instanceId,
                    template.Id,
                    emittedKind,
                    converterRepresentation,
                    fileIndex,
                    nodePath,
                    node.Name,
                    attributes,
                    ToColor(PdmsColors.TryGetColorByCode(node.MaterialId, out var color) ? color : Color.Magenta),
                    ToMatrix(instanceMatrix),
                    new[] { translation.X, translation.Y, translation.Z },
                    new[] { rotation.X, rotation.Y, rotation.Z, rotation.W },
                    new[] { scale.X, scale.Y, scale.Z },
                    Matrix4x4.Decompose(instanceMatrix, out _, out _, out _),
                    ToBounds(primitive.BoundingBoxLocal, localBoundsScale),
                    ToBounds(primitive.CalculateAxisAlignedBoundingBox())));
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                warnings.Add($"{instanceId}: {exception.Message}");
            }
        }

        var childNodeIndex = 0;
        foreach (var childNode in node.Children.OfType<RvmNode>())
        {
            CollectInstances(
                childNode,
                $"{nodePath}[{childNodeIndex++}]",
                attributes,
                fileIndex,
                tolerance,
                attributeExclusions,
                facetGroupMatches,
                templatesByKey,
                instances,
                warnings);
        }
    }

    private static bool IsExcluded(RvmNode node, IReadOnlyList<(string Key, Regex ValuePattern)>? exclusions)
    {
        return exclusions != null && exclusions.Any(exclusion =>
            node.Attributes.TryGetValue(exclusion.Key, out var value) && exclusion.ValuePattern.IsMatch(value));
    }

    private static RvmMesh? TessellateLocal(RvmPrimitive primitive, float tolerance)
    {
        if (!Matrix4x4.Decompose(primitive.Matrix, out var scale, out _, out _))
            throw new ArgumentException($"Could not decompose matrix for {primitive.Kind}");

        var scaleScalar = Math.Max(scale.X, Math.Max(scale.Y, scale.Z));
        return TessellatorBridge.TessellateWithoutApplyingMatrix(primitive, scaleScalar, tolerance);
    }

    private static string GetConverterRepresentation(
        RvmPrimitive primitive,
        RvmNode node,
        string instanceId,
        ICollection<string> warnings)
    {
        try
        {
            var converted = RvmPrimitiveToAPrimitive
                .FromRvmPrimitive(0, primitive, node, new FailedPrimitivesLogObject())
                .ToArray();
            return converted.Length == 0
                ? "Unsupported"
                : string.Join("+", converted.Select(value => value.GetType().Name).Distinct().OrderBy(name => name, StringComparer.Ordinal));
        }
        catch (Exception exception) when (exception is ArgumentException or NotImplementedException)
        {
            warnings.Add($"{instanceId}: converter classification fell back to mesh ({exception.Message}).");
            return "MeshFallback";
        }
    }

    private static RvmMesh CreateUnitBoxMesh()
    {
        var sourceUnitCubeSize = 1 / NativeTemplateUnitScale;
        var unitBox = new RvmBox(
            1,
            Matrix4x4.Identity,
            new RvmBoundingBox(-Vector3.One * (sourceUnitCubeSize * 0.5f), Vector3.One * (sourceUnitCubeSize * 0.5f)),
            sourceUnitCubeSize,
            sourceUnitCubeSize,
            sourceUnitCubeSize);
        return TessellatorBridge.TessellateWithoutApplyingMatrix(unitBox, 1, 0)!;
    }

    private static RvmMesh CreateUnitCylinderMesh(float tolerance)
    {
        var sourceUnitSize = 1 / NativeTemplateUnitScale;
        var unitCylinder = new RvmCylinder(
            1,
            Matrix4x4.Identity,
            new RvmBoundingBox(
                new Vector3(-sourceUnitSize, -sourceUnitSize, -sourceUnitSize * 0.5f),
                new Vector3(sourceUnitSize, sourceUnitSize, sourceUnitSize * 0.5f)),
            sourceUnitSize,
            sourceUnitSize);
        return TessellatorBridge.TessellateWithoutApplyingMatrix(unitCylinder, NativeTemplateUnitScale, tolerance)!;
    }

    private static RvmMesh CreateUnitPipeMesh(float tolerance)
    {
        var cylinder = CreateUnitCylinderMesh(tolerance);
        var usedVertices = cylinder.Triangles
            .Where((_, index) => MathF.Abs(cylinder.Normals[cylinder.Triangles[index]].Z) != 1)
            .ToArray();
        var retainedVertexIndices = usedVertices.Distinct().OrderBy(index => index).ToArray();
        var vertexMap = retainedVertexIndices
            .Select((originalIndex, replacementIndex) => (originalIndex, replacementIndex))
            .ToDictionary(pair => pair.originalIndex, pair => pair.replacementIndex);

        var triangles = cylinder.Triangles
            .Chunk(3)
            .Where(triangle => triangle.All(index => vertexMap.ContainsKey(index)))
            .SelectMany(triangle => triangle.Select(index => (uint)vertexMap[index]))
            .ToArray();
        return new RvmMesh(
            retainedVertexIndices.Select(index => cylinder.Vertices[index]).ToArray(),
            retainedVertexIndices.Select(index => cylinder.Normals[index]).ToArray(),
            triangles,
            cylinder.Error);
    }

    private static bool IsPipeCylinder(string converterRepresentation, RvmMesh sourceMesh)
    {
        if (converterRepresentation == "Cone")
            return true;
        if (converterRepresentation == "Circle+Cone")
            return false;

        return !sourceMesh.Triangles.Any(index => MathF.Abs(sourceMesh.Normals[index].Z) > 0.999f);
    }

    private static bool IsFourSidedCappedCylinder(RvmMesh mesh) => mesh.Vertices.Length == 16
        && mesh.TriangleCount == 12;

    private static bool TryCreateUnitBoxInstanceMatrix(
        RvmMesh mesh,
        Matrix4x4 primitiveMatrix,
        out Matrix4x4 matrix)
    {
        if (mesh.Vertices.Length != 24 || mesh.TriangleCount != 12)
        {
            matrix = default;
            return false;
        }

        var bounds = ToBounds(mesh.Vertices);
        var size = new Vector3(
            bounds.Max![0] - bounds.Min![0],
            bounds.Max[1] - bounds.Min[1],
            bounds.Max[2] - bounds.Min[2]);
        var tolerance = Math.Max(1e-6f, size.Length() * 1e-5f);
        var corners = new List<Vector3>(8);
        foreach (var vertex in mesh.Vertices)
        {
            if (!corners.Any(corner => Vector3.DistanceSquared(corner, vertex) <= tolerance * tolerance))
                corners.Add(vertex);
        }

        if (corners.Count != 8)
        {
            matrix = default;
            return false;
        }

        for (var originIndex = 0; originIndex < corners.Count; originIndex++)
        {
            var origin = corners[originIndex];
            var edges = corners.Where((_, index) => index != originIndex).Select(corner => corner - origin).ToArray();
            for (var first = 0; first < edges.Length - 2; first++)
            for (var second = first + 1; second < edges.Length - 1; second++)
            for (var third = second + 1; third < edges.Length; third++)
            {
                var edgeX = edges[first];
                var edgeY = edges[second];
                var edgeZ = edges[third];
                if (edgeX.Length() <= tolerance || edgeY.Length() <= tolerance || edgeZ.Length() <= tolerance)
                    continue;

                var expectedCorners = new[]
                {
                    origin, origin + edgeX, origin + edgeY, origin + edgeX + edgeY,
                    origin + edgeZ, origin + edgeX + edgeZ, origin + edgeY + edgeZ,
                    origin + edgeX + edgeY + edgeZ,
                };
                if (expectedCorners.Any(expected => !corners.Any(corner => Vector3.DistanceSquared(corner, expected) <= tolerance * tolerance)))
                    continue;

                if (!HasSixCubeFaces(mesh, expectedCorners, tolerance))
                    continue;

                var center = origin + (edgeX + edgeY + edgeZ) * 0.5f;
                matrix = new Matrix4x4(
                        edgeX.X, edgeX.Y, edgeX.Z, 0,
                        edgeY.X, edgeY.Y, edgeY.Z, 0,
                        edgeZ.X, edgeZ.Y, edgeZ.Z, 0,
                        center.X, center.Y, center.Z, 1)
                    * primitiveMatrix;
                return true;
            }
        }

        matrix = default;
        return false;
    }

    private static bool HasSixCubeFaces(RvmMesh mesh, IReadOnlyList<Vector3> corners, float tolerance)
    {
        var vertexCorners = mesh.Vertices
            .Select(vertex => Array.FindIndex(corners.ToArray(), corner =>
                Vector3.DistanceSquared(corner, vertex) <= tolerance * tolerance))
            .ToArray();
        if (vertexCorners.Any(cornerIndex => cornerIndex < 0))
            return false;

        var faceCounts = new int[6];
        foreach (var triangle in mesh.Triangles.Chunk(3))
        {
            if (triangle.Length != 3)
                return false;

            var triangleCorners = triangle.Select(vertexIndex => vertexCorners[vertexIndex]).ToArray();
            if (triangleCorners.Distinct().Count() != 3)
                return false;

            var faceIndex = -1;
            for (var axis = 0; axis < 3; axis++)
            {
                var coordinate = triangleCorners[0] >> axis & 1;
                if (triangleCorners.All(corner => (corner >> axis & 1) == coordinate))
                {
                    if (faceIndex >= 0)
                        return false;
                    faceIndex = axis * 2 + coordinate;
                }
            }

            if (faceIndex < 0)
                return false;
            faceCounts[faceIndex]++;
        }

        return faceCounts.All(count => count == 2);
    }

    private static bool TryCreateUnitBoxInstanceMatrix(RvmBox box, out Matrix4x4 matrix)
    {
        if (!Matrix4x4.Decompose(box.Matrix, out var sourceScale, out var rotation, out var translation))
        {
            matrix = default;
            return false;
        }

        var boxScale = Vector3.Multiply(
            sourceScale,
            new Vector3(box.LengthX, box.LengthY, box.LengthZ));
        matrix = Matrix4x4.CreateScale(boxScale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(translation);
        return true;
    }

    private static bool TryCreateUnitBoxInstanceMatrix(RvmPyramid pyramid, out Matrix4x4 matrix)
    {
        if (!Matrix4x4.Decompose(pyramid.Matrix, out var sourceScale, out var rotation, out var translation))
        {
            matrix = default;
            return false;
        }

        var boxScale = Vector3.Multiply(
            sourceScale,
            new Vector3(pyramid.BottomX, pyramid.BottomY, pyramid.Height));
        matrix = Matrix4x4.CreateScale(boxScale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(translation);
        return true;
    }

    private static bool TryCreateUnitBoxInstanceMatrix(RvmCylinder cylinder, out Matrix4x4 matrix)
    {
        if (!Matrix4x4.Decompose(cylinder.Matrix, out var sourceScale, out var rotation, out var translation))
        {
            matrix = default;
            return false;
        }

        var boxScale = Vector3.Multiply(
            sourceScale,
            new Vector3(cylinder.Radius * 2, cylinder.Radius * 2, cylinder.Height));
        matrix = Matrix4x4.CreateScale(boxScale)
            * Matrix4x4.CreateFromQuaternion(rotation)
            * Matrix4x4.CreateTranslation(translation);
        return true;
    }

    private static bool TryCreateUnitCylinderInstanceMatrix(RvmCylinder cylinder, out Matrix4x4 matrix)
    {
        if (cylinder.Radius <= 0 || cylinder.Height <= 0)
        {
            matrix = default;
            return false;
        }

        matrix = Matrix4x4.CreateScale(cylinder.Radius, cylinder.Radius, cylinder.Height) * cylinder.Matrix;
        return true;
    }

    private static NativeTemplate CreateTemplate(
        string key,
        RvmPrimitiveKind kind,
        string converterRepresentation,
        string? uniqueObjName,
        RvmMesh mesh,
        float unitScale)
    {
        var id = "t-" + key;
        var vertices = mesh.Vertices.Select(vertex => ToVector(vertex * unitScale)).ToArray();
        var normals = mesh.Normals.Select(ToVector).ToArray();
        var estimatedBinaryBytes = ((long)mesh.Vertices.Length * 3 * sizeof(float))
            + ((long)mesh.Normals.Length * 3 * sizeof(float))
            + ((long)mesh.Triangles.Length * sizeof(uint));

        return new NativeTemplate(
            id,
            kind.ToString(),
            converterRepresentation,
            uniqueObjName,
            0,
            mesh.Vertices.Length,
            mesh.TriangleCount,
            ToBounds(mesh.Vertices.Select(vertex => vertex * unitScale).ToArray()),
            estimatedBinaryBytes,
            vertices,
            normals,
            mesh.Triangles);
    }

    private static string CalculateTemplateKey(RvmPrimitiveKind kind, RvmMesh mesh)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Append(hash, (int)kind);
        foreach (var vertex in mesh.Vertices)
            Append(hash, vertex);
        foreach (var normal in mesh.Normals)
            Append(hash, normal);
        foreach (var triangle in mesh.Triangles)
            Append(hash, triangle);
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    private static void MergeCircularTorusRoundingTemplates(
        Dictionary<string, NativeTemplate> templatesByKey,
        List<NativeInstance> instances,
        ICollection<string> warnings)
    {
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
        var candidates = templatesByKey
            .Select(pair => (Key: pair.Key, Template: pair.Value))
            .Where(pair => pair.Template.Kind == RvmPrimitiveKind.CircularTorus.ToString())
            .ToArray();

        foreach (var bucket in candidates.GroupBy(pair => CreateCircularTorusMergeBucket(pair.Template), StringComparer.Ordinal))
        {
            var canonicalTemplates = new List<(string Key, NativeTemplate Template)>();
            foreach (var candidate in bucket.OrderBy(pair => pair.Template.Id, StringComparer.Ordinal))
            {
                var canonical = canonicalTemplates.FirstOrDefault(existing =>
                    AreCircularTorusTemplatesEquivalent(existing.Template, candidate.Template));
                if (canonical.Template == null)
                {
                    canonicalTemplates.Add(candidate);
                    continue;
                }

                var updatedTemplate = canonical.Template with
                {
                    UseCount = canonical.Template.UseCount + candidate.Template.UseCount,
                };
                templatesByKey[canonical.Key] = updatedTemplate;
                canonicalTemplates[canonicalTemplates.FindIndex(existing => existing.Key == canonical.Key)] =
                    (canonical.Key, updatedTemplate);
                templatesByKey.Remove(candidate.Key);
                replacements[candidate.Template.Id] = canonical.Template.Id;
            }
        }

        if (replacements.Count == 0)
            return;

        for (var index = 0; index < instances.Count; index++)
        {
            var instance = instances[index];
            if (replacements.TryGetValue(instance.TemplateId, out var replacementId))
                instances[index] = instance with { TemplateId = replacementId };
        }

        warnings.Add($"Merged {replacements.Count} circular-torus template(s) within rounding tolerance.");
    }

    private static string CreateCircularTorusMergeBucket(NativeTemplate template)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var triangle in template.Triangles)
            Append(hash, triangle);

        return string.Join("|", template.ConverterRepresentation, template.VertexCount, template.TriangleCount,
            QuantizeBounds(template.LocalBounds), Convert.ToHexString(hash.GetHashAndReset()));
    }

    private static string QuantizeBounds(Bounds bounds) => bounds.Min == null || bounds.Max == null
        ? "empty"
        : string.Join(",", bounds.Min.Concat(bounds.Max).Select(value =>
            MathF.Round(value / CircularTorusBoundsBucketSize).ToString(CultureInfo.InvariantCulture)));

    private static bool AreCircularTorusTemplatesEquivalent(NativeTemplate left, NativeTemplate right)
    {
        if (left.VertexCount != right.VertexCount || left.TriangleCount != right.TriangleCount
            || !left.Triangles.SequenceEqual(right.Triangles))
            return false;

        return left.Vertices.Zip(right.Vertices).All(pair => AreWithinTolerance(
                   pair.First, pair.Second, CircularTorusVertexMergeTolerance))
            && left.Normals.Zip(right.Normals).All(pair => AreWithinTolerance(
                pair.First, pair.Second, CircularTorusNormalMergeTolerance));
    }

    private static bool AreWithinTolerance(float[] left, float[] right, float tolerance) => left.Length == right.Length
        && left.Zip(right).All(pair => MathF.Abs(pair.First - pair.Second) <= tolerance);

    private static void Append(IncrementalHash hash, Vector3 value)
    {
        Append(hash, value.X);
        Append(hash, value.Y);
        Append(hash, value.Z);
    }

    private static void Append(IncrementalHash hash, float value) => Append(hash, BitConverter.SingleToInt32Bits(value));

    private static void Append(IncrementalHash hash, int value) => hash.AppendData(BitConverter.GetBytes(value));

    private static void Append(IncrementalHash hash, uint value) => hash.AppendData(BitConverter.GetBytes(value));

    private static float[][] ToMatrix(Matrix4x4 matrix) =>
    [
        [matrix.M11, matrix.M12, matrix.M13, matrix.M14],
        [matrix.M21, matrix.M22, matrix.M23, matrix.M24],
        [matrix.M31, matrix.M32, matrix.M33, matrix.M34],
        [matrix.M41, matrix.M42, matrix.M43, matrix.M44],
    ];

    private static float[] ToVector(Vector3 vector) => [vector.X, vector.Y, vector.Z];

    private static byte[] ToColor(Color color) => [color.R, color.G, color.B, color.A];

    private static Bounds ToBounds(RvmBoundingBox? bounds, float scale = 1) => bounds == null
        ? Bounds.Empty
        : new Bounds(ToVector(bounds.Min * scale), ToVector(bounds.Max * scale));

    private static Bounds ToBounds(IReadOnlyList<Vector3> vertices)
    {
        if (vertices.Count == 0)
            return Bounds.Empty;

        var min = vertices[0];
        var max = vertices[0];
        for (var index = 1; index < vertices.Count; index++)
        {
            min = Vector3.Min(min, vertices[index]);
            max = Vector3.Max(max, vertices[index]);
        }
        return new Bounds(ToVector(min), ToVector(max));
    }

    private static void WriteJson<T>(string outputPath, T value)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var stream = File.Create(outputPath);
        JsonSerializer.Serialize(stream, value, new JsonSerializerOptions { WriteIndented = true });
        stream.WriteByte((byte)'\n');
    }

    private static void WriteRemainsObj(
        string outputPath,
        IReadOnlyDictionary<string, NativeTemplate> templatesById,
        IReadOnlyList<NativeInstance> instances)
    {
        EnsureOutputDirectory(outputPath);

        using var exporter = new ObjExporter(outputPath);
        foreach (var instance in instances.Where(instance => templatesById[instance.TemplateId].UseCount == 1))
        {
            var template = templatesById[instance.TemplateId];
            var mesh = ToMesh(template);
            mesh.Apply(ToMatrix4x4(instance.Matrix));
            exporter.StartObject(instance.Id);
            exporter.StartGroup(instance.Id);
            exporter.WriteMesh(mesh);
        }
    }

    private static void WriteUniqueObj(string outputPath, IEnumerable<NativeTemplate> templates)
    {
        EnsureOutputDirectory(outputPath);

        using var exporter = new ObjExporter(outputPath);
        foreach (var template in templates)
        {
            var objectName = template.UniqueObjName ?? $"template_{template.Id}";
            exporter.StartObject(objectName);
            exporter.StartGroup(objectName);
            exporter.WriteMesh(ToMesh(template));
        }
    }

    private static void WriteTemplateUsageCsv(string outputPath, IReadOnlyList<NativeTemplate> templates)
    {
        EnsureOutputDirectory(outputPath);

        using var writer = new StreamWriter(outputPath, false, new System.Text.UTF8Encoding(false));
        writer.WriteLine("templateId,primitiveKind,useCount,vertexCount,triangleCount,singleCopyVertexBytes,duplicatedVertexBytes,estimatedVertexBytesSaved");
        foreach (var template in templates.OrderByDescending(template => template.UseCount).ThenBy(template => template.Id, StringComparer.Ordinal))
        {
            var singleCopyVertexBytes = (long)template.VertexCount * 3 * sizeof(float);
            var duplicatedVertexBytes = singleCopyVertexBytes * template.UseCount;
            var estimatedVertexBytesSaved = singleCopyVertexBytes * (template.UseCount - 1L);
            writer.WriteLine(string.Join(",",
                template.Id,
                template.Kind,
                template.UseCount.ToString(CultureInfo.InvariantCulture),
                template.VertexCount.ToString(CultureInfo.InvariantCulture),
                template.TriangleCount.ToString(CultureInfo.InvariantCulture),
                singleCopyVertexBytes.ToString(CultureInfo.InvariantCulture),
                duplicatedVertexBytes.ToString(CultureInfo.InvariantCulture),
                estimatedVertexBytesSaved.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void WriteInstancesCsv(string outputPath, IReadOnlyList<NativeInstance> instances)
    {
        EnsureOutputDirectory(outputPath);

        using var writer = new StreamWriter(outputPath, false, new System.Text.UTF8Encoding(false));
        writer.WriteLine("instanceId,templateId,kind,converterRepresentation,sourceFileIndex,nodePath,nodeName,colorR,colorG,colorB,colorA,m11,m12,m13,m14,m21,m22,m23,m24,m31,m32,m33,m34,m41,m42,m43,m44,translationX,translationY,translationZ,rotationX,rotationY,rotationZ,rotationW,scaleX,scaleY,scaleZ,transformDecomposable,localMinX,localMinY,localMinZ,localMaxX,localMaxY,localMaxZ,worldMinX,worldMinY,worldMinZ,worldMaxX,worldMaxY,worldMaxZ,attributesJson");
        foreach (var instance in instances)
        {
            var values = new List<string>
            {
                instance.Id, instance.TemplateId, instance.Kind, instance.ConverterRepresentation,
                instance.SourceFileIndex.ToString(CultureInfo.InvariantCulture), instance.NodePath, instance.NodeName,
                instance.Color[0].ToString(CultureInfo.InvariantCulture), instance.Color[1].ToString(CultureInfo.InvariantCulture),
                instance.Color[2].ToString(CultureInfo.InvariantCulture), instance.Color[3].ToString(CultureInfo.InvariantCulture),
            };
            values.AddRange(instance.Matrix.SelectMany(row => row).Select(FormatFloat));
            values.AddRange(instance.Translation.Select(FormatFloat));
            values.AddRange(instance.Rotation.Select(FormatFloat));
            values.AddRange(instance.Scale.Select(FormatFloat));
            values.Add(instance.TransformDecomposable ? "true" : "false");
            values.AddRange(FormatBounds(instance.LocalBounds));
            values.AddRange(FormatBounds(instance.WorldBounds));
            values.Add(JsonSerializer.Serialize(instance.Attributes));
            writer.WriteLine(string.Join(",", values.Select(EscapeCsv)));
        }
    }

    private static void WriteInventoryCsv(string outputPath, IReadOnlyList<NativeTemplate> templates, NativeSummary summary)
    {
        EnsureOutputDirectory(outputPath);

        using var writer = new StreamWriter(outputPath, false, new System.Text.UTF8Encoding(false));
        writer.WriteLine("recordType,id,kind,converterRepresentation,uniqueObjName,useCount,vertexCount,triangleCount,estimatedBinaryBytes,localMinX,localMinY,localMinZ,localMaxX,localMaxY,localMaxZ");
        foreach (var template in templates)
        {
            var values = new List<string>
            {
                "template", template.Id, template.Kind, template.ConverterRepresentation, template.UniqueObjName ?? string.Empty,
                template.UseCount.ToString(CultureInfo.InvariantCulture), template.VertexCount.ToString(CultureInfo.InvariantCulture),
                template.TriangleCount.ToString(CultureInfo.InvariantCulture), template.EstimatedBinaryBytes.ToString(CultureInfo.InvariantCulture),
            };
            values.AddRange(FormatBounds(template.LocalBounds));
            writer.WriteLine(string.Join(",", values.Select(EscapeCsv)));
        }

        foreach (var group in templates.GroupBy(template => template.Kind).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            writer.WriteLine(string.Join(",", new[]
            {
                "kind", string.Empty, group.Key, string.Empty, string.Empty,
                group.Sum(template => template.UseCount).ToString(CultureInfo.InvariantCulture),
                group.Sum(template => (long)template.VertexCount).ToString(CultureInfo.InvariantCulture),
                group.Sum(template => (long)template.TriangleCount).ToString(CultureInfo.InvariantCulture),
                group.Sum(template => template.EstimatedBinaryBytes).ToString(CultureInfo.InvariantCulture),
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            }.Select(EscapeCsv)));
        }

        writer.WriteLine(string.Join(",", new[]
        {
            "summary", string.Empty, string.Empty, string.Empty, string.Empty,
            summary.InstanceCount.ToString(CultureInfo.InvariantCulture), summary.TemplateVertexCount.ToString(CultureInfo.InvariantCulture),
            summary.TemplateTriangleCount.ToString(CultureInfo.InvariantCulture), summary.TemplateEstimatedBinaryBytes.ToString(CultureInfo.InvariantCulture),
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        }.Select(EscapeCsv)));
    }

    private static IEnumerable<string> FormatBounds(Bounds bounds) => bounds.Min == null || bounds.Max == null
        ? Enumerable.Repeat(string.Empty, 6)
        : bounds.Min.Concat(bounds.Max).Select(FormatFloat);

    private static string FormatFloat(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string EscapeCsv(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? '"' + value.Replace("\"", "\"\"") + '"'
        : value;

    private static void EnsureOutputDirectory(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    private static RvmMesh ToMesh(NativeTemplate template) => new(
        template.Vertices.Select(vertex => new Vector3(vertex[0], vertex[1], vertex[2])).ToArray(),
        template.Normals.Select(normal => new Vector3(normal[0], normal[1], normal[2])).ToArray(),
        template.Triangles,
        0);

    private static Matrix4x4 ToMatrix4x4(float[][] matrix) => new(
        matrix[0][0], matrix[0][1], matrix[0][2], matrix[0][3],
        matrix[1][0], matrix[1][1], matrix[1][2], matrix[1][3],
        matrix[2][0], matrix[2][1], matrix[2][2], matrix[2][3],
        matrix[3][0], matrix[3][1], matrix[3][2], matrix[3][3]);

    private static Matrix4x4 NormalizeMatrixForMetreTemplate(Matrix4x4 matrix, float templateUnitScale) => new(
        matrix.M11 / templateUnitScale, matrix.M12 / templateUnitScale,
        matrix.M13 / templateUnitScale, matrix.M14 / templateUnitScale,
        matrix.M21 / templateUnitScale, matrix.M22 / templateUnitScale,
        matrix.M23 / templateUnitScale, matrix.M24 / templateUnitScale,
        matrix.M31 / templateUnitScale, matrix.M32 / templateUnitScale,
        matrix.M33 / templateUnitScale, matrix.M34 / templateUnitScale,
        matrix.M41, matrix.M42, matrix.M43, matrix.M44);

    private sealed record FacetGroupMatch(RvmFacetGroup Template, Matrix4x4 Transform);

    private sealed class FacetGroupReferenceComparer : IEqualityComparer<RvmFacetGroup>
    {
        internal static readonly FacetGroupReferenceComparer Instance = new();

        public bool Equals(RvmFacetGroup? left, RvmFacetGroup? right) => ReferenceEquals(left, right);

        public int GetHashCode(RvmFacetGroup facetGroup) => RuntimeHelpers.GetHashCode(facetGroup);
    }

    private sealed record NativeManifest(
        int SchemaVersion,
        string AssetType,
        string CoordinateSystem,
        string Units,
        string MatrixConvention,
        float TessellationTolerance,
        NativeSummary Summary,
        NativeTemplate[] Templates,
        NativeInstance[] Instances,
        List<string> Warnings);

    private sealed record NativeInventory(
        int SchemaVersion,
        string AssetType,
        float TessellationTolerance,
        NativeSummary Summary,
        KindInventory[] Kinds,
        TemplateInventory[] Templates,
        SemanticGroup[] SemanticGroups,
        long? ManifestByteSize,
        List<string> Warnings);

    private sealed record NativeSummary(
        int TemplateCount,
        int InstanceCount,
        long TemplateVertexCount,
        long TemplateTriangleCount,
        long TotalInstanceVertexCount,
        long TotalInstanceTriangleCount,
        long TemplateEstimatedBinaryBytes,
        long TotalInstanceEstimatedBinaryBytes,
        int NonDecomposableTransformCount);

    private sealed record NativeTemplate(
        string Id,
        string Kind,
        string ConverterRepresentation,
        string? UniqueObjName,
        int UseCount,
        int VertexCount,
        int TriangleCount,
        Bounds LocalBounds,
        long EstimatedBinaryBytes,
        float[][] Vertices,
        float[][] Normals,
        uint[] Triangles);

    private sealed record NativeInstance(
        string Id,
        string TemplateId,
        string Kind,
        string ConverterRepresentation,
        int SourceFileIndex,
        string NodePath,
        string NodeName,
        SortedDictionary<string, string> Attributes,
        byte[] Color,
        float[][] Matrix,
        float[] Translation,
        float[] Rotation,
        float[] Scale,
        bool TransformDecomposable,
        Bounds LocalBounds,
        Bounds WorldBounds);

    private sealed record KindInventory(
        string Kind,
        int TemplateCount,
        int InstanceCount,
        long TemplateVertexCount,
        long TemplateTriangleCount,
        long TemplateEstimatedBinaryBytes,
        long TotalInstanceVertexCount,
        long TotalInstanceTriangleCount,
        long TotalInstanceEstimatedBinaryBytes);

    private sealed record TemplateInventory(
        string Id,
        string Kind,
        string ConverterRepresentation,
        int SingleVertexCount,
        int SingleTriangleCount,
        long SingleEstimatedBinaryBytes,
        long TotalVertexCount,
        long TotalTriangleCount,
        long TotalEstimatedBinaryBytes,
        int InstanceCount,
        Bounds LocalBounds);

    private sealed record SemanticCatalogKey(
        string Value,
        string Type,
        string Specification,
        string NominalDiameterMm);

    private sealed record SemanticGroup(
        string CatalogKey,
        string Type,
        string Specification,
        string NominalDiameterMm,
        int TotalInstanceCount,
        int UniqueTemplateCount,
        long SingleVertexCount,
        long SingleTriangleCount,
        long SingleEstimatedBinaryBytes,
        long TotalVertexCount,
        long TotalTriangleCount,
        long TotalEstimatedBinaryBytes,
        SemanticVariant[] Variants,
        RepresentativePlacement RepresentativePlacement);

    private sealed record SemanticVariant(
        string TemplateId,
        string Kind,
        int SingleVertexCount,
        int SingleTriangleCount,
        long SingleEstimatedBinaryBytes,
        long TotalVertexCount,
        long TotalTriangleCount,
        long TotalEstimatedBinaryBytes,
        int InstanceCount);

    private sealed record RepresentativePlacement(
        int SourceFileIndex,
        string NodePath,
        string NodeName,
        string? RefNo,
        string? Tag);

    private sealed record Bounds(float[]? Min, float[]? Max)
    {
        public static Bounds Empty { get; } = new(null, null);
    }
}