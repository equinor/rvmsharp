namespace CadRevealComposer.Utils;

using Commons.Utils;
using Primitives;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Tessellation;

public class BestFitBoundingBox
{
    public static Box CalculateBestFittingBox(Mesh mesh)
    {
        ConvexHull3D convexHull3D = new ConvexHull3D();
        var vertices = mesh.Vertices.ToList();

        List<ConvexHull3D.Face> faces = new List<ConvexHull3D.Face>();
        try
        {
            // To reduce the amount of vertices to process we assume the bounding box is always larger than the convex hull. Calculating the hull once makes the rest of the algorithm faster.
            faces = convexHull3D.GenerateConvexHull(vertices);
        }
        catch (Exception e)
        {
            // Console.WriteLine(e);
            return new Box(Matrix4x4.Identity, 1, Color.Aqua, new BoundingBox(Vector3.Zero, Vector3.Zero));
        }
        // generate mesh from faces

        var indexes = faces.Select(x => new List<int>() { x.Vertex1, x.Vertex2, x.Vertex3 }).SelectMany(x => x);

        var m2 = new Mesh(vertices.ToArray(), indexes.Select(x => (uint)x).ToArray(), 0);

        const float boundsMinWidthThreshold = 0.0001f;

        var bounds = m2.CalculateAxisAlignedBoundingBox();
        var minimumVolume = bounds.Extents.X * bounds.Extents.Y * bounds.Extents.Z;

        var centerOffset = bounds.Center;
        float x = 0.0f;
        float y = 0.0f;
        float z = 0.0f;
        //
        // if (minimumVolume < boundsMinWidthThreshold)
        // {
        //     mesh.Apply(Matrix4x4.CreateTranslation(-centerOffset));
        //     return (mesh.Id, MOPQuaternion.identity, centerOffset, MOPQuaternion.identity, mesh.Type);
        // }

        // Find the best fit bounding box by rotating the mesh in small steps
        // and checking the volume of the resulting bounding box
        for (float step = 15.0f; step > 1.0f; step /= 3.0f)
        {
            float halfRange = step * 3.0f;
            var sx = x;
            var sy = y;
            var sz = z;
            for (float tx = sx - halfRange; tx <= sx + halfRange; tx += step)
            {
                for (float ty = sy - halfRange; ty <= sy + halfRange; ty += step)
                {
                    for (float tz = sz - halfRange; tz <= sz + halfRange; tz += step)
                    {
                        var r = Quaternion.CreateFromYawPitchRoll(tx, ty, tz);
                        bounds = mesh.CalculateAxisAlignedBoundingBox(Matrix4x4.CreateFromQuaternion(r));
                        var newVolume = bounds.Extents.X * bounds.Extents.Y * bounds.Extents.Z;
                        if (newVolume >= minimumVolume)
                            continue;
                        minimumVolume = newVolume;
                        centerOffset = bounds.Center;
                        x = tx;
                        y = ty;
                        z = tz;
                    }
                }
            }
        }

        Quaternion rotation = Quaternion.CreateFromYawPitchRoll(x, y, z);

        mesh.Apply(Matrix4x4.CreateFromQuaternion(rotation));
        mesh.Apply(Matrix4x4.CreateTranslation(-centerOffset));

        var inverseRotation = Quaternion.Inverse(rotation);
        Vector3 offset = Vector3.Transform(centerOffset, inverseRotation);

        // if (
        //     configuration.CheckForCylinderApproximation
        //     && MOPCylinderApproximationUtils.IsCylindrical(
        //         mesh,
        //         out Plane plane,
        //         configuration.CylinderApproximationThreshold
        //     )
        // )
        // {
        //     rotation = MOPCylinderApproximationUtils.RotateCylinderApproximatedMesh(
        //         rotation,
        //         plane,
        //         mesh,
        //         out MOPQuaternion clRotation
        //     );
        //     cylinderRotation = clRotation;
        //     mesh.Type = MOPMesh.Types.Type.ACylinder;
        // }
        // else
        rotation = Quaternion.Inverse(rotation);

        return new Box(
            Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(offset),
            1,
            Color.Aqua,
            bounds
        );
        // return );
    }
}
