namespace CadRevealComposer.Operations.Tessellating;

using Primitives;
using Tessellation;
using Utils;
using Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class CircleTessellator
{
    public static TriangleMesh? Tessellate(Circle circle)
    {
        if (!circle.InstanceMatrix.DecomposeAndNormalize(out var scale, out _, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + circle.InstanceMatrix);
        }

        var normal = circle.Normal;
        var radius = scale.X / 2f;

        float tolerance = SagittaUtils.CalculateSagittaTolerance(radius);
        var segments = SagittaUtils.SagittaBasedSegmentCount(2 * MathF.PI, radius, 1, tolerance);
        var error = SagittaUtils.SagittaBasedError(2 * MathF.PI, radius, 1, segments);

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

        if (mesh.Vertices.Any(v => !v.IsFinite()))
        {
            Console.WriteLine(
                $"WARNING: Could not tessellate Circle. Matrix: {circle.InstanceMatrix.ToString()} Normal: {circle.Normal}"
            );
            return null;
        }
        return new TriangleMesh(mesh, circle.TreeIndex, circle.Color, circle.AxisAlignedBoundingBox);
    }
}
