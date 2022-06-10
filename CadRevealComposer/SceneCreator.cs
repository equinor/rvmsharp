namespace CadRevealComposer;

using Configuration;
using HierarchyComposer.Functions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Operations;
using Primitives;
using RvmSharp.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using Utils;
using Writers;

public static class SceneCreator
{
    public record SectorInfo(
        uint SectorId,
        uint? ParentSectorId,
        long Depth,
        string Path,
        string Filename,
        long EstimatedTriangleCount,
        long EstimatedDrawCalls,
        IReadOnlyList<APrimitive> Geometries,
        Vector3 BoundingBoxMin,
        Vector3 BoundingBoxMax
    )
    {
        public long DownloadSize { get; init; }
    }

    public static void ExportHierarchyDatabase(string databasePath, CadRevealNode[] allNodes)
    {
        var nodes = HierarchyComposerConverter.ConvertToHierarchyNodes(allNodes);

        ILogger<DatabaseComposer> databaseLogger = NullLogger<DatabaseComposer>.Instance;
        var exporter = new DatabaseComposer(databaseLogger);
        exporter.ComposeDatabase(nodes.ToList(), Path.GetFullPath(databasePath));
    }

    public static void WriteSceneFile(
        ImmutableArray<SectorInfo> sectors,
        ModelParameters parameters,
        DirectoryInfo outputDirectory,
        ulong maxTreeIndex,
        CameraPositioning.CameraPosition cameraPosition)
    {
        Sector FromSector(SectorInfo sector)
        {
            if (!sector.Geometries.Any())
                throw new Exception($"Sector {sector.SectorId} contains Zero geometries. This will cause issues in Reveal. Stopping!: {sector}");

            // TODO: Check if this may be the correct way to handle min and max diagonal values.
            float maxDiagonalLength = sector.Geometries.Max(x => x.AxisAlignedBoundingBox.Diagonal);
            float minDiagonalLength = sector.Geometries.Min(x => x.AxisAlignedBoundingBox.Diagonal);
            return new Sector
            {
                Id = sector.SectorId,
                ParentId = sector.ParentSectorId,
                BoundingBox =
                    new BoundingBox(
                        Min: new BbVector3(sector.BoundingBoxMin.X, sector.BoundingBoxMin.Y, sector.BoundingBoxMin.Z),
                        Max: new BbVector3(sector.BoundingBoxMax.X, sector.BoundingBoxMax.Y, sector.BoundingBoxMax.Z)
                    ),
                GeometryBoundingBox = null, // TODO: Implement Geometry and Subtree Bounding Box (Currently gracefully handled if null in reveal v9)
                Depth = sector.Depth,
                Path = sector.Path,
                EstimatedTriangleCount = sector.EstimatedTriangleCount,
                EstimatedDrawCallCount = sector.EstimatedDrawCalls,
                SectorFileName = sector.Filename,
                MaxDiagonalLength = maxDiagonalLength,
                MinDiagonalLength = minDiagonalLength,
                DownloadSize = sector.DownloadSize
            };
        }

        var scene = new Scene
        {
            Version = 9,
            ProjectId = parameters.ProjectId,
            ModelId = parameters.ModelId,
            RevisionId = parameters.RevisionId,
            SubRevisionId = -1,
            MaxTreeIndex = maxTreeIndex,
            Unit = "Meters",
            Sectors = sectors.Select(FromSector).ToArray()
        };

        var cameraPath = Path.Join(outputDirectory.FullName, "initialCamera.json");
        var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
        JsonUtils.JsonSerializeToFile(cameraPosition, cameraPath);
        JsonUtils.JsonSerializeToFile(scene, scenePath, Formatting.Indented);
    }

    public static void ExportSector(SectorInfo sector, string outputDirectory)
    {
        var facetGroupsWithProtoMesh = sector.Geometries
            .OfType<ProtoMeshFromFacetGroup>()
            .Select(p => new CadRevealComposerRunner.RvmFacetGroupWithProtoMesh(p, p.FacetGroup.Version,
                p.FacetGroup.Matrix, p.FacetGroup.BoundingBoxLocal, p.FacetGroup.Polygons))
            .Cast<RvmFacetGroup>()
            .ToArray();
        var facetGroupInstancingResults = RvmFacetGroupMatcher.MatchAll(facetGroupsWithProtoMesh, fg => fg.Length >= 20);

        var protoMeshesFromPyramids = sector.Geometries.OfType<ProtoMeshFromPyramid>().ToArray();
        // We have models where several pyramids on the same "part" are completely identical.
        var uniqueProtoMeshesFromPyramid = protoMeshesFromPyramids.Distinct().ToArray();
        var pyramidInstancingResult = RvmPyramidInstancer.Process(uniqueProtoMeshesFromPyramid, py => py.Length > 20);
        var filePath = Path.Join(outputDirectory, sector.Filename);

        var meshes =
            CadRevealComposerRunner.TessellateAndOutputInstanceMeshes(facetGroupInstancingResults,
                pyramidInstancingResult);
        var geometriesIncludingMeshes = sector.Geometries.Where(x => x is not ProtoMesh).Concat(meshes).ToArray();

        using var gltfSectorFile = File.Create(filePath);
        GltfWriter.WriteSector(geometriesIncludingMeshes, gltfSectorFile);
        gltfSectorFile.Flush(true);
    }


}