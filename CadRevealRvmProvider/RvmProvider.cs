namespace CadRevealRvmProvider;

using Ben.Collections.Specialized;
using CadRevealComposer;
using CadRevealComposer.IdProviders;
using CadRevealComposer.ModelFormatProvider;
using CadRevealComposer.Utils;
using RvmSharp.BatchUtils;
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



}