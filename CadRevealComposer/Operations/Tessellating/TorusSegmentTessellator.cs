namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

public static class TorusSegmentTessellator
{
    public static IEnumerable<APrimitive> Tessellate(TorusSegment torus, float error = 0)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var arcAngle = torus.ArcAngle;
        var offset = torus.Radius;
        var tubeRadius = torus.TubeRadius;
        var matrix = torus.InstanceMatrix;

        uint segments = 12;
        uint totalTurnSegments = 12;

        int turnSegments = (int)(totalTurnSegments * (arcAngle / (2 * MathF.PI)));
        if (turnSegments == 0)
            turnSegments = 1;

        var turnIncrement = arcAngle / turnSegments;

        var angleIncrement = (2 * MathF.PI) / segments;

        var startVectors = new List<Vector3>(); // start vectors at the circles at each turn segment
        var startCenters = new List<Vector3>(); // the center of the turn segment circles
        var startNormals = new List<Vector3>();

        for (int i = 0; i < turnSegments + 1; i++)
        {
            var turnAngle = i * turnIncrement;
            var normal = Vector3.UnitZ;

            var q = Quaternion.CreateFromAxisAngle(normal, turnAngle);
            var v = Vector3.Transform(Vector3.UnitX, q);

            startVectors.Add(v);
            startCenters.Add(Vector3.Zero + v * (offset));
            startNormals.Add(Vector3.Normalize(Vector3.Cross(normal, v)));
        }

        for (int j = 0; j < turnSegments + 1; j++)
        {
            var startVector = startVectors[j];
            var center = startCenters[j];
            var turnNormal = startNormals[j];

            for (int i = 0; i < segments; i++)
            {
                var q = Quaternion.CreateFromAxisAngle(turnNormal, angleIncrement * i);

                var v = Vector3.Transform(startVector, q);

                var vNorm = Vector3.Normalize(v);

                vertices.Add(center + vNorm * tubeRadius);
            }
        }

        for (uint j = 0; j < turnSegments; j++)
        {
            for (uint i = 0; i < segments; i++)
            {
                if (i < segments - 1)
                {
                    indices.Add(j * segments + i);
                    indices.Add(j * segments + i + 1);
                    indices.Add((j + 1) * segments + i);

                    indices.Add((j + 1) * segments + i);
                    indices.Add(j * segments + i + 1);
                    indices.Add((j + 1) * segments + i + 1);
                }
                else
                {
                    indices.Add(j * segments + i);
                    indices.Add(j * segments);
                    indices.Add((j + 1) * segments + i);

                    indices.Add(j * segments);
                    indices.Add((j + 1) * segments + i);
                    indices.Add((j + 1) * segments);
                }
            }
        }

        var transformedVertices = vertices.Select(x => Vector3.Transform(x, matrix)).ToArray();

        var mesh = new Mesh(transformedVertices, indices.ToArray(), error);
        yield return new TriangleMesh(mesh, torus.TreeIndex, Color.Gold, torus.AxisAlignedBoundingBox);
    }
}
