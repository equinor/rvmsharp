namespace CadRevealComposer.Exe
{
    using CommandLine;
    using System.IO;

    // ReSharper disable once ClassNeverInstantiated.Global - Its instantiated by CommandLineUtils NuGet Package
    public class CommandLineOptions
    {
        [Option("InputDirectory", Required = true,
            HelpText = "The path to the RVM and .txt folder you want to convert")]
        public DirectoryInfo InputDirectory { get; init; } = null!;

        [Option("OutputDirectory", Required = true, HelpText = "Where to position the output .i3df file(s)")]
        public DirectoryInfo OutputDirectory { get; init; } = null!;
    }
}