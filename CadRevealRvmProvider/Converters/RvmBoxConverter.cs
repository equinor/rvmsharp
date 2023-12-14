namespace CadRevealRvmProvider.Converters;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using RvmSharp.Primitives;
using RvmSharp.Tessellation;
using System.Drawing;

public static class RvmBoxConverter
{
    public static IEnumerable<APrimitive> ConvertToRevealPrimitive(this RvmBox rvmBox, ulong treeIndex, Color color)
    {
        if (!rvmBox.Matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + rvmBox.Matrix);
        }

        var rvmMesh = TessellatorBridge.Tessellate(rvmBox, 0.01f);
        if (rvmMesh != null)
        {
            var mesh = new Mesh(rvmMesh.Vertices, rvmMesh.Triangles, 0.01f);
            yield return new TriangleMesh(mesh, treeIndex, color, mesh.CalculateAxisAlignedBoundingBox(rvmBox.Matrix));
        }
    }
}
