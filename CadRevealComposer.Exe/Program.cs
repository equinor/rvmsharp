namespace CadRevealComposer.Exe
{
    using CommandLine;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

            var toolsParameters = new CadRevealComposerRunner.ToolsParameters(
                @"C:\Appl\Source\rvmsharp\tools\OpenCTM\mesh2ctm.exe",
                @"C:\Appl\Source\rvmsharp\tools\i3df-dump.exe",
                false);

            // TODO: find tools automatically

            CadRevealComposerRunner.Process(options.InputDirectory, options.OutputDirectory, parameters, toolsParameters);

            Console.WriteLine($"Export completed. {nameof(CadRevealComposer)} finished in {timer.Elapsed}");
            return Success;
        }
    }
}