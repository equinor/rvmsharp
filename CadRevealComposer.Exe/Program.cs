namespace CadRevealComposer.Exe
{
    using CommandLine;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public static class Program
    {
        private const int Success = 0;

        static void Main(string[] args)
        {
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
                new CadRevealComposerRunner.Parameters(
                    new ProjectId(options.ProjectId),
                    new ModelId(options.ModelId),
                    new RevisionId(options.RevisionId),
                    options.NoInstancing,
                    options.CreateSingleSector,
                    options.DeterministicOutput);

            var programPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var toolsPath = Path.Combine(programPath!, "tools");
            var toolsParameters = new CadRevealComposerRunner.ToolsParameters(
                Path.Combine(toolsPath, OperatingSystem.IsMacOS() ? "mesh2ctm.osx" : "mesh2ctm.exe"),
                Path.Combine(toolsPath, OperatingSystem.IsMacOS() ? "i3df-dump.osx" : "i3df-dump.exe"),
                options.GenerateSectorDumpFiles);

            if (!File.Exists(toolsParameters.Mesh2CtmToolPath))
            {
                Console.WriteLine($"Not found: {toolsParameters.Mesh2CtmToolPath}");
                return 1;
            }

            if (options.GenerateSectorDumpFiles && !File.Exists(toolsParameters.I3dfDumpToolPath))
            {
                Console.WriteLine($"Not found: {toolsParameters.I3dfDumpToolPath}");
                Console.WriteLine("i3df-dump has to be compiled manually and be placed in the tools folder. See README.md");
                return 1;
            }

            CadRevealComposerRunner.Process(options.InputDirectory, options.OutputDirectory, parameters, toolsParameters);

            Console.WriteLine($"Export completed. {nameof(CadRevealComposer)} finished in {timer.Elapsed}");
            return Success;
        }
    }
}