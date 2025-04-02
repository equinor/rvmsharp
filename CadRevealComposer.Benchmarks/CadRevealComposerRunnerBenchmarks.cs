using BenchmarkDotNet.Attributes;
using CadRevealComposer.Configuration;
using CadRevealComposer.ModelFormatProvider;
using CadRevealFbxProvider;
using CadRevealObjProvider;
using CadRevealRvmProvider;

namespace CadRevealComposer.Benchmarks;

using BenchmarkDotNet.Engines;
using Devtools;

[MemoryDiagnoser]
public class CadRevealComposerRunnerBenchmarks
{
    private const long ProjectId = 1;
    private const long ModelId = 1;
    private const long RevisionId = 1;
    private static readonly DirectoryInfo InputDirectory = new("/Users/SSOB/git/Echo/models/raw/HDA/20250331_022910/Huldra AsBuilt/ASB");
    private static readonly DirectoryInfo OutputDirectory = new("/Users/SSOB/git/Echo/models/temp/");
    private static readonly DirectoryInfo DevPrimitiveCacheFolder = new("/Users/SSOB/git/Echo/models/cache/");

    private static readonly ModelParameters Parameters = new(
        new ProjectId(ProjectId),
        new ModelId(ModelId),
        new RevisionId(RevisionId),
        new InstancingThreshold(300),
        new TemplateCountLimit(100)
    );

    [Benchmark]
    public void Process()
    {
        const bool noInstancing = false;
        const bool singleSector = false;
        const bool splitIntoZones = false;
        var nodeNameExcludeRegex = new NodeNameExcludeRegex(null);
        const float simplificationThreshold = 0.0f;

        var toolsParameters = new ComposerParameters(
            noInstancing,
            singleSector,
            splitIntoZones,
            nodeNameExcludeRegex,
            simplificationThreshold,
            null
        );

        List<IModelFormatProvider> providers = [
            new ObjProvider(),
            new RvmProvider(),
            new FbxProvider()
        ];

        CadRevealComposerRunner.Process(
            InputDirectory,
            OutputDirectory,
            Parameters,
            toolsParameters,
            providers
        );
    }

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
