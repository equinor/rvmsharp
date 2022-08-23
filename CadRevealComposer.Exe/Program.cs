namespace CadRevealComposer.Exe;

using CommandLine;
using Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public static class Program
{
    private const int Success = 0;

    static void Main(string[] args)
    {
        // use full Profile Guided Optimization
        Environment.SetEnvironmentVariable("DOTNET_ReadyToRun", "0");
        Environment.SetEnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1");
        Environment.SetEnvironmentVariable("DOTNET_TieredPGO", "1");

        var result = Parser.Default.ParseArguments<CommandLineOptions>(args)
            .MapResult(RunOptionsAndReturnExitCode, HandleParseError);
        Environment.Exit(result);
    }

    private static int HandleParseError(IEnumerable<Error> arg)
    {
        // TODO: Handle errors?
        Console.WriteLine(arg.First());
        return -1;
    }

    private static int RunOptionsAndReturnExitCode(CommandLineOptions options)
    {
        var timer = Stopwatch.StartNew();
        CommandLineOptions.AssertValidOptions(options);

        var parameters =
            new ModelParameters(
                new ProjectId(options.ProjectId),
                new ModelId(options.ModelId),
                new RevisionId(options.RevisionId),
                new InstancingThreshold(options.InstancingThreshold)
            );

        var programPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        var toolsPath = Path.Combine(programPath!, "tools");
        var toolsParameters = new ComposerParameters(
            Path.Combine(toolsPath, OperatingSystem.IsMacOS() ? "mesh2ctm.osx" : "mesh2ctm.exe"),
            options.NoInstancing,
            options.SingleSector,
            options.SplitIntoZones,
            options.UseEmptyRootSector);

        if (!File.Exists(toolsParameters.Mesh2CtmToolPath))
        {
            Console.WriteLine($"Not found: {toolsParameters.Mesh2CtmToolPath}");
            return 1;
        }

        CadRevealComposerRunner.Process(options.InputDirectory, options.OutputDirectory, parameters, toolsParameters);

        Console.WriteLine($"Export completed. {nameof(CadRevealComposer)} finished in {timer.Elapsed}");
        return Success;
    }
}