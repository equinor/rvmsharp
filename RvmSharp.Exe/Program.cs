using CommandLine;
using rvmsharp.Tessellator;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RvmSharp.Exe
{
    using Containers;
    using Primitives;
    using System.Text.RegularExpressions;
    using Tessellator;

    class Program
    {
        private class Options
        {
            private readonly string _inputFolder;
            private readonly string _filter;
            private readonly string _output;

            public Options(string inputFolder, string filter, string output)
            {
                _inputFolder = inputFolder;
                _filter = filter;
                _output = output;
            }

            [Option('i', "input", Required = true, HelpText = "Input folder containing RVM and TXT files.")]
            public string InputFolder { get { return _inputFolder; } }
            
            [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
            public string Filter { get { return _filter; } }
            
            [Option('o', "output", Required = true, HelpText = "Output folder")]
            public string Output { get { return _output; } }
        }

        private static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args).MapResult(o => RunOptionsAndReturnExitCode(o), e => HandleParseError(e));
            System.Environment.Exit(result);
        }

        private static int HandleParseError(IEnumerable<Error> e)
        {
            return -1;
        }

        private static int RunOptionsAndReturnExitCode(Options options)
        {
            var regexFilter = options.Filter != null ? new Regex(options.Filter) : null;
            var inputFiles = Directory.GetFiles(options.InputFolder, "*.rvm")
                .Concat(Directory.GetFiles(options.InputFolder, "*.txt"))
                .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f)))
                .GroupBy(Path.GetFileNameWithoutExtension).ToArray();

            var workload = (from filePair in inputFiles 
                select filePair.ToArray() into filePairStatic 
                let rvmFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".rvm")) 
                let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt")) 
                where rvmFilename != null select (rvmFilename, txtFilename)).ToArray();

            var progressBar = new ProgressBar(workload.Length, "Parsing input");
            
            var rvmFiles = workload.AsParallel().WithDegreeOfParallelism(64).Select(filePair =>
            {
                (string rvmFilename, string txtFilename) = filePair;
                progressBar.Message = Path.GetFileNameWithoutExtension(rvmFilename);
                var rvmFile = RvmParser.ReadRvm(File.OpenRead(rvmFilename));
                if (!string.IsNullOrEmpty(txtFilename))
                {
                    rvmFile.AttachAttributes(txtFilename);
                }

                progressBar.Tick();
                return rvmFile;
            }).ToArray();
            
            progressBar.Dispose();
            progressBar = new ProgressBar(2, "Aligining");
            var rvmStore = new RvmStore();
            rvmStore.RvmFiles.AddRange(rvmFiles);
            RvmConnect.Connect(rvmStore);
            progressBar.Tick();
            RvmAlign.Align(rvmStore);
            progressBar.Tick();
            progressBar.Dispose();

            var leafs = rvmStore.RvmFiles.SelectMany(rvm => rvm.Model.children.SelectMany(CollectGeometryNodes)).ToArray();
            progressBar = new ProgressBar(leafs.Length, "Tessellating");
            var meshes = leafs.AsParallel().Select(leaf =>
            {
                progressBar.Message = leaf.Name;
                var meshes = leaf.Primitives.Select(p =>
                {
                    var mesh = TessellatorBridge.Tessellate(p, 1f);
                    mesh?.Apply(p.Matrix);
                    return mesh;
                }).Where(m => m!= null);
                progressBar.Tick();
                return (leaf.Name, meshes);
            }).ToArray();
            progressBar.Dispose();
            progressBar = new ProgressBar(meshes.Length, "Exporting");
            
            using var objExporter = new ObjExporter( options.Output);
            foreach (var mesh in meshes)
            {
                objExporter.StartObject(mesh.Name);
                foreach (var m in mesh.meshes)
                    objExporter.WriteMesh(m);
                progressBar.Tick();
            }
            progressBar.Dispose();

            Console.WriteLine("Done!");
            return 0;
        }

        private static IEnumerable<RvmGroup> CollectGeometryNodes(RvmGroup root)
        {
            if (root.Primitives.Count > 0)
                yield return root;
            foreach (var p in root.Children.SelectMany(CollectGeometryNodes))
                yield return p;
        }
    }
}
