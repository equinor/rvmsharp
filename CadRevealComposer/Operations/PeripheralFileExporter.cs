namespace CadRevealComposer.Operations
{
    using IdProviders;
    using RvmSharp.Exporters;
    using RvmSharp.Tessellation;
    using System;
    using System.Collections.Generic;
    using System.IO;
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

        public async Task<(ulong fileId, Dictionary<RefLookup<Mesh>, (long triangleOffset, long triangleCount)>)> ExportInstancedMeshesToObjFile(IEnumerable<Mesh?> meshGeometries)
        {
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

        private async Task Convert(string inputObjFilePath, string outputCtmFilePath)
        {
            var process = System.Diagnostics.Process.Start(_meshToCtmExePath, new[]
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
            });
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                throw new Exception($"CTM conversion process failed for {inputObjFilePath}");
            }
        }
    }
}