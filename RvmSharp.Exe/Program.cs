using CommandLine;
using Equinor.MeshOptimizationPipeline;
using rvmsharp.Tessellator;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace RvmSharp.Exe
{
    using Containers;
    using Primitives;
    using System.Text.RegularExpressions;

    class Program
    {
        private class Options
        {
            private readonly string _inputFolder;
            private readonly string _filter;

            public Options(string inputFolder, string filter)
            {
                this._inputFolder = inputFolder;
                this._filter = filter;
            }

            [Option('i', "input", Required = true, HelpText = "Input folder containing RVM and TXT files.")]
            public string InputFolder { get { return _inputFolder; } }
            
            [Option('f', "filter", Required = false, HelpText = "Regex filter to match files in input folder")]
            public string Filter { get { return _filter; } }
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
                    var pdms = PdmsTextParser.GetAllPdmsNodesInFile(txtFilename);
                    AssignRecursive(pdms, rvmFile.Model.children);
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
            var meshes = leafs.AsParallel().SelectMany(leaf =>
            {
                progressBar.Message = leaf.Name;
                var meshes = leaf.Primitives.Select(p =>
                {
                    var mesh = TessellatorBridge.Tessellate(p, 1f);
                    mesh?.Apply(p.Matrix);
                    return mesh;
                }).Where(m => m!= null);
                progressBar.Tick();
                return meshes;
            }).ToArray();
            progressBar.Dispose();
            progressBar = new ProgressBar(meshes.Length, "Exporting");
            
            /*var iii = 0;
            using var objExporter = new OBJExporter( $"E:/test{iii}.obj");
            foreach (var mesh in meshes)
            {
                
                objExporter.WriteMesh(mesh);
                progressBar.Tick();
                if (iii++ > 5000)
                    break;
            }
            progressBar.Dispose();*/

            Console.WriteLine("Done!");
            return 0;


            /*var leafs = rvm.Model.children.SelectMany((c) => CollectGeometryNodes(c)).ToArray();

            var i = 0;
            foreach (var leaf in leafs)
            {
                var found = false;
                using OBJExporter o = new OBJExporter($"E:/testdata{i++}.obj");
                foreach (var p in leaf.Primitives)
                {
                    switch (p)
                    {
                        case RvmBox:
                        case RvmFacetGroup:
                            var mesh = TessellatorBridge.Tessellate(p, 1);
                            mesh.Apply(p.Matrix);
                            found = true;
                            o.WriteMesh(mesh);
                            break;
                    }
                }

            }

            */
        }

        

        private static void AssignRecursive(IList<PdmsTextParser.PdmsNode> attributes, IList<RvmGroup> groups)
        {
            //if (attributes.Count != groups.Count)
            //    Console.Error.WriteLine("Length of attribute nodes does not match group length");
            var copy = new List<RvmGroup>(groups);
            for (var i = 0; i < attributes.Count; i++)
            {
                var pdms = attributes[i];
                for (var k = 0; k < copy.Count; k++)
                {
                    var group = copy[k];
                    if (group.Name == pdms.Name)
                    {
                        // todo attr
                        foreach (var kvp in pdms.MetadataDict)
                            group.Attributes.Add(kvp.Key, kvp.Value);
                        AssignRecursive(pdms.Children, group.Children);
                        break;
                    }
                }
            }
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
