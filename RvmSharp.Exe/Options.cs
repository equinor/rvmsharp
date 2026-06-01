namespace RvmSharp.Exe;

using System.Collections.Generic;
using CommandLine;

internal class Options
{
    [Option('i', "input", Required = true, HelpText = "Input file or folder, can specify multiple items")]
    public IEnumerable<string> Inputs { get; init; } = [];

    [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
    public string? Filter { get; init; }

    [Option('o', "output", Required = true, HelpText = "Output folder")]
    public string Output { get; init; } = string.Empty;

    [Option('t', "tolerance", Default = 0.1f, Required = false, HelpText = "Tessellation tolerance")]
    public float Tolerance { get; init; }

    [Option('x', "exclude-attribute", Required = false, HelpText = "Exclude nodes (and all children) whose attribute value matches the pattern. Format: 'AttributeKey=ValueRegex' (value is case-insensitive regex). Specify multiple values space-separated after the flag. Use '|' in the regex to match multiple values for the same key, e.g. 'Description=STRU-ANODE|STRU-SOFTVOLUME'. Requires a paired .txt attributes file.")]
    public IEnumerable<string> ExcludeAttributes { get; init; } = [];

    [Option("tag-naming", Default = false, Required = false, HelpText = "Name exported OBJ objects as 'Tag/ExtraInfo' (spaces replaced with dashes). Uses the nearest ancestor's Tag attribute as the base name. Requires a paired .txt attributes file.")]
    public bool TagNaming { get; init; }
}
