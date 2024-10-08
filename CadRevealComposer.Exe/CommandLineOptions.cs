﻿// ReSharper disable UnusedAutoPropertyAccessor.Global -- Unsure if CommandLineOptions handles this
// ReSharper disable MemberCanBePrivate.Global -- Unsure if CommandLineOptions handles this
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global  -- Unsure if CommandLineOptions handles this

namespace CadRevealComposer.Exe;

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using CommandLine;

// ReSharper disable once ClassNeverInstantiated.Global - Its instantiated by CommandLineUtils NuGet Package
public class CommandLineOptions
{
    [Option(
        longName: "InputDirectory",
        shortName: 'i',
        Required = true,
        HelpText = "The path to the RVM and .txt folder you want to convert"
    )]
    public DirectoryInfo InputDirectory { get; init; } = null!;

    [Option(
        longName: "OutputDirectory",
        shortName: 'o',
        Required = true,
        HelpText = "Where to position the output .i3df file(s)"
    )]
    public DirectoryInfo OutputDirectory { get; init; } = null!;

    [
        Option(
            longName: "ProjectId",
            shortName: 'p',
            Required = true,
            HelpText = "A number identifying the current Echo Project."
        ),
        Range(minimum: 0, Double.PositiveInfinity)
    ]
    public long ProjectId { get; init; }

    [
        Option(
            longName: "ModelId",
            shortName: 'm',
            Required = true,
            HelpText = "A number identifying the Echo Model. A Model is the \"filter\" applied. Each processing of a filter should have the same ModelId, so we can compare revisions between Models."
        ),
        Range(minimum: 0, Double.PositiveInfinity)
    ]
    public long ModelId { get; init; }

    [
        Option(
            longName: "RevisionId",
            shortName: 'r',
            Required = true,
            HelpText = "A number describing the RevisionId. This is the number where clients decide if they need to update a Model based on."
        ),
        Range(minimum: 0, Double.PositiveInfinity)
    ]
    public long RevisionId { get; init; }

    [Option(
        longName: "NoInstancing",
        Required = false,
        HelpText = "Create triangle meshes instead of instance meshes."
    )]
    public bool NoInstancing { get; init; }

    [Option(longName: "SingleSector", shortName: 's', Required = false, HelpText = "Create a single sector.")]
    public bool SingleSector { get; init; }

    [Option(
        longName: "NodeNameExcludeRegex",
        Required = false,
        HelpText = "A regex matching node names to _exclude_ from export. Not case sensitive."
    )]
    public string? NodeNameExcludeRegex { get; init; } = null;

    [Option(longName: "SplitIntoZones", shortName: 'z', Required = false, HelpText = "Split models into zones.")]
    public bool SplitIntoZones { get; init; }

    [
        Option(
            longName: "InstancingThreshold",
            shortName: 't',
            Default = (uint)300,
            Required = false,
            HelpText = "Require at least this many matches to mark a mesh as Instanced. If not specified this will have a default value. Minimum 1."
        ),
        Range(1, uint.MaxValue)
    ]
    public uint InstancingThreshold { get; set; }

    [
        Option(
            longName: "TemplateCountLimit",
            Default = (uint)100,
            Required = false,
            HelpText = "Sets the maximal number of template meshes created. If not specified this will have a default value."
        ),
        Range(1, uint.MaxValue)
    ]
    public uint TemplateCountLimit { get; set; }

    [
        Option(
            longName: "SimplificationThreshold",
            Default = 0.0f,
            Required = false,
            HelpText = "The threshold used in simplification in meters. A reasonable value is 1 cm, i.e. 0.01."
        ),
        Range(0, float.MaxValue)
    ]
    public float SimplificationThreshold { get; set; }

    [Option(
        longName: "DevPrimitiveCacheFolder",
        Required = false,
        HelpText = "DevTool: The path to the primitive cache folder. If not set the primitive-cache will be disabled. By default the DevCache will use the input folders name to determine the cache file."
    )]
    public DirectoryInfo? DevPrimitiveCacheFolder { get; init; } = null;

    public static void AssertValidOptions(CommandLineOptions options)
    {
        // Validate DataAttributes
        Validator.ValidateObject(options, new ValidationContext(options), true);

        if (!options.InputDirectory.Exists)
        {
            throw new DirectoryNotFoundException(
                $"{nameof(options.InputDirectory)}: Could not find any directory at path {options.InputDirectory.FullName}"
            );
        }

        if (!options.OutputDirectory.Exists)
        {
            // Creates the whole path
            Directory.CreateDirectory(options.OutputDirectory.FullName);
        }

        if (options.DevPrimitiveCacheFolder != null && !options.DevPrimitiveCacheFolder.Exists)
        {
            Directory.CreateDirectory(options.DevPrimitiveCacheFolder.FullName);
            Console.WriteLine(
                $"Created {nameof(options.DevPrimitiveCacheFolder)} at path: "
                    + options.DevPrimitiveCacheFolder.FullName
            );
        }
    }
}
