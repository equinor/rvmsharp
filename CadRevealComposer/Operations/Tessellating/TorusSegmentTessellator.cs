namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Utils;

public static class TorusSegmentTessellator
{
    public static IEnumerable<APrimitive> Tessellate(TorusSegment torus)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        if (!torus.InstanceMatrix.DecomposeAndNormalize(out var scale, out _, out _))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + torus.InstanceMatrix);
        }

        var arcAngle = torus.ArcAngle;
        var offset = torus.Radius;
        var tubeRadius = torus.TubeRadius;
        var matrix = torus.InstanceMatrix;

        var toroidalSegments = (uint)
            TessellationUtils.SagittaBasedSegmentCount(arcAngle, offset + tubeRadius, scale.X, 0.05f);
        var error = TessellationUtils.SagittaBasedError(arcAngle, tubeRadius, scale.X, (int)toroidalSegments);

        var poloidalSegments = (uint)
            TessellationUtils.SagittaBasedSegmentCount(2 * MathF.PI, tubeRadius, scale.X, 0.05f);

        var turnIncrement = arcAngle / toroidalSegments;

        var angleIncrement = (2 * MathF.PI) / poloidalSegments;

        var startVectors = new List<Vector3>(); // start vectors at the circles at each turn segment
        var startCenters = new List<Vector3>(); // the center of the turn segment circles
        var startNormals = new List<Vector3>();

        for (int i = 0; i < toroidalSegments + 1; i++)
        {
            var turnAngle = i * turnIncrement;
            var normal = Vector3.UnitZ;

            var q = Quaternion.CreateFromAxisAngle(normal, turnAngle);
            var v = Vector3.Transform(Vector3.UnitX, q);

            startVectors.Add(v);
            startCenters.Add(Vector3.Zero + v * (offset));
            startNormals.Add(Vector3.Normalize(Vector3.Cross(normal, v)));
        }

        for (int j = 0; j < toroidalSegments + 1; j++)
        {
            var startVector = startVectors[j];
            var center = startCenters[j];
            var turnNormal = startNormals[j];

            for (int i = 0; i < poloidalSegments; i++)
            {
                var q = Quaternion.CreateFromAxisAngle(turnNormal, angleIncrement * i);

                var v = Vector3.Transform(startVector, q);

                var vNorm = Vector3.Normalize(v);

                vertices.Add(center + vNorm * tubeRadius);
            }
        }

        for (uint j = 0; j < toroidalSegments; j++)
        {
            for (uint i = 0; i < poloidalSegments; i++)
            {
                if (i < poloidalSegments - 1)
                {
                    indices.Add(j * poloidalSegments + i);
                    indices.Add(j * poloidalSegments + i + 1);
                    indices.Add((j + 1) * poloidalSegments + i);

                    indices.Add((j + 1) * poloidalSegments + i);
                    indices.Add(j * poloidalSegments + i + 1);
                    indices.Add((j + 1) * poloidalSegments + i + 1);
                }
                else
                {
                    indices.Add(j * poloidalSegments + i);
                    indices.Add(j * poloidalSegments);
                    indices.Add((j + 1) * poloidalSegments + i);

                    indices.Add(j * poloidalSegments);
                    indices.Add((j + 1) * poloidalSegments + i);
                    indices.Add((j + 1) * poloidalSegments);
                }
            }
        }

        var transformedVertices = vertices.Select(x => Vector3.Transform(x, matrix)).ToArray();

        var mesh = new Mesh(transformedVertices, indices.ToArray(), error);
        yield return new TriangleMesh(mesh, torus.TreeIndex, Color.Gold, torus.AxisAlignedBoundingBox);
    }
}
