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

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {

            var result = Parser.Default.ParseArguments<CommandLineOptions>(args).MapResult(RunOptionsAndReturnExitCode, HandleParseError);
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
            ValidateOptions(options);

            CadRevealComposerRunner.Process(options.InputDirectory, options.OutputDirectory);

            Console.WriteLine("Export completed. Total runtime: " + timer.Elapsed);
            return Success;
        }

        private static void ValidateOptions(CommandLineOptions options)
        {
            if (!options.InputDirectory.Exists)
            {
                throw new DirectoryNotFoundException(
                    $"{nameof(options.InputDirectory)}: Could not find any directory at path {options.InputDirectory.FullName}");
            }

            // ReSharper disable once InvertIf
            if (!options.OutputDirectory.Exists)
            {

                if (options.OutputDirectory.Parent?.Exists == true)
                {
                    Directory.CreateDirectory(options.OutputDirectory.FullName);
                }
                else
                {
                    throw new DirectoryNotFoundException(
                        $"{nameof(options.InputDirectory)}: Could not find the output directory OR its parent. Is the path {options.OutputDirectory} correct?");
                }
            }
        }
    }
}