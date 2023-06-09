namespace RvmSharp.Exe
{
    using CommandLine;
    using System.Collections.Generic;

    internal class Options
    {
        public Options(IEnumerable<string> inputs, string filter, string output, float tolerance, uint nodeId)
        {
            Inputs = inputs;
            Filter = filter;
            Output = output;
            Tolerance = tolerance;
            NodeId = nodeId;
        }

        [Option('i', "input", Required = true, HelpText = "Input file or folder, can specify multiple items")]
        public IEnumerable<string> Inputs { get; }

        [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
        public string? Filter { get; }

        [Option('o', "output", Required = true, HelpText = "Output folder")]
        public string Output { get; }

        [Option('t', "tolerance", Default = 0.1f, Required = false, HelpText = "Tessellation tolerance")]
        public float Tolerance { get; }
        
        [Option('n', "nodeid", Default = null, Required = false, HelpText = "Node ID to start counting from, if this parameter is set Node Id will be stored in vertex color channels of the meshes")]
        public uint? NodeId { get; }
    }
}