namespace CadRevealComposer.Exe;

using CadRevealFbxProvider;
using CadRevealObjProvider;
using CadRevealRvmProvider;
using CommandLine;
using Configuration;
using ModelFormatProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class Program
{
    private const int Success = 0;

    static void Main(string[] args)
    {
        // use full Profile Guided Optimization
        Environment.SetEnvironmentVariable("DOTNET_ReadyToRun", "0");
        Environment.SetEnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1");
        Environment.SetEnvironmentVariable("DOTNET_TieredPGO", "1");

        var result = Parser.Default
            .ParseArguments<CommandLineOptions>(args)
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

        var parameters = new ModelParameters(
            new ProjectId(options.ProjectId),
            new ModelId(options.ModelId),
            new RevisionId(options.RevisionId),
            new InstancingThreshold(options.InstancingThreshold),
            new TemplateCountLimit(options.TemplateCountLimit)
        );

        if (options.NodeNameExcludeRegex != null)
        {
            // Ensure regex is valid.
            if (!RegexUtils.IsValidRegex(options.NodeNameExcludeRegex))
                throw new ArgumentException(
                    $"The {nameof(options.NodeNameExcludeRegex)} is not a valid regex. Check its syntax. "
                        + $"The input was: {options.NodeNameExcludeRegex}"
                );
        }

        var toolsParameters = new ComposerParameters(
            options.NoInstancing,
            options.SingleSector,
            options.SplitIntoZones,
            new NodeNameExcludeRegex(options.NodeNameExcludeRegex)
        );

        if (options.SplitIntoZones)
        {
            throw new ArgumentException("SplitIntoZones is no longer supported. Use regular Octree splitting instead.");
        }

        var providers = new List<IModelFormatProvider>() { new ObjProvider(), new RvmProvider(), new FbxProvider() };

        CadRevealComposerRunner.Process(
            options.InputDirectory,
            options.OutputDirectory,
            parameters,
            toolsParameters,
            providers
        );

        Console.WriteLine($"Export completed. {nameof(CadRevealComposer)} finished in {timer.Elapsed}");
        return Success;
    }
}
