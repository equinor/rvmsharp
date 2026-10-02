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

    [Option("native-instances-output", Required = false, HelpText = "Optional JSON sidecar containing local-space reusable templates and their original RVM placement transforms.")]
    public string? NativeInstancesOutput { get; init; }

    [Option("native-inventory-output", Required = false, HelpText = "Optional JSON sidecar containing native primitive/template counts, dimensions, and tessellated export-size estimates.")]
    public string? NativeInventoryOutput { get; init; }

    [Option("native-instances-csv-output", Required = false, HelpText = "Optional compact CSV sidecar containing native instance transforms and metadata. Replaces --native-instances-output when selected by the runner.")]
    public string? NativeInstancesCsvOutput { get; init; }

    [Option("native-inventory-csv-output", Required = false, HelpText = "Optional compact CSV sidecar containing native template and kind inventory records. Replaces --native-inventory-output when selected by the runner.")]
    public string? NativeInventoryCsvOutput { get; init; }

    [Option("native-remains-obj", Required = false, HelpText = "Optional OBJ containing only single-use native geometry placements.")]
    public string? NativeRemainsObj { get; init; }

    [Option("native-unique-obj", Required = false, HelpText = "Optional OBJ containing each reused native local-space geometry template once.")]
    public string? NativeUniqueObj { get; init; }

    [Option("native-objects-per-file", Default = 500, Required = false, HelpText = "Maximum objects per native remains or unique OBJ part.")]
    public int NativeObjectsPerFile { get; init; } = 500;

    [Option("native-template-usage-csv", Required = false, HelpText = "Optional CSV companion containing per-template reuse counts and tessellated size estimates.")]
    public string? NativeTemplateUsageCsv { get; init; }
}
