namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

public static class GeneralRingTessellator
{
    public static IEnumerable<APrimitive> Tessellate(GeneralRing generalRing)
    {
        var matrix = generalRing.InstanceMatrix;

        if (!matrix.DecomposeAndNormalize(out var scale, out var rotation, out var position))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + matrix);
        }

        var transformedRadius = scale.X / 2f;
        var arcAngle = generalRing.ArcAngle;

        var segments = TessellationUtils.SagittaBasedSegmentCount(arcAngle, transformedRadius, 1, 0.05f);
        var error = TessellationUtils.SagittaBasedError(arcAngle, transformedRadius, 1, segments);

        var normal = -Vector3.UnitZ;
        var startVector = Vector3.UnitY;
        var angleIncrement = arcAngle / segments;

        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var radius = 0.5f;
        var center = Vector3.Zero;
        var thickness = generalRing.Thickness;

        if (thickness == 1) // Thickness is a value between 0 - 1, 1 means full cake slice
        {
            vertices.Add(center);
            for (uint i = 0; i < segments + 1; i++)
            {
                var q = Quaternion.CreateFromAxisAngle(normal, angleIncrement * i);
                var v = Vector3.Normalize(Vector3.Transform(startVector, q));
                vertices.Add(center + v * radius);
            }

            for (uint i = 1; i < segments + 1; i++)
            {
                indices.Add(0);
                indices.Add(i);
                indices.Add(i + 1);
            }
        }
        else
        {
            for (uint i = 0; i < segments + 1; i++)
            {
                var q = Quaternion.CreateFromAxisAngle(normal, angleIncrement * i);
                var v = Vector3.Normalize(Vector3.Transform(startVector, q));
                vertices.Add(center + v * radius);
                vertices.Add(center + v * radius * (1 - thickness));
            }

            for (uint i = 0; i < segments; i++)
            {
                indices.Add(i * 2);
                indices.Add(i * 2 + 1);
                indices.Add(i * 2 + 2);

                indices.Add(i * 2 + 1);
                indices.Add(i * 2 + 2);
                indices.Add(i * 2 + 3);
            }
        }

        var transformedVertices = vertices.Select(v => Vector3.Transform(v, matrix)).ToArray();

        var mesh = new Mesh(transformedVertices, indices.ToArray(), error);
        yield return new TriangleMesh(mesh, generalRing.TreeIndex, Color.Magenta, generalRing.AxisAlignedBoundingBox);
    }
}
