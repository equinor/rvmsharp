using CommandLine;
using Microsoft.Extensions.Logging;
using Mop.Hierarchy.Functions;
using System;
using System.Collections.Generic;

namespace Mop.Hierarchy
{
    public static class Program
    {
        internal class HierarchyOptions
        {
            [Option('i', "inputFolder", Required =true, HelpText ="Input folder containing JSON hierarchy files")]
            public string InputFolder { get; set; }
            [Option('o', "outputDatabaseFile", Required =true, HelpText = "Output SQLITE database file")]
            public string OuputFile { get; set; }
        }

        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<HierarchyOptions>(args)
                .WithParsed(Run)
                .WithNotParsed(HandleArgumentError);
        }

        private static void HandleArgumentError(IEnumerable<Error> errors)
        {
            errors.Output();
            Environment.Exit(-1);
        }

        private static void Run(HierarchyOptions options)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger("Mop.Hierarchy");
            var composer = new DatabaseComposer(loggerFactory);
            composer.ComposeDatabase(options.InputFolder, options.OuputFile);
        }
    }
}
