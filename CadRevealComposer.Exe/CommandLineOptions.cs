namespace CadRevealComposer
{
    using CommandLine;
    using System.IO;

    public class CommandLineOptions
    {
        [Option("InputDirectory", Required = true, HelpText = "The path to the RVM and .txt folder you want to convert")]
        public DirectoryInfo InputDirectory {get;set;} = null!;

        [Option("OutputDirectory", Required = true, HelpText = "Where to position the output .i3df file(s)")]
        public DirectoryInfo OutputDirectory { get; set; } = null!;
    }
}