namespace CadRevealFbxProvider;

using BatchUtils;

using CadRevealComposer;
using CadRevealComposer.Configuration;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Primitives;
using CadRevealComposer.Utils;

using Ben.Collections.Specialized;
using Commons;

using System.Diagnostics;

public class FbxProvider : IModelFormatProvider
{
    public IReadOnlyList<CadRevealNode> ParseFiles(IEnumerable<FileInfo> filesToParse, TreeIndexGenerator treeIndexGenerator)
    {
        var workload = FbxWorkload.CollectWorkload(filesToParse.Select(x => x.FullName).ToArray());

        var fbxTimer = Stopwatch.StartNew();

        var teamCityReadRvmFilesLogBlock = new TeamCityLogBlock("Reading Rvm Files");
        var progressReport = new Progress<(string fileName, int progress, int total)>(x =>
        {
            Console.WriteLine($"\t{x.fileName} ({x.progress}/{x.total})");
        });

        var stringInternPool = new BenStringInternPool(new SharedInternPool());

        var instanceIdGenerator = new InstanceIdGenerator();
        var nodes = FbxWorkload.ReadFbxData(workload, treeIndexGenerator, instanceIdGenerator, progressReport, stringInternPool);
        var fileSizesTotal = workload.Sum(w => new FileInfo(w.fbxFilename).Length);
        teamCityReadRvmFilesLogBlock.CloseBlock();

        if (workload.Length == 0)
        {
            // returns empty list if there are no rvm files to process
            return new List<CadRevealNode>();
        }
        Console.WriteLine(
            $"Read FbxData in {fbxTimer.Elapsed}. (~{fileSizesTotal / 1024 / 1024}mb of .fbx files (excluding evtl .txt file size))");

        return nodes;
    }

    public APrimitive[] ProcessGeometries(APrimitive[] geometries, ComposerParameters composerParameters,
        ModelParameters modelParameters)
    {
        return geometries;
    }
}
