using BenchmarkDotNet.Running;

namespace CadRevealComposer.Benchmarks;

using BenchmarkDotNet.Configs;

public class Program
{
    public static void Main(string[] args)
    {
        /*var benchmark = new CadRevealComposerRunnerBenchmarks();
        benchmark.Process();*/
        var summary = BenchmarkRunner.Run<CadRevealComposerRunnerBenchmarks>(ManualConfig
                .Create(DefaultConfig.Instance)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true));
    }
}
