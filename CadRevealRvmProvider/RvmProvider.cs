namespace CadRevealRvmProvider;

using System.Diagnostics;
using System.Text.RegularExpressions;
using BatchUtils;
using Ben.Collections.Specialized;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using Commons;
using Commons.Utils;
using Converters;
using Converters.CapVisibilityHelpers;
using Operations;
using RvmSharp.Containers;
using RvmSharp.Primitives;
using Tessellation;

public class RvmProvider : IModelFormatProvider
{
    public (IReadOnlyList<CadRevealNode>, ModelMetadata?) ParseFiles(
        IEnumerable<FileInfo> filesToParse,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering
    )
    {
        var filesToParseArray = filesToParse.ToArray();
        var workload = RvmWorkload.CollectWorkload(filesToParseArray.Select(x => x.FullName).ToArray());

        Console.WriteLine("Reading RvmData");
        var rvmTimer = Stopwatch.StartNew();

        var teamCityReadRvmFilesLogBlock = new TeamCityLogBlock("Reading Rvm Files");
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });

        var stringInternPool = new BenStringInternPool(new SharedInternPool());
        var rvmStore = RvmWorkload.ReadRvmFiles(workload, progressReport, stringInternPool);

        teamCityReadRvmFilesLogBlock.CloseBlock();

        if (workload.Length == 0)
        {
            // returns empty list if there are no rvm files to process
            return (new List<CadRevealNode>(), null);
        }

        LogRvmPrimitives(rvmStore);

        var rvmFilesSizeMb = GetFileSizeInMegaBytes(workload.Select(w => w.rvmFilename));
        var txtFilesSizeMb = GetFileSizeInMegaBytes(workload.Select(w => w.txtFilename).WhereNotNull());

        Console.WriteLine(
            $"Read RvmData in {rvmTimer.Elapsed}. (~{rvmFilesSizeMb:F2}MB of .rvm files (and (~{txtFilesSizeMb:F2}MB .txt file size) (sum: {rvmFilesSizeMb + txtFilesSizeMb:F2}MB)"
        );

        var stopwatch = Stopwatch.StartNew();
        int rvmNodeCount = rvmStore
            .RvmFiles.SelectMany(x => x.Model.Children)
            .SelectMany(x => x.EnumerateNodesRecursive())
            .Count();
        Console.WriteLine($"RvmNode count: {rvmNodeCount}");

        bool truncateEmptyNodes = rvmNodeCount > TreeIndexGenerator.MaxTreeIndex * 0.7;
        if (truncateEmptyNodes)
        {
            Console.WriteLine($"Truncating empty nodes due to very high node count {rvmNodeCount}");
        }

        var nodes = RvmStoreToCadRevealNodesConverter.RvmStoreToCadRevealNodes(
            rvmStore,
            treeIndexGenerator,
            nodeNameFiltering,
            truncateNodesWithoutMetadata: truncateEmptyNodes
        );
        Console.WriteLine(
            "CadRevealNodeCount: " + nodes.Length + ". TreeIndex count is " + treeIndexGenerator.PeekNextId
        );

        AddMetadataForSurfaceUnits(nodes);

        // Temp solution to add custom metadata for equipment nodes based on matching RefNo with an additional metadata CSV file, until we have a better solution for custom metadata in place.
        var additionalMetadataFiles = filesToParseArray.Where(x =>
            x.Name.EndsWith("AdditionalMetadata.csv", StringComparison.OrdinalIgnoreCase)
        );
        foreach (FileInfo additionalMetadataCsv in additionalMetadataFiles)
        {
            AddCustomEchoAttributeMetadata(nodes, additionalMetadataCsv);
        }

        Console.WriteLine($"Converted RVM files to Reveal nodes in {stopwatch.Elapsed}");

        return (nodes, null);
    }

    public APrimitive[] ProcessGeometries(
        APrimitive[] geometries,
        ComposerParameters composerParameters,
        ModelParameters modelParameters,
        InstanceIdGenerator instanceIdGenerator
    )
    {
        var stopwatch = Stopwatch.StartNew();

        var facetGroupsWithEmbeddedProtoMeshes = geometries
            .OfType<ProtoMeshFromFacetGroup>()
            .Select(p => new RvmFacetGroupWithProtoMesh(
                p,
                p.FacetGroup.Version,
                p.FacetGroup.Matrix,
                p.FacetGroup.BoundingBoxLocal,
                p.FacetGroup.Polygons
            ))
            .Cast<RvmFacetGroup>()
            .ToArray();

        RvmFacetGroupMatcher.Result[] facetGroupInstancingResult;
        if (composerParameters.NoInstancing)
        {
            facetGroupInstancingResult = facetGroupsWithEmbeddedProtoMeshes
                .Select(x => new RvmFacetGroupMatcher.NotInstancedResult(x))
                .Cast<RvmFacetGroupMatcher.Result>()
                .ToArray();
            Console.WriteLine("Facet group instancing disabled.");
        }
        else
        {
            facetGroupInstancingResult = RvmFacetGroupMatcher.MatchAll(
                facetGroupsWithEmbeddedProtoMeshes,
                facetGroups => facetGroups.Length >= modelParameters.InstancingThreshold.Value,
                modelParameters.TemplateCountLimit.Value
            );
            Console.WriteLine($"Facet groups instance matched in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }

        var protoMeshesFromPyramids = geometries.OfType<ProtoMeshFromRvmPyramid>().ToArray();
        // We have models where several pyramids on the same "part" are completely identical.
        var uniqueProtoMeshesFromPyramid = protoMeshesFromPyramids.Distinct().ToArray();
        if (uniqueProtoMeshesFromPyramid.Length < protoMeshesFromPyramids.Length)
        {
            var diffCount = protoMeshesFromPyramids.Length - uniqueProtoMeshesFromPyramid.Length;
            Console.WriteLine(
                $"Found and ignored {diffCount} duplicate pyramids (including: position, mesh, parent, id, etc)."
            );
        }

        RvmPyramidInstancer.Result[] pyramidInstancingResult;
        if (composerParameters.NoInstancing)
        {
            pyramidInstancingResult = uniqueProtoMeshesFromPyramid
                .Select(x => new RvmPyramidInstancer.NotInstancedResult(x))
                .OfType<RvmPyramidInstancer.Result>()
                .ToArray();
            Console.WriteLine("Pyramid instancing disabled.");
        }
        else
        {
            pyramidInstancingResult = RvmPyramidInstancer.Process(
                uniqueProtoMeshesFromPyramid,
                pyramids => pyramids.Length >= modelParameters.InstancingThreshold.Value
            );
            Console.WriteLine($"Pyramids instance matched in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }

        Console.WriteLine("Start tessellate");
        var meshes = RvmTessellator.TessellateAndOutputInstanceMeshes(
            facetGroupInstancingResult,
            pyramidInstancingResult,
            instanceIdGenerator,
            composerParameters.SimplificationThreshold
        );

        var geometriesIncludingMeshes = geometries.Where(g => g is not ProtoMesh).Concat(meshes).ToArray();

        Console.WriteLine($"Tessellated all meshes in {stopwatch.Elapsed}");

        Console.WriteLine($"Show number of caps: {CapVisibility.CapsShown}");
        Console.WriteLine($"Hide number of caps: {CapVisibility.CapsHidden}");
        Console.WriteLine($"Caps Without connection: {CapVisibility.CapsWithoutConnections}");
        Console.WriteLine($"Total number of caps tested: {CapVisibility.TotalNumberOfCapsTested}");

        stopwatch.Restart();

        return geometriesIncludingMeshes;
    }

    private static void LogRvmPrimitives(RvmStore rvmStore)
    {
        var allRvmPrimitivesGroups = rvmStore
            .RvmFiles.SelectMany(f => f.Model.Children)
            .SelectMany(RvmNode.GetAllPrimitivesFlat)
            .GroupBy(x => x.GetType());

        using (new TeamCityLogBlock("RvmPrimitive Count"))
        {
            foreach (var group in allRvmPrimitivesGroups)
            {
                Console.WriteLine($"Count of {group.Key.ToString().Split('.').Last()}: {group.Count()}");
            }
        }
    }

    /// <summary>
    /// Adds metadata for surface unit volumes to the Attributes of the given nodes.
    /// </summary>
    /// <remarks>
    /// Surface unit volumes are identified by their parent node name containing "/A00-AREA"
    /// </remarks>
    /// <param name="nodes"></param>
    public static void AddMetadataForSurfaceUnits(IReadOnlyList<CadRevealNode> nodes)
    {
        // Matches strings like "/12A34"
        // This MAY not be the naming standard on all assets, but it's the best we have for now.
        var regex = new Regex(@"^\/\d+[A-Z]\d+$");
        // /A00-AREA
        //   /A00-AREA/OFP
        //     /A00-AREA/OFP/EL-472000_DECK-1
        //       /1B41 <- This is a surface unit volume

        var foundA00Area = false;
        var foundSurfaceUnitVolumes = 0;
        foreach (CadRevealNode cadRevealNode in nodes)
        {
            if (cadRevealNode.Parent?.Name.StartsWith("/A00-AREA", StringComparison.OrdinalIgnoreCase) != true)
                continue;
            foundA00Area = true;

            if (!regex.IsMatch(cadRevealNode.Name))
                continue;

            var surfaceUnitVolumeName = cadRevealNode.Name.TrimStart('/');
            cadRevealNode.Attributes.Add("IsSurfaceUnitVolume", "true"); // Optimize for hiding all surface unit volumes when needed
            cadRevealNode.Attributes.Add("SurfaceUnitVolume", surfaceUnitVolumeName);
            foundSurfaceUnitVolumes++;
        }

        if (!foundA00Area)
            return;

        Console.WriteLine(
            foundSurfaceUnitVolumes == 0
                ? "Warning: RVM files contained /A00-AREA nodes but no surface unit volumes were found in that file."
                : $"Added metadata for {foundSurfaceUnitVolumes} surface unit volumes found in /A00-AREA nodes."
        );
    }

    public static void AddCustomEchoAttributeMetadata(
        IReadOnlyList<CadRevealNode> nodes,
        FileInfo additionalMetadataFile
    )
    {
        var metadataByRefNo = LoadAdditionalMetadataFromCsv(additionalMetadataFile);
        if (metadataByRefNo == null)
            return;

        var matchedEquipmentNodes = ApplyMetadataToNodes(nodes, metadataByRefNo);

        if (matchedEquipmentNodes == 0)
            return;

        Console.WriteLine(
            $"Added metadata for {matchedEquipmentNodes} out of {metadataByRefNo.Count} nodes based on RefNo matching in file {additionalMetadataFile.Name}."
        );
    }

    /// <summary>
    /// Loads additional metadata from a CSV file.
    /// </summary>
    /// <param name="additionalMetadataCsv">The CSV file containing additional metadata with a "RefNo" column.</param>
    /// <returns>
    /// A dictionary mapping each RefNo to its additional attributes (excluding RefNo itself),
    /// or null if the file is empty or missing the RefNo column.
    /// </returns>
    public static Dictionary<string, Dictionary<string, string>>? LoadAdditionalMetadataFromCsv(
        FileInfo additionalMetadataCsv
    )
    {
        using var reader = new StreamReader(additionalMetadataCsv.FullName);
        var header = reader.ReadLine();
        if (header == null)
            return null;

        var headers = header.Split(',');
        var refNoIndex = Array.IndexOf(headers, "RefNo");

        if (refNoIndex == -1)
            return null;

        var result = new Dictionary<string, Dictionary<string, string>>();

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line == null)
                continue;
            var values = line.Split(',');
            if (values.Length <= refNoIndex)
                continue;

            var refNo = values[refNoIndex];
            // Map all the attributes except the RefNo column
            var attrs = headers
                .Select((h, i) => (h, i))
                .Where(x => x.i != refNoIndex && x.i < values.Length)
                .ToDictionary(x => x.h, x => values[x.i]);
            result[refNo] = attrs;
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// Applies equipment metadata to nodes that have a matching RefNo attribute.
    /// </summary>
    /// <param name="nodes">The nodes to which metadata will be applied.</param>
    /// <param name="metadataByRefNo">Maps each RefNo to its additional attributes.</param>
    /// <returns>The number of nodes to which metadata was successfully applied.</returns>
    public static int ApplyMetadataToNodes(
        IReadOnlyList<CadRevealNode> nodes,
        Dictionary<string, Dictionary<string, string>> metadataByRefNo
    )
    {
        var matched = 0;

        // Prefix here may be considered for removal in the future.
        const string attributePrefix = "Echo_"; // Prefix to avoid potential conflicts with existing attributes

        foreach (var node in nodes)
        {
            var refNo = node.Attributes.GetValueOrNull("RefNo");
            if (refNo == null || !metadataByRefNo.TryGetValue(refNo, out var attrs))
                continue;

            foreach ((string key, string value) in attrs)
                node.Attributes.Add(attributePrefix + key, value);

            matched++;
        }

        return matched;
    }

    /// <summary>
    /// Get the total size of the files in all the filenames in MegaBytes.
    /// </summary>
    /// <param name="filenames">A list of filenames</param>
    /// <returns>Total file size in MegaBytes (MB)</returns>
    private static double GetFileSizeInMegaBytes(IEnumerable<string> filenames)
    {
        return ByteUtils.BytesToMegabytes(filenames.Sum(filename => new FileInfo(filename).Length));
    }
}
