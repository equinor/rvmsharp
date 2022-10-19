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
using System.Numerics;
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
        var totalTimeElapsed = Stopwatch.StartNew();

        List<CadRevealNode> nodesToProcess = new List<CadRevealNode>();
        List<Primitives.APrimitive> geometriesToProcess = new List<Primitives.APrimitive>();
        var treeIndexGenerator = new TreeIndexGenerator();

        foreach (IModelFormatProvider modelFormatProvider in modelFormatProviders)
        {
            var timer = Stopwatch.StartNew();
            IReadOnlyList<CadRevealNode> cadRevealNodes =
                modelFormatProvider.ParseFiles(inputFolderPath.EnumerateFiles(), treeIndexGenerator);
            if(cadRevealNodes != null)
            {
                Console.WriteLine(
                    $"Imported all files for {modelFormatProvider.GetType().Name} in {timer.Elapsed}. Got {cadRevealNodes.Count} nodes.");

                if (cadRevealNodes.Count > 0)
                {
                    // collect all nodes for later sector division of the entire scene
                    nodesToProcess.AddRange(cadRevealNodes);

                    //nodesToProcess.Sort((x, y) => (x.TreeIndex < y.TreeIndex) ? 1 : 0);
                    var inputGeometries = cadRevealNodes
                        .AsParallel()
                        .AsOrdered()
                        .SelectMany(x => x.Geometries)
                        .ToArray();

                    var geometriesIncludingMeshes = modelFormatProvider.ProcessGeometries(inputGeometries, composerParameters, modelParameters);
                    geometriesToProcess.AddRange(geometriesIncludingMeshes);
                }
            }

        }

        ProcessPrimitives(geometriesToProcess.ToArray(), outputDirectory, modelParameters, composerParameters, treeIndexGenerator);

        var exportHierarchyDatabaseTask = Task.Run(() =>
        {
            var databasePath = Path.GetFullPath(Path.Join(outputDirectory.FullName, "hierarchy.db"));
            SceneCreator.ExportHierarchyDatabase(databasePath, nodesToProcess);
            Console.WriteLine($"Exported hierarchy database to path \"{databasePath}\"");
        });

        Task.WaitAll(exportHierarchyDatabaseTask);

        Console.WriteLine($"Export Finished. Wrote output files to \"{Path.GetFullPath(outputDirectory.FullName)}\"");
        Console.WriteLine($"Convert completed in {totalTimeElapsed.Elapsed}");
    }

    public static void ProcessPrimitives(Primitives.APrimitive[] allPrimitives, DirectoryInfo outputDirectory,
        ModelParameters modelParameters,
        ComposerParameters composerParameters,
        TreeIndexGenerator treeIndexGenerator)
    {
        var stopwatch = Stopwatch.StartNew();

        SectorSplitter.ProtoSector[] sectors;
        if (composerParameters.SingleSector)
        {
            sectors = SectorSplitter.CreateSingleSector(allPrimitives).ToArray();
        }
        else if (composerParameters.SplitIntoZones)
        {
            var zones = ZoneSplitter.SplitIntoZones(allPrimitives, outputDirectory);
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
            sectors = SectorSplitter.SplitIntoSectors(allPrimitives)
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
        var cameraPosition = CameraPositioning.CalculateInitialCamera(allPrimitives);
        SceneCreator.WriteSceneFile(
            sectorsWithDownloadSize,
            modelParameters,
            outputDirectory,
            treeIndexGenerator.CurrentMaxGeneratedIndex,
            cameraPosition);

        Console.WriteLine($"Wrote scene file in {stopwatch.Elapsed}");
        stopwatch.Restart();
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
            p.SubtreeBoundingBoxMin,
            p.SubtreeBoundingBoxMax,
            p.GeometryBoundingBoxMin,
            p.GeometryBoundingBoxMax
        );
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