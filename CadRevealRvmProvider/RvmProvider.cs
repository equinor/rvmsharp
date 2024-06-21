namespace CadRevealRvmProvider;

using System.Diagnostics;
using BatchUtils;
using Ben.Collections.Specialized;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Operations;
using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using CadRevealComposer.Utils.MeshOptimization;
using Commons;
using Converters;
using Converters.CapVisibilityHelpers;
using Operations;
using RvmSharp.Containers;
using RvmSharp.Primitives;
using System.Drawing;
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
        var workload = RvmWorkload.CollectWorkload(filesToParse.Select(x => x.FullName).ToArray());

        Console.WriteLine("Reading RvmData");
        var rvmTimer = Stopwatch.StartNew();

        var teamCityReadRvmFilesLogBlock = new TeamCityLogBlock("Reading Rvm Files");
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });

        var stringInternPool = new BenStringInternPool(new SharedInternPool());
        var rvmStore = RvmWorkload.ReadRvmFiles(workload, progressReport, stringInternPool);
        var fileSizesTotal = workload.Sum(w => new FileInfo(w.rvmFilename).Length);
        teamCityReadRvmFilesLogBlock.CloseBlock();

        if (workload.Length == 0)
        {
            // returns empty list if there are no rvm files to process
            return (new List<CadRevealNode>(), null);
        }

        LogRvmPrimitives(rvmStore);
        Console.WriteLine(
            $"Read RvmData in {rvmTimer.Elapsed}. (~{fileSizesTotal / 1024 / 1024}mb of .rvm files (excluding .txt file size))"
        );

        var stopwatch = Stopwatch.StartNew();
        var nodes = RvmStoreToCadRevealNodesConverter.RvmStoreToCadRevealNodes(
            rvmStore,
            treeIndexGenerator,
            nodeNameFiltering
        );
        Console.WriteLine($"Converted to reveal nodes in {stopwatch.Elapsed}");

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
        List<TriangleMesh> disjointedTriangleMeshes;
        using (new TeamCityLogBlock("Split Disjoined Meshes to separate TriangleMeshes"))
        {
            // The idea behind this is that we can avoid loading smaller parts of disjoined meshes.
            // The disjoin check is fast, and we combine the meshes later so it should(TM) not impact performance too much.

            // THIS CODE IS HACKY AND MUST BE REFACTORED BEFORE MERGING
            var disjointTimer = Stopwatch.StartNew();
            var triangleMeshInput = meshes.OfType<TriangleMesh>().ToList();
            disjointedTriangleMeshes = triangleMeshInput
                .AsParallel().SelectMany(original =>
                {
                    var pieces = DisjointMeshTools.SplitDisjointPieces(original.Mesh);
                    if (pieces.Length == 1) return [original];
                    var results = new List<TriangleMesh>();
                    var i = 0;
                    foreach (Mesh p in pieces)
                    {
                        var color = i == 0 ? original.Color : Color.Magenta; //
                        results.Add(new TriangleMesh(p, original.TreeIndex, color, p.CalculateAxisAlignedBoundingBox(null)));
                        i++;
                    }

                    return results;
                })
                .ToList();

            Console.WriteLine(
                $"Disjoint mesh split in {disjointTimer.Elapsed}, Was {triangleMeshInput.Count} now {disjointedTriangleMeshes.Count}"
            );
        }

        // TODO FIXME Refactor this code
        var geometriesIncludingMeshes = geometries.Where(g => g is not ProtoMesh && g is not TriangleMesh).Concat(
            disjointedTriangleMeshes
        ).Concat(meshes).ToArray();

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
}
