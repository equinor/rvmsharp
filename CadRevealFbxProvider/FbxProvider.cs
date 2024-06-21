namespace CadRevealFbxProvider;

using System.Diagnostics;
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

public class FbxProvider : IModelFormatProvider
{
    public (IReadOnlyList<CadRevealNode>, ModelMetadata?) ParseFiles(
        IEnumerable<FileInfo> filesToParse,
        TreeIndexGenerator treeIndexGenerator,
        InstanceIdGenerator instanceIdGenerator,
        NodeNameFiltering nodeNameFiltering
    )
    {
        var workload = FbxWorkload.CollectWorkload(filesToParse.Select(x => x.FullName).ToArray());
        if (!workload.Any())
        {
            Console.WriteLine("Found no .fbx files. Skipping FBX Parser.");
            return (new List<CadRevealNode>(), null);
        }

        var fbxTimer = Stopwatch.StartNew();

        var teamCityReadFbxFilesLogBlock = new TeamCityLogBlock("Reading Fbx Files");
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });

        var stringInternPool = new BenStringInternPool(new SharedInternPool());

        (var nodes, var metadata) = FbxWorkload.ReadFbxData(
            workload,
            treeIndexGenerator,
            instanceIdGenerator,
            nodeNameFiltering,
            progressReport,
            stringInternPool
        );
        var fileSizesTotal = workload.Sum(w => new FileInfo(w.fbxFilename).Length);
        teamCityReadFbxFilesLogBlock.CloseBlock();

        if (workload.Length == 0)
        {
            // returns empty list if there are no rvm files to process
            return (new List<CadRevealNode>(), null);
        }
        Console.WriteLine(
            $"Read FbxData in {fbxTimer.Elapsed}. (~{fileSizesTotal / 1024 / 1024}mb of .fbx files (excluding evtl .csv file size))"
        );

        return (nodes, metadata);
    }

    public APrimitive[] ProcessGeometries(
        APrimitive[] geometries,
        ComposerParameters composerParameters,
        ModelParameters modelParameters,
        InstanceIdGenerator instanceIdGenerator
    )
    {
        return geometries;
    }
}
