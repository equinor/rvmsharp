namespace CadRevealComposer;

using Configuration;
using HierarchyComposer.Functions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Operations;
using Primitives;
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
        string? Filename,
        long EstimatedTriangleCount,
        long EstimatedDrawCalls,
        IReadOnlyList<APrimitive>? Geometries,
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
            var isRootNode = sector.SectorId == 0;
            var parentId = sector.SectorId != 0 ? sector.ParentSectorId : null;
            var isEmptyNode = (sector.Geometries == null || !sector.Geometries.Any());
            if (!isRootNode && (sector.Geometries == null || !sector.Geometries.Any()))    //This should not happen any more?
            {
                isEmptyNode = true;
            }

            // TODO: Check if this may be the correct way to handle min and max diagonal values.
            float maxDiagonalLength = 0;
            float minDiagonalLength = 0;
            if (!isEmptyNode)
            {
                maxDiagonalLength = sector.Geometries!.Max(x => x.AxisAlignedBoundingBox.Diagonal);
                minDiagonalLength = sector.Geometries!.Min(x => x.AxisAlignedBoundingBox.Diagonal);
            }
            return new Sector
            {
                Id = sector.SectorId,
                ParentId = parentId,
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
                SectorFileName = !isEmptyNode ? sector.Filename : null,
                MaxDiagonalLength = maxDiagonalLength,
                MinDiagonalLength = minDiagonalLength,
                DownloadSize = !isEmptyNode ? sector.DownloadSize : 0
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
            Sectors = sectors.OrderBy(s => s.ParentSectorId).Select(FromSector).ToArray()
        };
        foreach (var sector in sectors.Where(s => s.ParentSectorId == 0 && s.SectorId != 0))
        {
            var subsectors = new List<SectorInfo>();
            subsectors.Add(sectors.Where(s => s.SectorId == 0).First());
            subsectors.Add(sector);
            subsectors.AddRange(GetSectorChildren(sectors, sector.SectorId));
            var subscene = new Scene
            {
                Version = 9,
                ProjectId = parameters.ProjectId,
                ModelId = parameters.ModelId,
                RevisionId = parameters.RevisionId,
                SubRevisionId = -1,
                MaxTreeIndex = maxTreeIndex,
                Unit = "Meters",
                Sectors = subsectors.OrderBy(s => s.ParentSectorId).Select(FromSector).ToArray()
            };
            var subScenePath = Path.Join(outputDirectory.FullName, $"scene_{sector.SectorId}.json");
            JsonUtils.JsonSerializeToFile(subscene, subScenePath, Formatting.Indented);

        }
        var cameraPath = Path.Join(outputDirectory.FullName, "initialCamera.json");
        var scenePath = Path.Join(outputDirectory.FullName, "scene.json");
        JsonUtils.JsonSerializeToFile(cameraPosition, cameraPath);
        JsonUtils.JsonSerializeToFile(scene, scenePath, Formatting.Indented);
    }
    private static IEnumerable<SectorInfo> GetSectorChildren(ImmutableArray<SectorInfo> sectors, uint sectorId)
    {
        var subsectors = sectors.Where(ss => ss.ParentSectorId == sectorId).ToArray();
        foreach (var s in subsectors)
            yield return s;
        foreach (var s2 in subsectors)
        {
            var ssectors = GetSectorChildren(sectors, s2.SectorId);
            foreach (var s in ssectors)
                yield return s;

        }
    }
    public static void ExportSector(SectorInfo sector, string outputDirectory)
    {
        if (sector.Geometries != null && sector.Geometries.Any())
        {
            var filePath = Path.Join(outputDirectory, sector.Filename);
            using var gltfSectorFile = File.Create(filePath);
            GltfWriter.WriteSector(sector.Geometries, gltfSectorFile);
            gltfSectorFile.Flush(true);
        }
        else
        {
            Console.WriteLine($"NOTE: Sector {sector.SectorId} contains no geometries, skipping sector file creation.");
        }
    }
}