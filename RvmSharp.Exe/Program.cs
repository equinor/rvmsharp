namespace RvmSharp.Exe
{
    using CommandLine;
    using Tessellation;
    using ShellProgressBar;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Containers;
    using Exporters;
    using Operations;
    using System.Diagnostics;
    using System.Text.RegularExpressions;

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
                var meshes = TessellatorBridge.Tessellate(leaf, options.Tolerance);
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
        
        private static (string rvmFilename, string txtFilename)[] CollectWorkload(Options options)
        {
            var regexFilter = options.Filter != null ? new Regex(options.Filter) : null;
            var directories = options.Inputs.Where(Directory.Exists).ToArray();
            var files = options.Inputs.Where(File.Exists).ToArray();
            var missingInputs = options.Inputs.Where(i => !files.Contains(i) && !directories.Contains(i)).ToArray();
            
            if (missingInputs.Any())
            {
                throw new FileNotFoundException(
                    $"Missing file or folder: {Environment.NewLine}{string.Join(Environment.NewLine, missingInputs)}");
            }

            var inputFiles = 
                directories.SelectMany(directory => Directory.GetFiles(directory, "*.rvm")) // Collect RVMs
                .Concat(directories.SelectMany(directory=> Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
                .Concat(files) // Append single files
                .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
                .GroupBy(Path.GetFileNameWithoutExtension).ToArray(); // Group by filename (rvm, txt)

            var workload = (from filePair in inputFiles
                select filePair.ToArray()
                into filePairStatic
                let rvmFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".rvm"))
                let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
                select (rvmFilename, txtFilename)).ToArray();

            foreach (var workItem in workload)
            {
                if (workItem.rvmFilename == null)
                    Console.WriteLine(
                        $"No corresponding RVM file found for attributes: '{workItem.txtFilename}', the file will be skipped.");
            }
            
            return workload.Where(rvmTxt => rvmTxt.rvmFilename != null).ToArray();
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
