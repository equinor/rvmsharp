﻿namespace RvmSharp.Exe
{
    using CommandLine;
    using System.Collections.Generic;

    internal class Options
    {
        public Options(IEnumerable<string> inputs, string filter, string output, float tolerance, bool optimize)
        {
            Inputs = inputs;
            Filter = filter;
            Output = output;
            Tolerance = tolerance;
            Optimize = optimize;
        }

        [Option('i', "input", Required = true, HelpText = "Input file or folder, can specify multiple items")]
        public IEnumerable<string> Inputs { get; }

        [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
        public string? Filter { get; }

        [Option('o', "output", Required = true, HelpText = "Output folder")]
        public string Output { get; }

        [Option('t', "tolerance", Default = 0.1f, Required = false, HelpText = "Tessellation tolerance")]
        public float Tolerance { get; }

        [Option('z', "optimize", Required = false,
            HelpText = "Replace simple pyramids and snouts by boxes and cylinders")]
        public bool Optimize { get; }
    }
}