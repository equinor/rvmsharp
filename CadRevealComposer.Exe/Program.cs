namespace CadRevealComposer.Exe
{
    using CommandLine;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Program
    {
        private const int Success = 0;

        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            
            var result = Parser.Default.ParseArguments<Options>(args).MapResult(RunOptionsAndReturnExitCode, HandleParseError);
            Environment.Exit(result);
        }

        private static int HandleParseError(IEnumerable<Error> arg)
        {
            // TODO: Handle errors?
            Console.WriteLine(arg.First());
            return -1;
        }

        private static int RunOptionsAndReturnExitCode(Options arg)
        {
            CadRevealComposerRunner.Process(Options.InputRvmPath);
            
            return Success;
        }
    }
}