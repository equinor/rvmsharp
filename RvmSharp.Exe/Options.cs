namespace RvmSharp.Exe
{
    using CommandLine;
    using System.Collections.Generic;

    internal class Options
    {
        private readonly IEnumerable<string>_inputs;
        private readonly string _filter;
        private readonly string _output;
        private readonly float _tolerance;

        public Options(IEnumerable<string> inputs, string filter, string output, float tolerance)
        {
            _inputs = inputs;
            _filter = filter;
            _output = output;
            _tolerance = tolerance;
        }

        [Option('i', "input", Required = true, HelpText = "Input folder containing RVM and TXT files.")]
        public IEnumerable<string> Inputs { get { return _inputs; } }
        
        [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
        public string Filter { get { return _filter; } }
        
        [Option('o', "output", Required = true, HelpText = "Output folder")]
        public string Output { get { return _output; } }

        [Option('t', "tolerance", Default = 0.1f, Required = false, HelpText = "Tessellation tolerance")]
        public float Tolerance { get { return _tolerance; } }
    }
}