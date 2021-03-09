namespace RvmSharp.Exe
{
    using CommandLine;
    using rvmsharp.Tessellator;
    using ShellProgressBar;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Containers;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using Tessellator;

    static class Program
    {
        private static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args).MapResult(RunOptionsAndReturnExitCode, HandleParseError);
            Environment.Exit(result);
        }

        private static int HandleParseError(IEnumerable<Error> e)
        {
            return -1;
        }

        private static int RunOptionsAndReturnExitCode(Options options)
        {
            var workload = CollectWorkload(options);

            var rvmStore = ReadRvmData(workload);
            
            var progressBar = new ProgressBar(2, "Aligining");
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
            foreach ((string objectName, IEnumerable<Mesh> primitives) in meshes)
            {
                objExporter.StartObject(objectName);
                foreach (var primitive in primitives)
                    objExporter.WriteMesh(primitive);
                progressBar.Tick();
            }
            progressBar.Dispose();

            Console.WriteLine("Done!");
            return 0;
        }
        
        private static (string rvmFilename, string txtFilename)[] CollectWorkload(Options options1)
        {
            var regexFilter = options1.Filter != null ? new Regex(options1.Filter) : null;
            var inputFiles = Directory.GetFiles(options1.InputFolder, "*.rvm")
                .Concat(Directory.GetFiles(options1.InputFolder, "*.txt"))
                .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f)))
                .GroupBy(Path.GetFileNameWithoutExtension).ToArray();

            var workload = (from filePair in inputFiles
                select filePair.ToArray()
                into filePairStatic
                let rvmFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".rvm"))
                let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
                where rvmFilename != null
                select (rvmFilename, txtFilename)).ToArray();
            return workload;
        }

        private static RvmStore ReadRvmData((string rvmFilename, string txtFilename)[] workload)
        {
            using var progressBar = new ProgressBar(workload.Length, "Parsing input");

            var rvmFiles = workload.AsParallel().WithDegreeOfParallelism(64).Select(filePair =>
            {
                (string rvmFilename, string txtFilename) = filePair;
                Debug.Assert(true, nameof(progressBar) + " != null");
                progressBar.Message = Path.GetFileNameWithoutExtension(rvmFilename);
                var rvmFile = RvmParser.ReadRvm(File.OpenRead(rvmFilename));
                if (!string.IsNullOrEmpty(txtFilename))
                {
                    rvmFile.AttachAttributes(txtFilename);
                }

                progressBar.Tick();
                return rvmFile;
            });
            var rvmStore = new RvmStore();
            rvmStore.RvmFiles.AddRange(rvmFiles);
            
            progressBar.Dispose();
            return rvmStore;
        }

        private static IEnumerable<RvmGroup> CollectGeometryNodes(RvmGroup root)
        {
            if (root.Primitives.Count > 0)
                yield return root;
            foreach (var geometryNode in root.Children.SelectMany(CollectGeometryNodes))
                yield return geometryNode;
        }
    }
}
