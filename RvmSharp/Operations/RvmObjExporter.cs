namespace RvmSharp.Operations;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Containers;
using Exporters;
using Primitives;
using Tessellation;

public static class RvmObjExporter
{
    public static void ExportToObj(
        RvmStore rvmStore,
        float tolerance,
        string outputFilename,
        (Action<int> init, Action tick)? tessellationProgressCallback = null,
        (Action<int> init, Action tick)? exportProgressCallback = null
    )
    {
        var leafs = rvmStore.RvmFiles.SelectMany(rvm => rvm.Model.Children.SelectMany(CollectGeometryNodes)).ToArray();
        var totalLeafs = leafs.Length;
        tessellationProgressCallback?.init(totalLeafs);
        var meshes = leafs
            .AsParallel()
            .Select(leaf =>
            {
                var tessellatedMeshes = TessellatorBridge.Tessellate(leaf, tolerance);
                tessellationProgressCallback?.tick();
                return (name: leaf.Name, primitives: tessellatedMeshes);
            })
            .ToArray();

        var totalMeshes = meshes.Length;
        exportProgressCallback?.init(totalMeshes);

        using var objExporter = new ObjExporter(outputFilename);
        Color? previousColor = null;
        foreach ((string objectName, (RvmMesh, Color)[] primitives) in meshes)
        {
            objExporter.StartObject(objectName);
            objExporter.StartGroup(objectName);

            foreach ((RvmMesh? mesh, Color color) in primitives)
            {
                if (previousColor != color)
                    objExporter.StartMaterial(color);
                objExporter.WriteMesh(mesh);
                previousColor = color;
            }
            exportProgressCallback?.tick();
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
