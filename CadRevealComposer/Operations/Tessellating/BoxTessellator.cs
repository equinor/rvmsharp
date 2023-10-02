namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Tessellation;

public static class BoxTessellator
{
    public static IEnumerable<APrimitive> Tessellate(Box box, float error = 0f)
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
            0,1,2,
            0,2,3,
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

        var mesh = new Mesh(transformedVertices, indices, error);
        yield return new TriangleMesh(mesh, box.TreeIndex, Color.Aqua, box.AxisAlignedBoundingBox);
    }
}
