namespace CadRevealComposer.Operations
{
    using Primitives;
    using RvmSharp.Exporters;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class InstancedMeshFileExporter
    {
        public static IReadOnlyList<InstancedMesh> ExportInstancedMeshesToObjFile(DirectoryInfo outputDirectory, ulong meshFileId, IReadOnlyList<InstancedMesh> meshGeometries)
        {
            using var objExporter = new ObjExporter(Path.Combine(outputDirectory.FullName, $"mesh_{meshFileId}.obj"));
            objExporter.StartObject("root");
            var exportedInstancedMeshes = new List<InstancedMesh>();
            uint triangleOffset = 0;

            var counter = 0;
            foreach (var instancedMeshesGroupedByMesh in meshGeometries.GroupBy(x => x.TempTessellatedMesh))
            {
                counter++;
                var mesh = instancedMeshesGroupedByMesh.Key;

                if (mesh == null)
                    throw new ArgumentException(
                        $"Expected meshGeometries to not have \"null\" meshes, was null on {instancedMeshesGroupedByMesh}",
                        nameof(meshGeometries));
                uint triangleCount = (uint)mesh.Triangles.Count / 3;

                objExporter.WriteMesh(mesh);

                // Create new InstancedMesh for all the InstancedMesh that were exported here.
                // This makes it possible to set the TriangleOffset
                var adjustedInstancedMeshes = instancedMeshesGroupedByMesh
                    .Select(instancedMesh => instancedMesh with
                    {
                        FileId = meshFileId,
                        TriangleOffset = triangleOffset,
                        TriangleCount = triangleCount
                    })
                    .ToArray();

                exportedInstancedMeshes.AddRange(
                    adjustedInstancedMeshes);

                triangleOffset += triangleCount;
            }

            Console.WriteLine($"{counter} distinct instanced meshes exported to MeshFile{meshFileId}");

            return exportedInstancedMeshes;
        }
    }
}