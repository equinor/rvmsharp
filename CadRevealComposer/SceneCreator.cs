namespace CadRevealComposer;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CadRevealComposer.Operations.SectorSplitting;
using Commons.Utils;
using Configuration;
using HierarchyComposer.Functions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Operations;
using Primitives;
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
        var metadataPath = Path.Join(outputDirectory.FullName, "modelMetadata.json");
        var metadataString = ModelMetadata.Serialize(metadata);
        File.WriteAllText(metadataPath, metadataString);
    }

    public static void ExportHierarchyDatabase(string databasePath, IReadOnlyList<CadRevealNode> allNodes)
    {
        var nodes = HierarchyComposerConverter.ConvertToHierarchyNodes(allNodes);

        ILogger<DatabaseComposer> databaseLogger = NullLogger<DatabaseComposer>.Instance;
        var exporter = new DatabaseComposer(databaseLogger);
        exporter.ComposeDatabase(nodes.ToList(), Path.GetFullPath(databasePath));
    }

    public static void CreateSceneFile(
        APrimitive[] allPrimitives,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ulong maxTreeIndex,
        Stopwatch stopwatch,
        InternalSector[] sectors
    )
    {
        var sectorInfos = sectors.Select(s => SerializeSector(s, outputDirectory.FullName)).ToArray();

        Console.WriteLine($"Serialized {sectors.Length} sectors in {stopwatch.Elapsed}");
        stopwatch.Restart();

        var sectorsWithDownloadSize = CalculateDownloadSizes(sectorInfos, outputDirectory).ToImmutableArray();

        PrintSectorStats(sectorsWithDownloadSize);

        var cameraPosition = CameraPositioning.CalculateInitialCamera(allPrimitives);
        WriteSceneFile(sectorsWithDownloadSize, modelParameters, outputDirectory, maxTreeIndex, cameraPosition);
    }

    private static void WriteSceneFile(
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

    private static void ExportSectorGeometries(
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

    private static SectorInfo SerializeSector(InternalSector p, string outputDirectory)
    {
        var (estimatedTriangleCount, estimatedDrawCalls) = DrawCallEstimator.Estimate(p.Geometries);

        string? sectorFilename = !p.IsHighlightSector
            ? p.Geometries.Any()
                ? $"sector_{p.SectorId}.glb"
                : null
            : p.Geometries.Any()
                ? $"highlight_sector_{p.SectorId}.glb"
                : null;

        var sectorInfo = new SectorInfo(
            SectorId: p.SectorId,
            ParentSectorId: p.ParentSectorId,
            Depth: p.Depth,
            Path: p.Path,
            Filename: sectorFilename,
            EstimatedTriangleCount: estimatedTriangleCount,
            EstimatedDrawCalls: estimatedDrawCalls,
            MinNodeDiagonal: p.MinNodeDiagonal,
            MaxNodeDiagonal: p.MaxNodeDiagonal,
            Geometries: p.Geometries,
            SubtreeBoundingBox: p.SubtreeBoundingBox,
            GeometryBoundingBox: p.GeometryBoundingBox
        );

        if (sectorFilename != null)
            ExportSectorGeometries(sectorInfo.Geometries, sectorFilename, outputDirectory);
        return sectorInfo;
    }

    private static IEnumerable<SectorInfo> CalculateDownloadSizes(
        IEnumerable<SectorInfo> sectors,
        DirectoryInfo outputDirectory
    )
    {
        foreach (var sector in sectors)
        {
            if (string.IsNullOrEmpty(sector.Filename))
            {
                yield return sector;
            }
            else
            {
                var filepath = Path.Combine(outputDirectory.FullName, sector.Filename);
                yield return sector with
                {
                    DownloadSize = new FileInfo(filepath).Length
                };
            }
        }
    }

    private static void PrintSectorStats(ImmutableArray<SectorInfo> sectorsWithDownloadSize)
    {
        // Helpers
        static float BytesToMegabytes(long bytes) => bytes / 1024f / 1024f;

        (string, string, string, string, string, string, string, string, string, string) headers = (
            "Depth",
            "Sectors",
            "μ drawCalls",
            "μ Triangles",
            "μ sectDiam",
            "^ sectDiam",
            "v sectDiam",
            "μ s/l part",
            "μ DLsize",
            "v DLsize"
        );
        // Add stuff you would like for a quick overview here:
        using (new TeamCityLogBlock("Sector Stats"))
        {
            Console.WriteLine($"Sector Count: {sectorsWithDownloadSize.Length}");
            Console.WriteLine(
                $"Sum all sectors .glb size megabytes: {BytesToMegabytes(sectorsWithDownloadSize.Sum(x => x.DownloadSize)):F2}MB"
            );
            Console.WriteLine(
                $"Total Estimated Triangle Count: {sectorsWithDownloadSize.Sum(x => x.EstimatedTriangleCount)}"
            );
            Console.WriteLine($"Depth Stats:");
            Console.WriteLine(
                $"|{headers.Item1, 5}|{headers.Item2, 7}|{headers.Item3, 10}|{headers.Item4, 11}|{headers.Item5, 10}|{headers.Item6, 10}|{headers.Item7, 10}|{headers.Item8, 17}|{headers.Item9, 10}|{headers.Item10, 8}|"
            );
            Console.WriteLine(new String('-', 110));
            foreach (IGrouping<long, SectorInfo> g in sectorsWithDownloadSize.GroupBy(x => x.Depth).OrderBy(x => x.Key))
            {
                var anyHasGeometry = g.Any(x => x.Geometries.Any());
                var sizeMinAvgExceptEmpty = anyHasGeometry
                    ? g.Where(x => x.Geometries.Any()).Average(x => x.MinNodeDiagonal)
                    : 0;
                var sizeMaxAvgExceptEmpty = anyHasGeometry
                    ? g.Where(x => x.Geometries.Any()).Average(x => x.MaxNodeDiagonal)
                    : 0;
                var maxSize = "N/A";
                if (g.Count() > 1)
                {
                    maxSize = $"{BytesToMegabytes(g.Max(x => x.DownloadSize)):F2}";
                }

                var formatted = $@"|
{g.Key, 5}|
{g.Count(), 7}|
{g.Average(x => x.EstimatedDrawCalls), 11:F2}|
{g.Average(x => x.EstimatedTriangleCount), 11:F0}|
{g.Average(x => x.SubtreeBoundingBox.Diagonal), 9:F2}m|
{g.Min(x => x.SubtreeBoundingBox.Diagonal), 9:F2}m|
{g.Max(x => x.SubtreeBoundingBox.Diagonal), 9:F2}m|
{sizeMinAvgExceptEmpty, 7:F2}m/{sizeMaxAvgExceptEmpty, 7:F2}m|
{g.Average(x => x.DownloadSize / 1024f / 1024f), 8:F}MB|
{maxSize, 8}|
".Replace(Environment.NewLine, "");
                Console.WriteLine(formatted);
            }
        }
    }
}
