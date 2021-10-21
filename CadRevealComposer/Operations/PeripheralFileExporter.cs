namespace CadRevealComposer.Operations
{
    using IdProviders;
    using RvmSharp.Exporters;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Utils;

    public class PeripheralFileExporter
    {
        private readonly string _outputDirectory;
        private readonly string _meshToCtmExePath;
        private readonly SequentialIdGenerator _idGenerator;

        public PeripheralFileExporter(string outputDirectory, string meshToCtmExePath)
        {
            _outputDirectory = outputDirectory;
            _meshToCtmExePath = meshToCtmExePath;
            _idGenerator = new SequentialIdGenerator();
        }

        public async Task<(ulong fileId, Dictionary<RefLookup<Mesh>, (long triangleOffset, long triangleCount)>)>
            ExportMeshesToObjAndCtmFile(IReadOnlyCollection<Mesh?> meshGeometries)
        {
            if (!meshGeometries.Any())
                await Console.Error.WriteLineAsync(
                    "WARNING: Trying to export InstancedMeshes but the argument has no items. The InstancingThreshold is maybe too high?");

            var meshFileId = _idGenerator.GetNextId();
            var objFileName = Path.Combine(_outputDirectory, $"mesh_{meshFileId}.obj");
            var ctmFileName = Path.Combine(_outputDirectory, $"mesh_{meshFileId}.ctm");
            using var objExporter = new ObjExporter(objFileName);
            objExporter.StartObject("root");

            var triangleOffset = 0L;
            var result = new Dictionary<RefLookup<Mesh>, (long triangleOffset, long triangleCount)>();
            foreach (var mesh in meshGeometries)
            {
                objExporter.WriteMesh(mesh!);
                var triangleCount = mesh!.Triangles.Count / 3;
                result.Add(new RefLookup<Mesh>(mesh), (triangleOffset, triangleCount));
                triangleOffset += triangleCount;
            }

            objExporter.Dispose();
            await Convert(objFileName, ctmFileName);

            return (meshFileId, result);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="inputObjFilePath"></param>
        /// <param name="outputCtmFilePath"></param>
        /// <param name="verbose">Should the output be Written to console?</param>
        /// <exception cref="Exception">When exit code is non-zero</exception>
        private async Task Convert(string inputObjFilePath, string outputCtmFilePath, bool verbose = false)
        {
            var processStartInfo = new ProcessStartInfo(_meshToCtmExePath)
            {
                ArgumentList =
                {
                    inputObjFilePath,
                    outputCtmFilePath,
                    "--comment",
                    "RvmSharp",
                    "--method",
                    "MG1",
                    "--level",
                    "4",
                    "--no-texcoords",
                    "--no-colors",
                    "--upaxis",
                    "Y"
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var ctmConvProcess = Process.Start(processStartInfo)!;
            await ctmConvProcess.WaitForExitAsync();


            async Task PrintOutputToConsoleHelper(Process process)
            {
                Console.WriteLine(await process.StandardOutput.ReadToEndAsync());
                var error = await process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(error))
                {
                    await Console.Error.WriteLineAsync(error);
                }
            }

            if (verbose)
            {
                await PrintOutputToConsoleHelper(ctmConvProcess);
            }

            if (ctmConvProcess.ExitCode != 0)
            {
                if (!verbose) { await PrintOutputToConsoleHelper(ctmConvProcess); } // Already logged above if verbose.

                throw new Exception($"CTM conversion process failed for {inputObjFilePath} with exit code " + ctmConvProcess.ExitCode);
            }
        }
    }
}