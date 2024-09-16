namespace CadRevealComposer.Operations.Tessellating;

using System;
using System.Linq;
using System.Numerics;
using Primitives;
using Tessellation;
using Utils;

public static class BoxTessellator
{
    public static TriangleMesh? Tessellate(Box box)
    {
        var vertices = new Vector3[]
        {
            new(-0.5f, -0.5f, -0.5f),
            new(0.5f, -0.5f, -0.5f),
            new(0.5f, 0.5f, -0.5f),
            new(-0.5f, 0.5f, -0.5f),
            new(-0.5f, -0.5f, 0.5f),
            new(0.5f, -0.5f, 0.5f),
            new(0.5f, 0.5f, 0.5f),
            new(-0.5f, 0.5f, 0.5f)
        };
        // csharpier-ignore
        var indices = new uint[]
        {
            0,2,1,
            0,3,2,
            0,1,5,
            0,5,4,
            1,2,6,
            1,6,5,
            2,3,7,
            2,7,6,
            3,0,4,
            3,4,7,
            4,5,6,
            4,6,7
        };

        var matrix = box.InstanceMatrix;

        var transformedVertices = vertices.Select(x => Vector3.Transform(x, matrix)).ToArray();

        var mesh = new Mesh(transformedVertices, indices, 0f);

        if (mesh.Vertices.Any(v => !v.IsFinite()))
        {
            Console.WriteLine($"WARNING: Could not tessellate Box. Matrix: {box.InstanceMatrix.ToString()}");
            return null;
        }

        return new TriangleMesh(mesh, box.TreeIndex, box.Color, box.AxisAlignedBoundingBox);
    }
}
