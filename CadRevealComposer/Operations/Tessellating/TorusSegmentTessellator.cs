namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Utils;

public static class TorusSegmentTessellator
{
    public static TriangleMesh? Tessellate(TorusSegment torus)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        if (!torus.InstanceMatrix.DecomposeAndNormalize(out var scale, out _, out _))
        {
            throw new Exception("Failed to decompose matrix to transform. Input Matrix: " + torus.InstanceMatrix);
        }

        var arcAngle = torus.ArcAngle;
        var radius = torus.Radius;
        var tubeRadius = torus.TubeRadius;
        var matrix = torus.InstanceMatrix;

        float toroidalTolerance = SagittaUtils.CalculateSagittaTolerance((radius + tubeRadius) * 0.001f);
        var toroidalSegments = (uint)
            SagittaUtils.SagittaBasedSegmentCount(arcAngle, radius + tubeRadius, scale.X, toroidalTolerance);
        var error = SagittaUtils.SagittaBasedError(arcAngle, tubeRadius, scale.X, (int)toroidalSegments);

        float poloidalTolerance = SagittaUtils.CalculateSagittaTolerance(tubeRadius * scale.X);
        var poloidalSegments = (uint)
            SagittaUtils.SagittaBasedSegmentCount(2 * MathF.PI, tubeRadius, scale.X, poloidalTolerance);

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
            startCenters.Add(Vector3.Zero + v * (radius));
            startNormals.Add(Vector3.Normalize(Vector3.Cross(v, normal)));
        }

        for (int j = 0; j < toroidalSegments + 1; j++)
        {
            var startVector = startVectors[j];
            var center = startCenters[j];
            var turnNormal = startNormals[j];

            for (int i = 0; i < poloidalSegments; i++)
            {
                var q = Quaternion.CreateFromAxisAngle(turnNormal, angleIncrement * i);

                var v = Vector3.Normalize(Vector3.Transform(startVector, q));

                vertices.Add(center + v * tubeRadius);
            }
        }

        bool isComplete = arcAngle.ApproximatelyEquals(2 * MathF.PI);

        for (uint j = 0; j < toroidalSegments; j++)
        {
            if (j < toroidalSegments - 1 || !isComplete)
            {
                for (uint i = 0; i < poloidalSegments; i++)
                {
                    if (i < poloidalSegments - 1)
                    {
                        indices.Add(j * poloidalSegments + i);
                        indices.Add((j + 1) * poloidalSegments + i);
                        indices.Add(j * poloidalSegments + i + 1);

                        indices.Add(j * poloidalSegments + i + 1);
                        indices.Add((j + 1) * poloidalSegments + i);
                        indices.Add((j + 1) * poloidalSegments + i + 1);
                    }
                    else
                    {
                        indices.Add(j * poloidalSegments + i);
                        indices.Add((j + 1) * poloidalSegments + i);
                        indices.Add(j * poloidalSegments);

                        indices.Add(j * poloidalSegments);
                        indices.Add((j + 1) * poloidalSegments + i);
                        indices.Add((j + 1) * poloidalSegments);
                    }
                }
            }
            else
            {
                for (uint i = 0; i < poloidalSegments; i++)
                {
                    if (i < poloidalSegments - 1)
                    {
                        indices.Add(j * poloidalSegments + i);
                        indices.Add(i);
                        indices.Add(j * poloidalSegments + i + 1);

                        indices.Add(j * poloidalSegments + i + 1);
                        indices.Add(i);
                        indices.Add((i + 1));
                    }
                    else
                    {
                        indices.Add(j * poloidalSegments + i);
                        indices.Add(i);
                        indices.Add(j * poloidalSegments);

                        indices.Add(j * poloidalSegments);
                        indices.Add(i);
                        indices.Add(0);
                    }
                }
            }
        }

        var transformedVertices = vertices.Select(x => Vector3.Transform(x, matrix)).ToArray();

        var mesh = new Mesh(transformedVertices, indices.ToArray(), error);
        if (mesh.Vertices.Any(v => !v.IsFinite()))
        {
            Console.WriteLine(
                $"WARNING: Could not tessellate TorusSegment. ArcAngle: {torus.ArcAngle} Matrix: {torus.InstanceMatrix.ToString()} Radius: {torus.Radius} TubeRadius: {torus.TubeRadius}"
            );

            return null;
        }
        return new TriangleMesh(mesh, torus.TreeIndex, torus.Color, torus.AxisAlignedBoundingBox);
    }
}
