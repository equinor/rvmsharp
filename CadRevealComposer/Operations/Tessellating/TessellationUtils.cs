﻿namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;

public static class TessellationUtils
{
    private const int MinSamples = 3;
    private const int MaxSamples = 100;

    public static float AngleBetween(Vector3 v1, Vector3 v2)
    {
        if (v1.EqualsWithinFactor(v2, 0.1f))
            return 0;

        if ((v1 * -1).EqualsWithinFactor(v2, 0.1f))
            return MathF.PI;

        var result = MathF.Acos(Vector3.Dot(v1, v2) / (v1.Length() * v2.Length()));
        return float.IsFinite(result) ? result : MathF.PI;
    }

    public static Vector3 CreateOrthogonalUnitVector(Vector3 vector)
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

    /// <summary>
    /// Used for debugging vectors by creating a literal arrow as a triangle mesh
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="startPoint"></param>
    /// <param name="color"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static TriangleMesh DebugDrawVector(Vector3 direction, Vector3 startPoint, Color color, float length = 1.0f)
    {
        var baseDiameter = length / 10f;
        var baseLength = length * (4.0f / 5.0f);
        var arrowLength = length / 5.0f;
        var arrowDiameter = length / 5f;

        var unitDirection = Vector3.Normalize(direction);

        var baseCenterA = startPoint;
        var baseCenterB = startPoint + unitDirection * baseLength;
        var arrowCenterA = baseCenterB;
        var arrowCenterB = arrowCenterA + unitDirection * arrowLength;

        uint segments = 6;

        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var angleIncrement = (2 * MathF.PI) / segments;

        var startVector = CreateOrthogonalUnitVector(unitDirection);

        for (uint i = 0; i < segments; i++)
        {
            var q = Quaternion.CreateFromAxisAngle(unitDirection, angleIncrement * i);
            var v = Vector3.Transform(startVector, q);
            var vNormalized = Vector3.Normalize(v);

            vertices.Add(baseCenterA + vNormalized * baseDiameter);
        }

        for (uint i = 0; i < segments; i++)
        {
            var q = Quaternion.CreateFromAxisAngle(unitDirection, angleIncrement * i);
            var v = Vector3.Transform(startVector, q);
            var vNormalized = Vector3.Normalize(v);

            vertices.Add(baseCenterB + vNormalized * baseDiameter);
        }

        for (uint i = 0; i < segments; i++)
        {
            var q = Quaternion.CreateFromAxisAngle(unitDirection, angleIncrement * i);
            var v = Vector3.Transform(startVector, q);
            var vNormalized = Vector3.Normalize(v);

            vertices.Add(arrowCenterA + vNormalized * arrowDiameter);
        }

        vertices.Add(arrowCenterB);

        for (uint j = 0; j < 2; j++)
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

        uint firstBaseVertex = (uint)vertices.Count - 1 - segments;
        uint arrowPoint = (uint)vertices.Count - 1;
        for (uint i = 0; i < segments; i++)
        {
            if (i < segments - 1)
            {
                indices.Add(firstBaseVertex + i);
                indices.Add(((firstBaseVertex + i + 1)));
                indices.Add(arrowPoint);
            }
            else
            {
                indices.Add(firstBaseVertex + i);
                indices.Add(firstBaseVertex);
                indices.Add(arrowPoint);
            }
        }

        var boundingBox = new BoundingBox(baseCenterB - Vector3.One, baseCenterB + Vector3.One);

        var mesh = new Mesh(vertices.ToArray(), indices.ToArray(), 0);
        return new TriangleMesh(mesh, 0, color, boundingBox);
    }

    /// <summary>
    /// Used for debugging planes by creating a literal square in the plane as a triangle mesh
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="startPoint"></param>
    /// <returns></returns>
    public static TriangleMesh DebugDrawPlane(Vector4 plane, Vector3 startPoint)
    {
        var planeNormal = new Vector3(plane.X, plane.Y, plane.Z);

        var startVector = CreateOrthogonalUnitVector(planeNormal);

        var vertices = new List<Vector3>();

        for (int i = 0; i < 4; i++)
        {
            var q = Quaternion.CreateFromAxisAngle(planeNormal, i * MathF.PI / 2.0f);

            vertices.Add(Vector3.Transform(startVector, q) + startPoint);
        }

        var indices = new uint[] { 0, 1, 2, 0, 2, 3 };

        var boundingBox = new BoundingBox(startPoint - Vector3.One, startPoint + Vector3.One);

        if (!float.IsFinite(boundingBox.Center.X))
            Console.WriteLine("mksdlf");

        var mesh = new Mesh(vertices.ToArray(), indices.ToArray(), 0);
        return new TriangleMesh(mesh, 0, Color.Aquamarine, boundingBox);
    }

    /// <summary>
    /// Calculates the "maximum deviation" in the mesh from the "ideal" primitive.
    /// If we round a cylinder to N segment faces, this method gives us the distance from the extents of a the center
    /// of a flat face to the extents of a perfect cylinder.
    /// See: https://en.wikipedia.org/wiki/Sagitta_(geometry)
    /// </summary>
    public static float SagittaBasedError(double arc, float radius, float scale, int segments)
    {
        var lengthOfSagitta = scale * radius * (1.0f - Math.Cos(arc / segments)); // Length of sagitta
        return (float)lengthOfSagitta;
    }

    /// <summary>
    /// Calculates the amount of segments we need to represent this primitive within a given tolerance.
    /// </summary>
    /// <example>
    /// Example: A small cylinder with a tolerance of 0.1 might be represented with 8 sides, but a large cylinder might need 32
    /// </example>
    public static int SagittaBasedSegmentCount(double arc, float radius, float scale, float tolerance)
    {
        var maximumSagitta = tolerance;
        var samples = arc / Math.Acos(Math.Max(-1.0f, 1.0f - maximumSagitta / (scale * radius)));
        if (double.IsNaN(samples))
        {
            throw new Exception(
                $"Number of samples is calculated as NaN. Diagnostics: ({nameof(scale)}: {scale}, {nameof(arc)}: {arc}, {nameof(radius)}: {radius}, {nameof(tolerance)}: {tolerance} )"
            );
        }

        return Math.Min(MaxSamples, (int)(Math.Max(MinSamples, Math.Ceiling(samples))));
    }

    public static float CalculateSagittaTolerance(float radius)
    {
        if (radius == 0) // Some geometries doesn't have radius, just set an arbitrary default value
            return 1;

        var value = radius * 0.04f + 0.02f; // Arbitrary calculation of tolerance
        return value;
    }
}
