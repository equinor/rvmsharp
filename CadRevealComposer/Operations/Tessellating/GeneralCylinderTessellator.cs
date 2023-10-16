namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System;
using System.Collections.Generic;
using System.Numerics;
using Utils;

public static class GeneralCylinderTessellator
{
    public static IEnumerable<APrimitive> Tessellate(GeneralCylinder cylinder)
    {
        var arcAngle = cylinder.ArcAngle;
        var radius = cylinder.Radius;

        var segments = TessellationUtils.SagittaBasedSegmentCount(arcAngle, radius, 1f, 0.05f);
        var error = TessellationUtils.SagittaBasedError(arcAngle, radius, 1f, segments);

        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var planeA = cylinder.PlaneA;
        var planeB = cylinder.PlaneB;
        var localXAxis = cylinder.LocalXAxis;

        var localPlaneANormal = Vector3.Normalize(new Vector3(planeA.X, planeA.Y, planeA.Z));
        var localPlaneBNormal = Vector3.Normalize(new Vector3(planeB.X, planeB.Y, planeB.Z));

        // TODO This one is wrong O:)
        var rotation = Quaternion.Identity;

        var planeANormal = Vector3.Normalize(Vector3.Transform(localPlaneANormal, rotation));
        var planeBNormal = Vector3.Normalize(-Vector3.Transform(localPlaneBNormal, rotation));

        var extendedCenterA = cylinder.CenterA;
        var extendedCenterB = cylinder.CenterB;
        var normal = Vector3.Normalize(extendedCenterB - extendedCenterA);

        var anglePlaneA = TessellationUtils.AngleBetween(normal, planeANormal);
        var anglePlaneB = TessellationUtils.AngleBetween(normal, planeBNormal);

        var extendedHeightA = MathF.Sin(anglePlaneA) * radius;
        var extendedHeightB = MathF.Sin(anglePlaneB) * radius;

        var centerA = extendedCenterA + extendedHeightA * normal;
        var centerB = extendedCenterB - extendedHeightB * normal;

        var angleIncrement = (2 * MathF.PI) / segments;

        var startVectorA = Vector3.Normalize(Vector3.Cross(Vector3.Cross(normal, planeANormal), planeANormal));
        var startVectorB = Vector3.Normalize(Vector3.Cross(Vector3.Cross(-normal, planeBNormal), planeBNormal));

        if (startVectorA.IsFinite() && !startVectorB.IsFinite())
        {
            startVectorB = -Vector3.Normalize(Vector3.Cross(Vector3.Cross(startVectorA, normal), normal));
        }
        else if (!startVectorA.IsFinite() && startVectorB.IsFinite())
        {
            startVectorA = -Vector3.Normalize(Vector3.Cross(Vector3.Cross(startVectorB, normal), normal));
        }

        if (!startVectorA.IsFinite())
        {
            startVectorA = TessellationUtils.CreateOrthogonalUnitVector(normal);
        }

        if (!startVectorB.IsFinite())
        {
            startVectorB = TessellationUtils.CreateOrthogonalUnitVector(normal);
        }

        float hypoA = radius;
        float hypoB = radius;

        if (anglePlaneA != 0)
        {
            hypoA = MathF.Abs(radius / MathF.Cos(anglePlaneA));
        }

        if (anglePlaneB != 0)
        {
            hypoB = MathF.Abs(radius / MathF.Cos(anglePlaneB));
        }

        for (uint i = 0; i < segments; i++)
        {
            var qA = Quaternion.CreateFromAxisAngle(planeANormal, angleIncrement * i);
            var qB = Quaternion.CreateFromAxisAngle(planeBNormal, angleIncrement * i);

            var vA = Vector3.Transform(startVectorA, qA);
            var vB = Vector3.Transform(startVectorB, qB);

            var vANormalized = Vector3.Normalize(vA);
            var vBNormalized = Vector3.Normalize(vB);

            // TODO
            var distanceFromCenterA = radius + MathF.Abs((hypoA - radius) * MathF.Cos(i * angleIncrement));
            var distanceFromCenterB = radius + MathF.Abs((hypoB - radius) * MathF.Cos(i * angleIncrement));

            vertices.Add(centerA + vANormalized * distanceFromCenterA);
            vertices.Add(centerB + vBNormalized * distanceFromCenterB);

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
        yield return new TriangleMesh(mesh, cylinder.TreeIndex, cylinder.Color, cylinder.AxisAlignedBoundingBox);
    }
}
