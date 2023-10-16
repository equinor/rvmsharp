namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;

public static class CircleTessellator
{
    public static IEnumerable<APrimitive> Tessellate(Circle circle)
    {
        if (!circle.InstanceMatrix.DecomposeAndNormalize(out var scale, out _, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + circle.InstanceMatrix);
        }

        var normal = circle.Normal;
        var radius = scale.X / 2f;

        float tolerance = TessellationUtils.CalculateSagittaTolerance(radius);
        var segments = TessellationUtils.SagittaBasedSegmentCount(2 * MathF.PI, radius, 1, tolerance);
        var error = TessellationUtils.SagittaBasedError(2 * MathF.PI, radius, 1, segments);

        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var startVector = TessellationUtils.CreateOrthogonalUnitVector(normal);
        var angleIncrement = (2 * MathF.PI) / segments;
        var center = position;

        vertices.Add(center);

        for (uint i = 0; i < segments; i++)
        {
            var q = Quaternion.CreateFromAxisAngle(normal, angleIncrement * i);
            var v = Vector3.Normalize(Vector3.Transform(startVector, q));
            vertices.Add(center + v * radius);
        }

        for (uint i = 1; i < segments + 1; i++)
        {
            if (i < segments)
            {
                indices.Add(0);
                indices.Add(i);
                indices.Add(i + 1);
            }
            else
            {
                indices.Add(0);
                indices.Add(i);
                indices.Add(1);
            }
        }

        var mesh = new Mesh(vertices.ToArray(), indices.ToArray(), error);
        yield return new TriangleMesh(mesh, circle.TreeIndex, circle.Color, circle.AxisAlignedBoundingBox);
    }
}
