namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

internal class ConeTessellator
{
    public static IEnumerable<APrimitive> Tessellate(Cone cone, float error = 0)
    {
        if (Vector3.Distance(cone.CenterB, cone.CenterA) == 0)
        {
            yield return cone;
            yield break;
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

        var startVector = TessellationUtils.CreateOrthogonalUnitVector(normal);
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

            if (!float.IsFinite(vertexA.X) || !float.IsFinite(vertexB.X))
            {
                Console.WriteLine("nkajsnd");
            }

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
        yield return new TriangleMesh(mesh, cone.TreeIndex, Color.Red, cone.AxisAlignedBoundingBox);
    }
}
