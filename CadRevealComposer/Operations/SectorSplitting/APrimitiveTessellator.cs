namespace CadRevealComposer.Operations.SectorSplitting;

using Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Tessellation;
using Utils;

public static class APrimitiveTessellator
{
    public static APrimitive TryToTessellate(APrimitive primitive)
    {
        switch (primitive)
        {
            case Box box:
                return Tessellate(box);
            case EccentricCone cone:
                return Tessellate(cone);
            case TorusSegment torus:
                return Tessellate(torus);
            case Cone cone:
                return Tessellate(cone);
            default:
                return primitive with { Color = Color.WhiteSmoke };
        }
    }

    private static TriangleMesh Tessellate(Box box, float error = 0f)
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
        return new TriangleMesh(mesh, box.TreeIndex, Color.Aqua, box.AxisAlignedBoundingBox);
    }

    private static APrimitive Tessellate(EccentricCone cone, float error = 0)
    {
        int segments = 12;

        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var normal = cone.Normal;
        var centerA = cone.CenterA;
        var radiusA = cone.RadiusA;
        var centerB = cone.CenterB;
        var radiusB = cone.RadiusB;

        var angleIncrement = (2 * MathF.PI) / segments;

        var startVector = CreateOrthogonalUnitVector(normal);

        for (uint i = 0; i < segments; i++)
        {
            var q = Quaternion.CreateFromAxisAngle(normal, angleIncrement * i);

            var v = Vector3.Transform(startVector, q);

            var vNorm = Vector3.Normalize(v);

            vertices.Add(centerA + vNorm * radiusA);
            vertices.Add(centerB + vNorm * radiusB);

            if (i < segments - 1)
            {
                indices.Add(i * 2);
                indices.Add(i * 2 + 1);
                indices.Add(i * 2 + 2);

                indices.Add(i * 2 + 1);
                indices.Add(i * 2 + 2);
                indices.Add(i * 2 + 3);
            }
            else
            {
                indices.Add(i * 2);
                indices.Add(i * 2 + 1);
                indices.Add(0);

                indices.Add(i * 2 + 1);
                indices.Add(0);
                indices.Add(1);
            }
        }

        var mesh = new Mesh(vertices.ToArray(), indices.ToArray(), error);
        return new TriangleMesh(mesh, cone.TreeIndex, Color.Magenta, cone.AxisAlignedBoundingBox);
    }

    private static APrimitive Tessellate(TorusSegment torus, float error = 0)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var arcAngle = torus.ArcAngle;
        var offset = torus.Radius;
        var tubeRadius = torus.TubeRadius;
        var matrix = torus.InstanceMatrix;

        uint segments = 12;
        uint turnSegments = 4; // var turnSegments = (int)(torus.ArcAngle / (MathF.PI / 8));

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
        return new TriangleMesh(mesh, torus.TreeIndex, Color.Gold, torus.AxisAlignedBoundingBox);
    }

    private static APrimitive Tessellate(Cone cone, float error = 0)
    {
        if (Vector3.Distance(cone.CenterB, cone.CenterA) == 0)
        {
            return cone;
        }

        uint totalSegments = 12; // Number of segments if the cone is complete
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var centerA = cone.CenterA;
        var radiusA = cone.RadiusA;
        var centerB = cone.CenterB;
        var radiusB = cone.RadiusB;
        var arcAngel = cone.ArcAngle;

        var normal = Vector3.Normalize(centerB - centerA);

        int segments = (int)(totalSegments * (arcAngel / (2 * MathF.PI)));
        if (segments == 0)
            segments = 1;

        bool isComplete = segments == totalSegments;

        var angleIncrement = arcAngel / segments;

        var startVector = CreateOrthogonalUnitVector(normal);
        var localXAxis = cone.LocalXAxis;

        if (
            !startVector.EqualsWithinTolerance(localXAxis, 0.1f)
            && !((startVector * -1).EqualsWithinTolerance(localXAxis, 0.1f))
        )
        {
            var angle = MathF.Acos(Vector3.Dot(startVector, localXAxis) / (startVector.Length() * localXAxis.Length()));
            var test = Quaternion.CreateFromAxisAngle(normal, angle);

            startVector = Vector3.Transform(startVector, test);
        }

        if ((startVector * -1).EqualsWithinTolerance(localXAxis, 0.1f))
        {
            var halfRotation = Quaternion.CreateFromAxisAngle(normal, MathF.PI);
            startVector = Vector3.Transform(startVector, halfRotation);
        }

        var qTest = Quaternion.CreateFromAxisAngle(normal, 3 * MathF.PI / 2.0f);
        startVector = Vector3.Transform(startVector, qTest);

        if (!float.IsFinite(startVector.X) || !float.IsFinite(startVector.X) || !float.IsFinite(startVector.X))
        {
            Console.WriteLine("asmdalks");
        }

        for (uint i = 0; i < segments + 1; i++)
        {
            if (isComplete && i == segments)
                continue;

            var q = Quaternion.CreateFromAxisAngle(normal, angleIncrement * i);

            var v = Vector3.Transform(startVector, q);

            var vNorm = Vector3.Normalize(v);

            var vertexA = centerA + vNorm * radiusA;
            var vertexB = centerB + vNorm * radiusB;

            vertices.Add(centerA + vNorm * radiusA);
            vertices.Add(centerB + vNorm * radiusB);
        }

        for (uint i = 0; i < segments; i++)
        {
            if (i < segments - 1 || !isComplete)
            {
                indices.Add(i * 2);
                indices.Add(i * 2 + 1);
                indices.Add(i * 2 + 2);

                indices.Add(i * 2 + 1);
                indices.Add(i * 2 + 2);
                indices.Add(i * 2 + 3);
            }
            else
            {
                indices.Add(i * 2);
                indices.Add(i * 2 + 1);
                indices.Add(0);

                indices.Add(i * 2 + 1);
                indices.Add(0);
                indices.Add(1);
            }
        }

        var mesh = new Mesh(vertices.ToArray(), indices.ToArray(), error);
        return new TriangleMesh(mesh, cone.TreeIndex, Color.Red, cone.AxisAlignedBoundingBox);
    }

    private static Vector3 CreateOrthogonalUnitVector(Vector3 vector)
    {
        var v = Vector3.Normalize(vector);

        if (v.X != 0 && v.Y != 0)
            return Vector3.Normalize(new Vector3(-v.Y, v.X, 0));
        if (v.X != 0 && v.Z != 0)
            return Vector3.Normalize(new Vector3(-v.Z, 0, v.X));
        if (v.Y != 0 && v.Z != 0)
            return Vector3.Normalize(new Vector3(0, -v.Z, v.Y));
        if (v.Equals(Vector3.UnitX) || v.Equals(-Vector3.UnitX))
            return Vector3.UnitY;
        if (v.Equals(Vector3.UnitY) || v.Equals(-Vector3.UnitY))
            return Vector3.UnitZ;
        if (v.Equals(Vector3.UnitZ) || v.Equals(-Vector3.UnitZ))
            return Vector3.UnitX;

        throw new Exception($"Could not find orthogonal vector of {v.ToString()}");
    }
}
