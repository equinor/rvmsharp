namespace CadRevealComposer;

using Configuration;
using IdProviders;
using ModelFormatProvider;
using Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public static class CadRevealComposerRunner
{
    public static void Process(
        DirectoryInfo inputFolderPath,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        IReadOnlyList<IModelFormatProvider> modelFormatProviders)
    {
        List<CadRevealNode> nodesToProcess = new List<CadRevealNode>();
        var treeIndexGenerator = new TreeIndexGenerator();
        foreach (IModelFormatProvider modelFormatProvider in modelFormatProviders)
        {
            var timer = Stopwatch.StartNew();
            IReadOnlyList<CadRevealNode> cadRevealNodes =
                modelFormatProvider.ParseFiles(inputFolderPath.EnumerateFiles(), treeIndexGenerator);
            nodesToProcess.AddRange(cadRevealNodes);
            Console.WriteLine(
                $"Imported all files for {modelFormatProvider.GetType().Name} in {timer.Elapsed}. Got {cadRevealNodes.Count} nodes.");
        }

        ProcessNodes(nodesToProcess, outputDirectory, modelParameters, composerParameters, treeIndexGenerator, modelFormatProviders);
    }

    public static void ProcessNodes(IReadOnlyList<CadRevealNode> allNodes,
        DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        TreeIndexGenerator treeIndexGenerator,
        IReadOnlyList<IModelFormatProvider> modelFormatProviders)
    {
        var totalTimeElapsed = Stopwatch.StartNew();
        var exportHierarchyDatabaseTask = Task.Run(() =>
        {
            var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
            SceneCreator.ExportHierarchyDatabase(databasePath, allNodes);
            Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
        });
        var geometries = allNodes
            .AsParallel()
            .AsOrdered()
            .SelectMany(x => x.Geometries)
            .ToArray();

        foreach (var b in modelFormatProviders)
        {
            var geometriesIncludingMeshes = b.ProcessGeometries(geometries, composerParameters, modelParameters);

            var stopwatch = Stopwatch.StartNew();

            SectorSplitter.ProtoSector[] sectors;
            if (composerParameters.SingleSector)
            {
                sectors = SectorSplitter.CreateSingleSector(geometriesIncludingMeshes).ToArray();
            }
            else if (composerParameters.SplitIntoZones)
            {
                var zones = ZoneSplitter.SplitIntoZones(geometriesIncludingMeshes, outputDirectory);
                Console.WriteLine($"Split into {zones.Length} zones in {stopwatch.Elapsed}");
                stopwatch.Restart();

                sectors = SectorSplitter.SplitIntoSectors(zones)
                    .OrderBy(x => x.SectorId)
                    .ToArray();
                Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");
                stopwatch.Restart();
            }
            else
            {
                sectors = SectorSplitter.SplitIntoSectors(geometriesIncludingMeshes)
                    .OrderBy(x => x.SectorId)
                    .ToArray();
                Console.WriteLine($"Split into {sectors.Length} sectors in {stopwatch.Elapsed}");
                stopwatch.Restart();
            }

            var sectorInfos = sectors
                .Select(s => SerializeSector(s, outputDirectory.FullName))
                .ToArray();

            Console.WriteLine($"Serialized {sectors.Length} sectors in {stopwatch.Elapsed}");
            stopwatch.Restart();

            var sectorsWithDownloadSize = CalculateDownloadSizes(sectorInfos, outputDirectory).ToImmutableArray();
            var cameraPosition = CameraPositioning.CalculateInitialCamera(geometriesIncludingMeshes);
            SceneCreator.WriteSceneFile(
                sectorsWithDownloadSize,
                modelParameters,
                outputDirectory,
                treeIndexGenerator.CurrentMaxGeneratedIndex,
                cameraPosition);

            Console.WriteLine($"Wrote scene file in {stopwatch.Elapsed}");
            stopwatch.Restart();

            Task.WaitAll(exportHierarchyDatabaseTask);

            Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
            Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
        }

        
    }

    private static SceneCreator.SectorInfo SerializeSector(SectorSplitter.ProtoSector p, string outputDirectory)
    {
        var estimateDrawCalls = DrawCallEstimator.Estimate(p.Geometries);

        var sectorInfo = new SceneCreator.SectorInfo(
            p.SectorId,
            p.ParentSectorId,
            p.Depth,
            p.Path,
            $"sector_{p.SectorId}.glb",
            EstimatedTriangleCount: estimateDrawCalls.EstimatedTriangleCount,
            EstimatedDrawCalls: estimateDrawCalls.EstimatedDrawCalls,
            p.Geometries,
            p.BoundingBoxMin,
            p.BoundingBoxMax);
        SceneCreator.ExportSector(sectorInfo, outputDirectory);

        return sectorInfo;
    }

    private static IEnumerable<SceneCreator.SectorInfo> CalculateDownloadSizes(
        IEnumerable<SceneCreator.SectorInfo> sectors, DirectoryInfo outputDirectory)
    {
        foreach (var sector in sectors)
        {
            var filepath = Path.Combine(outputDirectory.FullName, sector.Filename);
            yield return sector with { DownloadSize = new FileInfo(filepath).Length };
        }
    }

    

    

    
}