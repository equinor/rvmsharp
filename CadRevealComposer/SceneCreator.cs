namespace CadRevealComposer;

using Commons.Utils;
using Configuration;
using HierarchyComposer.Functions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Operations;
using Primitives;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Utils;
using Writers;
using static CadRevealComposer.Operations.CameraPositioning;

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
        float MinNodeDiagonal,
        float MaxNodeDiagonal,
        IReadOnlyList<APrimitive> Geometries,
        BoundingBox SubtreeBoundingBox,
        BoundingBox? GeometryBoundingBox
    )
    {
        public long DownloadSize { get; init; }
    }

    public static void ExportModelMetadata(DirectoryInfo outputDirectory, ModelMetadata metadata)
    {
        var metadataPath = Path.Join(outputDirectory.FullName, "modelMetada.json");
        var metadataString = ModelMetadata.MakeSerializable(metadata);
        File.WriteAllText(metadataPath, metadataString);
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
        CameraPositioning.CameraPosition cameraPosition
    )
    {
        Sector FromSector(SectorInfo sector)
        {
            float maxDiagonalLength = sector.MaxNodeDiagonal;
            float minDiagonalLength = sector.MinNodeDiagonal;

            // TODO: Check if this may be the correct way to handle min and max diagonal values.

            return new Sector
            {
                Id = sector.SectorId,
                ParentId = sector.ParentSectorId,
                SubtreeBoundingBox = new SerializableBoundingBox(
                    Min: SerializableVector3.FromVector3(sector.SubtreeBoundingBox.Min),
                    Max: SerializableVector3.FromVector3(sector.SubtreeBoundingBox.Max)
                ),
                GeometryBoundingBox =
                    sector.GeometryBoundingBox != null
                        ? new SerializableBoundingBox(
                            Min: SerializableVector3.FromVector3(sector.GeometryBoundingBox.Min),
                            Max: SerializableVector3.FromVector3(sector.GeometryBoundingBox.Max)
                        )
                        : null,
                Depth = sector.Depth,
                Path = sector.Path,
                EstimatedTriangleCount = sector.EstimatedTriangleCount,
                EstimatedDrawCallCount = sector.EstimatedDrawCalls,
                SectorFileName = sector.Filename,
                MaxDiagonalLength = maxDiagonalLength,
                MinDiagonalLength = minDiagonalLength,
                DownloadSize = sector.DownloadSize,
                SectorEchoDevMetadata = new SectorEchoDevMetadata()
                {
                    GeometryDistributions = new GeometryDistributionStats(sector.Geometries)
                }
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
        JsonUtils.JsonSerializeToFile(scene, scenePath, writeIndented: EnvUtil.IsDebugBuild); // We don't want indentation in prod, it doubles the size. Format in an editor if needed.
    }

    public static void ExportSectorGeometries(
        IReadOnlyList<APrimitive> geometries,
        string sectorFilename,
        string? outputDirectory
    )
    {
        var filePath = Path.Join(outputDirectory, sectorFilename);
        using var gltfSectorFile = File.Create(filePath);
        GltfWriter.WriteSector(geometries, gltfSectorFile);
        gltfSectorFile.Flush(true);
    }
}
