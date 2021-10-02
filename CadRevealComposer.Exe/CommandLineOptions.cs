// ReSharper disable UnusedAutoPropertyAccessor.Global -- Unsure if CommandLineOptions handles this
// ReSharper disable MemberCanBePrivate.Global -- Unsure if CommandLineOptions handles this
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global  -- Unsure if CommandLineOptions handles this

namespace CadRevealComposer.Exe
{
    using CommandLine;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;

    // ReSharper disable once ClassNeverInstantiated.Global - Its instantiated by CommandLineUtils NuGet Package
    public class CommandLineOptions
    {
        [Option(longName: "InputDirectory", Required = true,
            HelpText = "The path to the RVM and .txt folder you want to convert")]
        public DirectoryInfo InputDirectory { get; init; } = null!;

        [Option(longName: "OutputDirectory", Required = true, HelpText = "Where to position the output .i3df file(s)")]
        public DirectoryInfo OutputDirectory { get; init; } = null!;

        [Option(longName: "ProjectId", Required = true, HelpText = "A number identifying the current Echo Project."),
         Range(minimum: 0, Double.PositiveInfinity)]
        public long ProjectId { get; init; }

        [Option(longName: "ModelId",
             Required = true,
             HelpText =
                 "A number identifying the Echo Model. A Model is the \"filter\" applied. Each processing of a filter should have the same ModelId, so we can compare revisions between Models."
         ),
         Range(minimum: 0, Double.PositiveInfinity)
        ]
        public long ModelId { get; init; }

        [Option(longName: "RevisionId",
             Required = true,
             HelpText =
                 "A number describing the RevisionId. This is the number where clients decide if they need to update a Model based on."),
         Range(minimum: 0, Double.PositiveInfinity),
        ]
        public long RevisionId { get; init; }

        [Option(longName: "NoInstancing", Required = false, HelpText = "Create triangle meshes instead of instance meshes.")]
        public bool NoInstancing { get; init; }

        [Option(longName: "CreateSingleSector", Required = false, HelpText = "Create a single sector.")]
        public bool CreateSingleSector { get; init; }

        [Option(longName: "DeterministicOutput", Required = false, HelpText = "Disables parallel processing in order to create a deterministic ordering.")]
        public bool DeterministicOutput { get; init; }

        public static void AssertValidOptions(CommandLineOptions options)
        {
            // Validate DataAttributes
            Validator.ValidateObject(options, new ValidationContext(options), true);

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
                        $"{nameof(options.InputDirectory)}: Could not find the output directory OR its parent. Is the path \"{options.OutputDirectory}\" correct?");
                }
            }
        }
    }
}