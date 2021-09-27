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
                    new RevisionId(options.RevisionId));

            var programPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var toolsPath = Path.Combine(programPath, "tools");
            var toolsParameters = new CadRevealComposerRunner.ToolsParameters(
                Path.Combine(toolsPath, OperatingSystem.IsMacOS() ? "mesh2ctm.osx" : "mesh2ctm.exe"),
                Path.Combine(toolsPath, "i3df-dump.exe"), // TODO: support OSX
                options.GenerateSectorDumpFiles);

            Debug.Assert(File.Exists(toolsParameters.I3dfDumpToolPath));
            Debug.Assert(File.Exists(toolsParameters.Mesh2CtmToolPath));

            CadRevealComposerRunner.Process(options.InputDirectory, options.OutputDirectory, parameters, toolsParameters);

            Console.WriteLine($"Export completed. {nameof(CadRevealComposer)} finished in {timer.Elapsed}");
            return Success;
        }
    }
}