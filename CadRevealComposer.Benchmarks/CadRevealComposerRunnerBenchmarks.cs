using BenchmarkDotNet.Attributes;
using CadRevealComposer.Configuration;
using CadRevealComposer.ModelFormatProvider;
using CadRevealFbxProvider;
using CadRevealObjProvider;
using CadRevealRvmProvider;
using BenchmarkDotNet.Engines;

namespace CadRevealComposer.Benchmarks;

[SimpleJob(RunStrategy.ColdStart, iterationCount: 3)]
[MemoryDiagnoser]
public class CadRevealComposerRunnerBenchmarks
{
    private const long ProjectId = 1;
    private const long ModelId = 1;
    private const long RevisionId = 1;
    private static readonly DirectoryInfo InputDirectory = new("/Users/SSOB/git/Echo/models/raw/HDA/20250331_022910/HuldraBenchmark/ASB");
    private static readonly DirectoryInfo OutputDirectory = new("/Users/SSOB/git/Echo/models/temp/");
    private static readonly DirectoryInfo DevPrimitiveCacheFolder = new("/Users/SSOB/git/Echo/models/cache/");

    private ComposerParameters _toolsParameters;
    private List<IModelFormatProvider> _providers;

    private static readonly ModelParameters Parameters = new(
        new ProjectId(ProjectId),
        new ModelId(ModelId),
        new RevisionId(RevisionId),
        new InstancingThreshold(300),
        new TemplateCountLimit(100)
    );

    [GlobalSetup]
    public void GlobalSetup()
    {
        const bool noInstancing = false;
        const bool singleSector = false;
        const bool splitIntoZones = false;
        var nodeNameExcludeRegex = new NodeNameExcludeRegex(null);
        const float simplificationThreshold = 0.0f;

        _toolsParameters = new ComposerParameters(
            noInstancing,
            singleSector,
            splitIntoZones,
            nodeNameExcludeRegex,
            simplificationThreshold,
            null
        );

        _providers = [
            new ObjProvider(),
            new RvmProvider(),
            new FbxProvider()
        ];
    }

    [Benchmark]
    public void ProcessRvm()
    {
        CadRevealComposerRunner.Process(
            InputDirectory,
            OutputDirectory,
            Parameters,
            _toolsParameters,
            [new RvmProvider()]
        );
    }
/*
    [Benchmark]
    public void ProcessFbx()
    {
        CadRevealComposerRunner.Process(
            InputDirectory,
            OutputDirectory,
            Parameters,
            _toolsParameters,
            [new FbxProvider()]
        );
    }

    [Benchmark]
    public void Process()
    {
        CadRevealComposerRunner.Process(
            InputDirectory,
            OutputDirectory,
            Parameters,
            _toolsParameters,
            _providers
        );
    }
*/
    [IterationCleanup]
    public void IterationCleanup()
    {
        Console.WriteLine("Cleanup");
        foreach (FileInfo fileInfo in OutputDirectory.GetFiles())
        {
            fileInfo.Delete();
        }
    }
}
