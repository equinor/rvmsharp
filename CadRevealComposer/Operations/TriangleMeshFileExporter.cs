namespace CadRevealComposer.Operations
{
    using Primitives;
    using RvmSharp.Exporters;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class TriangleMeshFileExporter
    {
        public static IReadOnlyList<TriangleMesh> ExportMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshFileId, IReadOnlyList<TriangleMesh> meshGeometries)
        {
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshFileId}.obj"));
            objExporter.StartObject("root"); // Keep a single object in each file

            var result = new List<TriangleMesh>(meshGeometries.Count);

            foreach (var triangleMesh in meshGeometries)
            {
                if (triangleMesh.TempTessellatedMesh == null)
                    throw new ArgumentNullException(nameof(triangleMesh.TempTessellatedMesh),
                        "Expected all TriangleMeshes to have a temp mesh when exporting");

                objExporter.WriteMesh(triangleMesh.TempTessellatedMesh);

                result.Add(triangleMesh with
                {
                    FileId = meshFileId
                });
            }

            return result;
        }
    }
}