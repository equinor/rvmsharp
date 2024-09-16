namespace CadRevealComposer.Operations.Tessellating;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Commons.Utils;
using Primitives;
using Tessellation;
using Utils;

public static class EccentricConeTessellator
{
    public static TriangleMesh? Tessellate(EccentricCone cone)
    {
        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var normal = cone.Normal;
        var centerA = cone.CenterA;
        var radiusA = cone.RadiusA;
        var centerB = cone.CenterB;
        var radiusB = cone.RadiusB;

        var tolerance = SagittaUtils.CalculateSagittaTolerance(float.Max(radiusA, radiusB));
        var segments = SagittaUtils.SagittaBasedSegmentCount(2 * MathF.PI, float.Max(radiusA, radiusB), 1f, tolerance);
        var error = SagittaUtils.SagittaBasedError(2 * MathF.PI, float.Max(radiusA, radiusB), 1f, segments);

        var angleIncrement = (2 * MathF.PI) / segments;

        var startVector = TessellationUtils.CreateOrthogonalUnitVector(normal);

        for (uint i = 0; i < segments; i++)
        {
            var q = Quaternion.CreateFromAxisAngle(-normal, angleIncrement * i);

            var v = Vector3.Normalize(Vector3.Transform(startVector, q));

            vertices.Add(centerA + v * radiusA);
            vertices.Add(centerB + v * radiusB);

            if (i < segments - 1)
            {
                indices.Add(i * 2);
                indices.Add(i * 2 + 2);
                indices.Add(i * 2 + 1);

                indices.Add(i * 2 + 1);
                indices.Add(i * 2 + 2);
                indices.Add(i * 2 + 3);
            }
            else
            {
                indices.Add(i * 2);
                indices.Add(0);
                indices.Add(i * 2 + 1);

                indices.Add(i * 2 + 1);
                indices.Add(0);
                indices.Add(1);
            }
        }

        var mesh = new Mesh(vertices.ToArray(), indices.ToArray(), error);

        if (mesh.Vertices.Any(v => !v.IsFinite()))
        {
            Console.WriteLine(
                $"WARNING: Could not tessellate Cone. CenterA: {cone.CenterA} CenterB: {cone.CenterB} Normal: {cone.Normal} RadiusA: {cone.RadiusA} RadiusB: {cone.RadiusB}"
            );
            return null;
        }
        return new TriangleMesh(mesh, cone.TreeIndex, cone.Color, cone.AxisAlignedBoundingBox);
    }
}
