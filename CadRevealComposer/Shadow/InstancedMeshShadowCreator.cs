namespace CadRevealComposer.Shadow;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System.Linq;
using System.Numerics;

public static class InstancedMeshShadowCreator
{
    public static InstancedMesh CreateShadow(this InstancedMesh instanceMesh)
    {
        var boundingBox = instanceMesh.AxisAlignedBoundingBox;
        // csharpier-ignore
        uint[] indices =
        {
            0, 1, 2,
            1, 2, 3,

            0, 1, 4,
            1, 4, 5,

            0, 2, 4,
            2, 4, 6,

            2, 3, 6,
            3, 6, 7,

            1, 3, 5,
            3, 5, 7,

            4, 5, 6,
            5, 6, 7
        };

        var templateMesh = instanceMesh.TemplateMesh;

        var minX = templateMesh.Vertices.Min(x => x.X);
        var minY = templateMesh.Vertices.Min(y => y.Y);
        var minZ = templateMesh.Vertices.Min(z => z.Z);
        var maxX = templateMesh.Vertices.Max(x => x.X);
        var maxY = templateMesh.Vertices.Max(y => y.Y);
        var maxZ = templateMesh.Vertices.Max(z => z.Z);

        var min = new Vector3(minX, minY, minZ);
        var max = new Vector3(maxX, maxY, maxZ);

        var v0 = new Vector3(min.X, min.Y, min.Z);
        var v1 = new Vector3(max.X, min.Y, min.Z);
        var v2 = new Vector3(min.X, max.Y, min.Z);
        var v3 = new Vector3(max.X, max.Y, min.Z);
        var v4 = new Vector3(min.X, min.Y, max.Z);
        var v5 = new Vector3(max.X, min.Y, max.Z);
        var v6 = new Vector3(min.X, max.Y, max.Z);
        var v7 = new Vector3(max.X, max.Y, max.Z);

        Vector3[] vertices = { v0, v1, v2, v3, v4, v5, v6, v7 };

        var error = instanceMesh.TemplateMesh.Error;

        var mesh = new Mesh(vertices, indices, error);

        return instanceMesh with
        {
            TemplateMesh = mesh
        };
    }
}
