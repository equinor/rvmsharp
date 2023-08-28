﻿namespace RvmSharp.Exe
{
    using CommandLine;
    using System.Collections.Generic;

    internal class Options
    {
        public Options(IEnumerable<string> inputs, string filter, string output, float tolerance, string nodeIdFile)
        {
            Inputs = inputs;
            Filter = filter;
            Output = output;
            Tolerance = tolerance;
            NodeIdFile = nodeIdFile;
        }

        [Option('i', "input", Required = true, HelpText = "Input file or folder, can specify multiple items")]
        public IEnumerable<string> Inputs { get; }

        [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
        public string? Filter { get; }

        [Option('o', "output", Required = true, HelpText = "Output folder")]
        public string Output { get; }

        [Option('t', "tolerance", Default = 0.1f, Required = false, HelpText = "Tessellation tolerance")]
        public float Tolerance { get; }
        
        [Option("nodeidfile", Default = null, Required = false, HelpText = "ASCII file containing node ID to start counting from. Node Id will be stored in vertex color channels of the meshes")]
        public string NodeIdFile { get; }
    }
}