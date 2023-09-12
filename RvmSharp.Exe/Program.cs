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
    using Primitives;
    using System.Text;
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

            var progressBar = new ProgressBar(2, "Connecting geometry");
            RvmConnect.Connect(rvmStore);
            progressBar.Tick();
            progressBar.Message = "Aligning geometry";
            RvmAlign.Align(rvmStore);
            progressBar.Tick();
            progressBar.Dispose();

            var nodeIdOffset = 0;
            if (File.Exists(options.NodeIdFile))
            {
                using var reader = new StreamReader(File.OpenRead(options.NodeIdFile), Encoding.ASCII);
                nodeIdOffset = Convert.ToInt32(reader.ReadToEnd());
            }
            
            var leafs = rvmStore.RvmFiles.SelectMany(rvm => rvm.Model.Children.SelectMany(node =>
                CollectGeometryNodes(node))).ToArray();
            List<string> warnings = new();
            progressBar = new ProgressBar(leafs.Length, "Tessellating");
            var meshes = leafs.AsParallel().Select((leaf, i) =>
            {
                var nodeId = nodeIdOffset + 1 + i;
                progressBar.Message = leaf.Name;
                removeInvalidGeometries(leaf, warnings);
                var tessellatedMeshes = TessellatorBridge.Tessellate(leaf, options.Tolerance);
                foreach (Mesh tessellatedMesh in tessellatedMeshes)
                {
                    tessellatedMesh.ApplySingleColor(nodeId);
                }
                progressBar.Tick();
                return (leaf.Name, tesselatedMeshes: tessellatedMeshes);
            }).ToArray();
            progressBar.Dispose();
            foreach (string warning in warnings)
            {
                Console.WriteLine(warning);
            }
            progressBar = new ProgressBar(meshes.Length, "Exporting");

            using var objExporter = new ObjExporter(options.Output);
            foreach ((string objectName, IEnumerable<Mesh> primitives) in meshes)
            {
                objExporter.StartObject(objectName);
                foreach (var primitive in primitives)
                    objExporter.WriteMesh(primitive);
                progressBar.Tick();
            }

            progressBar.Dispose();

            using var writer = new StreamWriter(File.Create(options.NodeIdFile), Encoding.ASCII);
            {
                writer.WriteLine(nodeIdOffset + leafs.Length);
            }
            
            Console.WriteLine("Done!");
            return 0;
        }

        /// <summary>
        /// Removes any invalid geometries that are known to crash the tesselator with this error:
        /// Unhandled exception. System.AggregateException: One or more errors occurred. (Array dimensions exceeded supported range.)
        /// ---&gt; System.OutOfMemoryException: Array dimensions exceeded supported range.ELBOW 1 of BRANCH 1 of PIPE /P170-2-530
        /// at RvmSharp.Tessellation.TessellatorBridge.Tessellate(RvmPrimitive sphereBasedPrimitive, Single radius, Single arc, Single shift_z, Single scale_z, Single scale, Single tolerance) in /Users/EFWA/git/vorker-model-slicer/rvmsharp/RvmSharp/Tessellation/TessellatorBridge.cs:line 994
        /// </summary>
        /// <param name="rvmNode">Node to check for error (invalid geometry)</param>
        private static void removeInvalidGeometries(RvmNode rvmNode, List<string> warnings)
        {
            if (rvmNode.Children.Count > 0)
            {
                List<RvmGroup> toRemove = new();
                foreach (RvmGroup child in rvmNode.Children)
                {
                    if (child is RvmEllipticalDish dish)
                    {
                        if (dish.BaseRadius == 0)
                        {
                            warnings.Add($"WARNING: Removing invalid geometry in {rvmNode.Name}.");
                            toRemove.Add(child);
                        }
                    }
                }

                foreach (RvmGroup item in toRemove)
                {
                    rvmNode.Children.Remove(item);
                }
            }
        }

        private static (string rvmFilename, string? txtFilename)[] CollectWorkload(Options options)
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
                    .Concat(directories.SelectMany(directory => Directory.GetFiles(directory, "*.txt"))) // Collect TXTs
                    .Concat(files) // Append single files
                    .Where(f => regexFilter == null || regexFilter.IsMatch(Path.GetFileName(f))) // Filter by regex
                    .GroupBy(Path.GetFileNameWithoutExtension).ToArray(); // Group by filename (rvm, txt)

            var workload = (from filePair in inputFiles
                select filePair.ToArray()
                into filePairStatic
                let rvmFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".rvm"))
                let txtFilename = filePairStatic.FirstOrDefault(f => f.ToLower().EndsWith(".txt"))
                select (rvmFilename, txtFilename)).ToArray();

            var result = new List<(string, string?)>();
            foreach ((string? rvmFilename, string? txtFilename) in workload)
            {
                if (rvmFilename == null)
                    Console.WriteLine(
                        $"No corresponding RVM file found for attributes: '{txtFilename}', the file will be skipped.");
                else
                    result.Add((rvmFilename, txtFilename));
            }

            return result.ToArray();
        }

        private static RvmStore ReadRvmData(IReadOnlyCollection<(string rvmFilename, string? txtFilename)> workload)
        {
            using var progressBar = new ProgressBar(workload.Count, "Parsing input");

            var rvmFiles = workload.Select(filePair =>
            {
                (string rvmFilename, string? txtFilename) = filePair;
                progressBar.Message = Path.GetFileNameWithoutExtension(rvmFilename);
                using var stream = File.OpenRead(rvmFilename);
                var rvmFile = RvmParser.ReadRvm(stream);
                if (!string.IsNullOrEmpty(txtFilename))
                {
                    rvmFile.AttachAttributes(txtFilename);
                }

                progressBar.Tick();
                return rvmFile;
            }).ToArray();
            var rvmStore = new RvmStore();
            rvmStore.RvmFiles.AddRange(rvmFiles);
            return rvmStore;
        }

        private static IEnumerable<RvmNode> CollectGeometryNodes(RvmNode root, string parentName = "")
        {
            string rootName = string.IsNullOrEmpty(parentName) ? root.Name : parentName + (root.Name.StartsWith('/') ? "" : "/") + root.Name;
            if (root.Children.OfType<RvmPrimitive>().Any())
            {
                root.Name = rootName;
                yield return root;
            }
            foreach (var geometryNode in root.Children.OfType<RvmNode>().SelectMany(node => CollectGeometryNodes(node, rootName)))
                yield return geometryNode;
        }
    }
}