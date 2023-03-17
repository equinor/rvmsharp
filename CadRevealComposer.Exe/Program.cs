namespace CadRevealComposer.Exe;

using CadRevealObjProvider;
using CadRevealRvmProvider;
using CadRevealFbxProvider;
using CommandLine;
using Configuration;
using ModelFormatProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;

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
                new InstancingThreshold(options.InstancingThreshold),
                new TemplateCountLimit(options.TemplateCountLimit)
            );
        var programPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        var toolsPath = Path.Combine(programPath!, "tools");
        var toolsParameters = new ComposerParameters(
            Path.Combine(toolsPath, OperatingSystem.IsMacOS() ? "mesh2ctm.osx" : "mesh2ctm.exe"),
            options.NoInstancing,
            options.SingleSector,
            options.SplitIntoZones);

        if (!File.Exists(toolsParameters.Mesh2CtmToolPath))
        {
            Console.WriteLine($"Not found: {toolsParameters.Mesh2CtmToolPath}");
            return 1;
        }

        var providers = new List<IModelFormatProvider>() { new ObjProvider(), new RvmProvider(), new FbxProvider() };

        CadRevealComposerRunner.Process(
            options.InputDirectory,
            options.OutputDirectory,
            parameters,
            toolsParameters,
            providers);

        Console.WriteLine($"Export completed. {nameof(CadRevealComposer)} finished in {timer.Elapsed}");
        return Success;
    }
}