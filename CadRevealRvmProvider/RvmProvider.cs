﻿namespace CadRevealRvmProvider;

using Operations;
using Utils;
using Tessellation;
using Ben.Collections.Specialized;
using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;
using RvmSharp.BatchUtils;
using RvmSharp.Primitives;
using System.Diagnostics;

public class RvmProvider : IModelFormatProvider
{

    public IReadOnlyList<CadRevealNode> ParseFiles(IEnumerable<FileInfo> filesToParse, TreeIndexGenerator treeIndexGenerator)
    {
        var workload = Workload.CollectWorkload( filesToParse.Select(x => x.FullName).ToArray());

        Console.WriteLine("Reading RvmData");
        var rvmTimer = Stopwatch.StartNew();

        var teamCityReadRvmFilesLogBlock = new TeamCityLogBlock("Reading Rvm Files");
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });

        var stringInternPool = new BenStringInternPool(new SharedInternPool());
        var rvmStore = Workload.ReadRvmData(workload, progressReport, stringInternPool);
        var fileSizesTotal = workload.Sum(w => new FileInfo(w.rvmFilename).Length);
        teamCityReadRvmFilesLogBlock.CloseBlock();
        Console.WriteLine(
            $"Read RvmData in {rvmTimer.Elapsed}. (~{fileSizesTotal / 1024 / 1024}mb of .rvm files (excluding .txt file size))");

        var stopwatch = Stopwatch.StartNew();
        var nodes = RvmStoreToCadRevealNodesConverter.RvmStoreToCadRevealNodes(rvmStore, treeIndexGenerator);
        Console.WriteLine($"Converted to reveal nodes in {stopwatch.Elapsed}");

        return nodes;
    }

    public APrimitive[] ProcessGeometries(APrimitive[] geometries,
        ComposerParameters composerParameters,
        ModelParameters modelParameters)
    {
        var stopwatch = Stopwatch.StartNew();

        var facetGroupsWithEmbeddedProtoMeshes = geometries
            .OfType<ProtoMeshFromFacetGroup>()
            .Select(p => new RvmFacetGroupWithProtoMesh(p, p.FacetGroup.Version, p.FacetGroup.Matrix,
                p.FacetGroup.BoundingBoxLocal, p.FacetGroup.Polygons))
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
                facetGroups => facetGroups.Length >= modelParameters.InstancingThreshold.Value);
            Console.WriteLine($"Facet groups instance matched in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }

        var protoMeshesFromPyramids = geometries.OfType<ProtoMeshFromRvmPyramid>().ToArray();
        // We have models where several pyramids on the same "part" are completely identical.
        var uniqueProtoMeshesFromPyramid = protoMeshesFromPyramids.Distinct().ToArray();
        if (uniqueProtoMeshesFromPyramid.Length < protoMeshesFromPyramids.Length)
        {
            var diffCount = protoMeshesFromPyramids.Length - uniqueProtoMeshesFromPyramid.Length;
            Console.WriteLine($"Found and ignored {diffCount} duplicate pyramids (including: position, mesh, parent, id, etc).");
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
                pyramids => pyramids.Length >= modelParameters.InstancingThreshold.Value);
            Console.WriteLine($"Pyramids instance matched in {stopwatch.Elapsed}");
            stopwatch.Restart();
        }

        Console.WriteLine("Start tessellate");
        var meshes = RvmTessellator.TessellateAndOutputInstanceMeshes(
            facetGroupInstancingResult,
            pyramidInstancingResult
        );

        var geometriesIncludingMeshes = geometries
            .Where(g => g is not ProtoMesh)
            .Concat(meshes)
            .ToArray();

        Console.WriteLine($"Tessellated all meshes in {stopwatch.Elapsed}");
        stopwatch.Restart();

        return geometriesIncludingMeshes;
    }

}