namespace CadRevealComposer;

using Configuration;
using HierarchyComposer.Functions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Operations;
using Primitives;
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
        IReadOnlyList<APrimitive> Geometries,
        BoundingBox SubtreeBoundingBox,
        BoundingBox GeometryBoundingBox
    )
    {
        public long DownloadSize { get; init; }
    }

    public static void ExportHierarchyDatabase(string databasePath, IReadOnlyList<CadRevealNode> allNodes)
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
            //if (!sector.Geometries.Any())
            //    throw new Exception($"Sector {sector.SectorId} contains Zero geometries. This will cause issues in Reveal. Stopping!: {sector}");

            float maxDiagonalLength;
            float minDiagonalLength;

            if (!sector.Geometries.Any())
            {
                maxDiagonalLength = 0;
                minDiagonalLength = 0;
            }
            else
            {
                maxDiagonalLength = sector.Geometries.Max(x => x.AxisAlignedBoundingBox.Diagonal);
                minDiagonalLength = sector.Geometries.Min(x => x.AxisAlignedBoundingBox.Diagonal);
            }

            // TODO: Check if this may be the correct way to handle min and max diagonal values.

            return new Sector
            {
                Id = sector.SectorId,
                ParentId = sector.ParentSectorId,
                SubtreeBoundingBox =
                    new SerializableBoundingBox(
                        Min: SerializableVector3.FromVector3(sector.SubtreeBoundingBox.Min),
                        Max: SerializableVector3.FromVector3(sector.SubtreeBoundingBox.Max)
                    ),
                GeometryBoundingBox =
                    new SerializableBoundingBox(
                        Min: SerializableVector3.FromVector3(sector.GeometryBoundingBox.Min),
                        Max: SerializableVector3.FromVector3(sector.GeometryBoundingBox.Max)
                    ),
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

    public static void ExportSectorGeometries(IReadOnlyList<APrimitive> geometries, string sectorFilename, string? outputDirectory)
    {
        var filePath = Path.Join(outputDirectory, sectorFilename);
        using var gltfSectorFile = File.Create(filePath);
        GltfWriter.WriteSector(geometries, gltfSectorFile);
        gltfSectorFile.Flush(true);
    }
}