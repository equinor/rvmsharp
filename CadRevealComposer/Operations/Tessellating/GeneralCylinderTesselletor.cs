namespace CadRevealComposer.Operations.Tessellating;

using CadRevealComposer.Primitives;
using CadRevealComposer.Tessellation;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

internal class GeneralCylinderTessallator
{
    public static IEnumerable<APrimitive> Tessellate(GeneralCylinder cylinder, float error = 0)
    {
        int segments = 12;

        var vertices = new List<Vector3>();
        var indices = new List<uint>();

        var planeA = cylinder.PlaneA;
        var planeB = cylinder.PlaneB;

        var localPlaneANormal = Vector3.Normalize(new Vector3(planeA.X, planeA.Y, planeA.Z));
        var localPlaneBNormal = Vector3.Normalize(new Vector3(planeB.X, planeB.Y, planeB.Z));

        var localXAxis = Vector3.Normalize(cylinder.LocalXAxis);
        var qqq = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.1f);

        var testV1 = Vector3.Transform(Vector3.UnitX, qqq);
        var testV2 = Vector3.Transform(localXAxis, qqq);

        Quaternion rotation;
        if (Vector3.Dot(Vector3.UnitX, localXAxis) > 0.99999f)
        {
            rotation = Quaternion.Identity;
        }
        else if (Vector3.Dot(Vector3.UnitX, localXAxis) < -0.99999f)
        {
            rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI);
        }
        else
        {
            // var cross = Vector3.Normalize(Vector3.Cross(localXAxis, Vector3.UnitX));
            //
            // var angle = MathF.Acos(Vector3.Dot(localXAxis, Vector3.UnitX));
            //
            // var testQ = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(cross, angle));
            //
            // // var rotation = cylinder.Rotation;
            // rotation = testQ;

            // var cross = Vector3.Normalize(Vector3.Cross(Vector3.UnitX, localXAxis));
            //
            // Quaternion q;
            // q.X = cross.X;
            // q.Y = cross.Y;
            // q.Z = cross.Z;
            //
            // q.W =
            //     MathF.Sqrt((Vector3.UnitX.LengthSquared()) * (localXAxis.LengthSquared()))
            //     + Vector3.Dot(Vector3.UnitX, localXAxis);
            //
            // rotation = Quaternion.Inverse(q);

            // float k_cos_theta = Vector3.Dot(Vector3.UnitX, localXAxis);
            // float k = MathF.Sqrt(Vector3.UnitX.LengthSquared() * localXAxis.LengthSquared());
            //
            // if ((k_cos_theta / k).ApproximatelyEquals(-1f, 0.01f))
            // {
            //     rotation = Quaternion.Normalize(new Quaternion(0, 1, 1, 0));
            // }
            //
            // rotation = Quaternion.Normalize(
            //     Quaternion.CreateFromAxisAngle(Vector3.Cross(Vector3.UnitX, localXAxis), k_cos_theta + k)
            // );

            var angle = MathF.Acos(Vector3.Dot(localXAxis, Vector3.UnitX));
            var cross = Vector3.Normalize(Vector3.Cross(Vector3.UnitX, localXAxis));

            rotation = new Quaternion(
                MathF.Cos(angle),
                MathF.Sin(angle / 2f) * cross.X,
                MathF.Sin(angle / 2f) * cross.Y,
                MathF.Sin(angle / 2f) * cross.Z
            );
        }

        Console.WriteLine("-------------------------------------------------------");
        // Console.WriteLine($"Test: {testQ.ToString()}");
        Console.WriteLine($"Rotation: {cylinder.Rotation.ToString()}");
        Console.WriteLine($"Our rotation: {rotation.ToString()}");
        // Console.WriteLine($"Processed unit x: {Vector3.Transform(Vector3.UnitX, testQ)}");
        Console.WriteLine("-------------------------------------------------------");

        //var angleBetweenXs = AngleBetween(Vector3.UnitX, localXAxis);
        //var cross = Vector3.Normalize(Vector3.Cross(Vector3.UnitX, localXAxis));
        //q.X = cross.X;
        //q.Y = cross.Y;
        //q.Z = cross.Z;

        //q.W = MathF.Sqrt(1 * 1 * 1 * 1) + Vector3.Dot(Vector3.UnitX, localXAxis);

        //q = Quaternion.Normalize(q);

        //var q = Quaternion.CreateFromAxisAngle(cross, angleBetweenXs);

        var planeANormal = Vector3.Normalize(Vector3.Transform(localPlaneANormal, rotation));
        var planeBNormal = Vector3.Normalize(-Vector3.Transform(localPlaneBNormal, rotation));

        //var planeANormal = localPlaneANormal;
        //var planeBNormal = localPlaneBNormal;

        var extendedCenterA = cylinder.CenterA;
        var extendedCenterB = cylinder.CenterB;
        var radius = cylinder.Radius;
        var normal = Vector3.Normalize(extendedCenterB - extendedCenterA);

        var anglePlaneA = TessellationUtils.AngleBetween(normal, planeANormal);
        var anglePlaneB = TessellationUtils.AngleBetween(normal, planeBNormal);

        //anglePlaneA -= MathF.PI / 2;
        //anglePlaneB -= MathF.PI / 2;

        var extendedHeightA = MathF.Sin(anglePlaneA) * radius;
        var extendedHeightB = MathF.Sin(anglePlaneB) * radius;

        float hypoA = radius;
        float hypoB = radius;

        if (anglePlaneA != 0)
        {
            hypoA = extendedHeightA * (1f / MathF.Sin(anglePlaneA));
        }

        if (anglePlaneB != 0)
        {
            hypoB = extendedHeightB * (1f / MathF.Sin(anglePlaneB));
        }

        var centerA = extendedCenterA + extendedHeightA * normal;
        var centerB = extendedCenterB - extendedHeightB * normal;

        if (!float.IsFinite(centerA.X) || !float.IsFinite(centerB.X))
        {
            Console.WriteLine("jn");
        }

        var angleIncrement = (2 * MathF.PI) / segments;

        //yield return DebugDrawVector(actualPlaneNormalA, centerA);
        //yield return DebugDrawVector(actualPlaneNormalB, centerB);

        //yield return DebugDrawVector(planeANormal, centerA);
        //yield return DebugDrawVector(planeBNormal, centerB);

        //yield return DebugDrawPlane(planeA, centerA);
        //yield return DebugDrawPlane(planeB, centerB);

        var startVectorA = Vector3.Normalize(TessellationUtils.CreateOrthogonalUnitVector(planeANormal));
        var startVectorB = Vector3.Normalize(TessellationUtils.CreateOrthogonalUnitVector(planeBNormal));

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
        yield return new TriangleMesh(mesh, cylinder.TreeIndex, Color.LimeGreen, cylinder.AxisAlignedBoundingBox);
    }
}
