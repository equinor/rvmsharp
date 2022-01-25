namespace RvmSharp.Operations;

using Containers;
using Exporters;
using Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Tessellation;

public static class RvmObjExporter
{
    public static void ExportToObj(RvmStore rvmStore, float tolerance, string outputFilename, Action<int, int, string>? tesselationProgressCallback = null, Action<int, int, string>? exportProgressCallback = null)
    {
        var leafs = rvmStore.RvmFiles.SelectMany(rvm => rvm.Model.Children.SelectMany(CollectGeometryNodes)).ToArray();
        var totalLeafs = leafs.Length;
        var tessellationProgress = 0;
        var meshes = leafs.AsParallel().Select(leaf =>
        {
            var progressMessage = $"Tessellating {leaf.Name}";
            tesselationProgressCallback?.Invoke(totalLeafs, tessellationProgress, progressMessage);
            var tessellatedMeshes = TessellatorBridge.Tessellate(leaf, tolerance);
            tesselationProgressCallback?.Invoke(totalLeafs, (++tessellationProgress), progressMessage);
            return (name: leaf.Name, primitives: tessellatedMeshes);
        }).ToArray();

        var totalMeshes = meshes.Length;
        var exportProgress = 0;
        exportProgressCallback?.Invoke(totalMeshes, exportProgress, "Exporting...");

        using var objExporter = new ObjExporter(outputFilename);
        Color? previousColor = null;
        foreach ((string objectName, (Mesh, Color)[] primitives) in meshes)
        {
            objExporter.StartObject(objectName);
            objExporter.StartGroup(objectName);

            foreach ((Mesh? mesh, Color color) in primitives)
            {
                if (previousColor != color)
                    objExporter.StartMaterial(color);
                objExporter.WriteMesh(mesh);
                previousColor = color;
            }

            exportProgressCallback?.Invoke(totalMeshes, ++exportProgress, "Exporting...");
        }
    }

    private static IEnumerable<RvmNode> CollectGeometryNodes(RvmNode root)
    {
        if (root.Children.OfType<RvmPrimitive>().Any())
            yield return root;
        foreach (var geometryNode in root.Children.OfType<RvmNode>().SelectMany(CollectGeometryNodes))
            yield return geometryNode;
    }
}