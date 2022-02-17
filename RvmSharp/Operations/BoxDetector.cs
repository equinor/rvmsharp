namespace RvmSharp.Operations;

using Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class BoxDetector
{
    public static bool IsBox(RvmFacetGroup facetGroup, out RvmBox box)
    {
        box = new RvmBox(1,
            Matrix4x4.Identity,
            new RvmBoundingBox(Vector3.Zero, Vector3.Zero),
            0, 0, 0);

        if (facetGroup.Polygons.Length != 6)
            return false;

        var sides = new (Vector3[] vertices, Vector3 normal)[6];
        // we need 6 sides

        // collect sides and check normal on all vertices
        for (var i = 0; i < facetGroup.Polygons.Length; i++)
        {
            var polygon = facetGroup.Polygons[i];
            if (polygon.Contours.Length != 1)
                return false;
            foreach (var contour in polygon.Contours)
            {
                var vs = contour.Vertices;
                if (vs.Length != 4)
                    return false;
                if (vs[0].Normal != vs[1].Normal ||
                    vs[1].Normal != vs[2].Normal ||
                    vs[2].Normal != vs[3].Normal)
                    return false;

                sides[i++] = (new[] { vs[0].Vertex, vs[1].Vertex, vs[2].Vertex, vs[3].Vertex }, vs[0].Normal);
            }
        }

        // Check that all angles in each side are 90 degrees
        foreach (var side in sides)
        {
            var a1 = AngleToRad(side.vertices[1] - side.vertices[0], side.vertices[3] - side.vertices[0]);
            var a2 = AngleToRad(side.vertices[1] - side.vertices[2], side.vertices[2] - side.vertices[3]);
            var a3 = AngleToRad(side.vertices[0] - side.vertices[1], side.vertices[2] - side.vertices[1]);
            if (!ApproximatelyEquals(a1, MathF.PI / 2) || !ApproximatelyEquals(a2, MathF.PI / 2) || !ApproximatelyEquals(a3, MathF.PI / 2))
                return false;
        }

        // Check that we have 8 unique vertices
        var uniqueVertices = new List<Vector3>(8);
        foreach (var side in sides)
        {
            foreach (var v in side.vertices)
            {
                if (uniqueVertices.Any(uv => uv.Equals(v)))
                    continue;
                if (uniqueVertices.Count == 8) // we have more than 8 unique vertices
                    return false;
                uniqueVertices.Add(v);
            }
        }
        if (uniqueVertices.Count != 8)
            return false;

        // Take 3 unique vertices, construct box verify that every side exists

        // Find vertex not on the first side
        Vector3 origin = default;
        Vector3 dir1 = default;
        Vector3 dir2 = default;
        Vector3 dir3 = default;
        foreach (var side1VertexX in sides[1].vertices)
        {
            var side0Vertices = sides[0].vertices;
            if (side0Vertices.Any(v => v.ApproximatelyEquals(side1VertexX)))
                continue;

            // find side 0 vertex that is connected to side 1 vertex X, by measuring angle
            if (AngleToRad(side1VertexX - side0Vertices[0], side0Vertices[1] - side0Vertices[0]).ApproximatelyEquals(MathF.PI / 2))
            {
                // connected to side 0 vertex 0
                origin = side0Vertices[0];
                dir1 = side0Vertices[1] - origin;
                dir2 = side0Vertices[3] - origin;
                dir3 = side1VertexX - origin;
            } else if (AngleToRad(side1VertexX - side0Vertices[1], side0Vertices[2] - side0Vertices[1]).ApproximatelyEquals(MathF.PI / 2))
            {
                // connected to side 0 vertex 1
                origin = side0Vertices[1];
                dir1 = side0Vertices[2] - origin;
                dir2 = side0Vertices[0] - origin;
                dir3 = side1VertexX - origin;
            } else if (AngleToRad(side1VertexX - side0Vertices[2], side0Vertices[3] - side0Vertices[2]).ApproximatelyEquals(MathF.PI / 2))
            {
                // connected to side 0 vertex 2
                origin = side0Vertices[2];
                dir1 = side0Vertices[3] - origin;
                dir2 = side0Vertices[1] - origin;
                dir3 = side1VertexX - origin;
            } else if (AngleToRad(side1VertexX - side0Vertices[3], side0Vertices[0] - side0Vertices[3]).ApproximatelyEquals(MathF.PI / 2))
            {
                // connected to side 0 vertex 3
                origin = side0Vertices[3];
                dir1 = side0Vertices[0] - origin;
                dir2 = side0Vertices[2] - origin;
                dir3 = side1VertexX - origin;
            }
            else
            {
                // something is not right, bail
                return false;
            }
            break;
        }

        // Creating new box, from initial vertex and 3 directional vectors with lengths
        var v1 = origin;
        var v2 = origin + dir1;
        var v3 = origin + dir1 + dir2;
        var v4 = origin + dir2;
        var v5 = origin + dir3;
        var v6 = origin + dir3 + dir1;
        var v7 = origin + dir3 + dir1 + dir2;
        var v8 = origin + dir3 + dir2;
        var n1 = Vector3.Normalize(dir1);
        var n2 = Vector3.Normalize(dir2);
        var n3 = Vector3.Normalize(dir3);
        var newSides = new (Vector3[] vertices, Vector3 normal)[]
        {
            (new []{v1, v2, v3, v4}, -n3),
            (new []{v5, v6, v7, v8}, n3),
            (new []{v1, v2, v6, v5}, -n2),
            (new []{v2, v3, v7, v6}, n1),
            (new []{v3, v4, v8, v7}, n2),
            (new []{v4, v1, v5, v8}, -n1),
        };

        // verify that all sides are present and found in original facet group
        foreach (var newSide in newSides)
        {
            var found = false;
            foreach (var oldSide in sides)
            {
                if (!newSide.normal.ApproximatelyEquals(oldSide.normal))
                    continue;

                foreach (var v in newSide.vertices)
                {
                    if (!oldSide.vertices.Any(vv => vv.ApproximatelyEquals(v)))
                        return false;
                }
                found = true;
                break;
            }
            if (!found)
                return false;
        }

        if (!Matrix4x4.Decompose(facetGroup.Matrix, out var scale, out var rotation, out var translation))
            return false; // no point in trying

        var boxCenter = origin + (dir1 + dir2 + dir3) / 2;
        // check that two directions are axis aligned, two is enough to determine that all 3 are aligned
        var dir1NormalizedAbs = Vector3.Abs(Vector3.Normalize(dir1));
        var dir2NormalizedAbs = Vector3.Abs(Vector3.Normalize(dir2));
        if (dir1NormalizedAbs.X.ApproximatelyEquals(1) && dir2NormalizedAbs.Y.ApproximatelyEquals(1) ||
            dir1NormalizedAbs.X.ApproximatelyEquals(1) && dir2NormalizedAbs.Z.ApproximatelyEquals(1) ||
            dir1NormalizedAbs.Y.ApproximatelyEquals(1) && dir2NormalizedAbs.X.ApproximatelyEquals(1) ||
            dir1NormalizedAbs.Y.ApproximatelyEquals(1) && dir2NormalizedAbs.Z.ApproximatelyEquals(1) ||
            dir1NormalizedAbs.Z.ApproximatelyEquals(1) && dir2NormalizedAbs.X.ApproximatelyEquals(1) ||
            dir1NormalizedAbs.Z.ApproximatelyEquals(1) && dir2NormalizedAbs.Y.ApproximatelyEquals(1))
        {
            // axis aligned box, lets choose optimal with identity transform
            var size = Vector3.Abs(dir1 + dir2 + dir3);

            box = box with
            {
                BoundingBoxLocal = new RvmBoundingBox(-size/2, size/2),
                Matrix = Matrix4x4.CreateScale(scale) *
                         Matrix4x4.CreateFromQuaternion(rotation) *
                         Matrix4x4.CreateTranslation(boxCenter * scale) * Matrix4x4.CreateTranslation(translation),
                LengthX = MathF.Abs(size.X),
                LengthY = MathF.Abs(size.Y),
                LengthZ = MathF.Abs(size.Z),
            };
        }
        else
        {
            var rot1 = Vector3.UnitX.FromToRotation(Vector3.Normalize(dir1));
            var yTransformed = Vector3.Normalize(Vector3.Transform(Vector3.UnitY, rot1));
            var angle = yTransformed.AngleToRad(Vector3.Normalize(dir3));
            var rotationNormal = Vector3.Cross(yTransformed, Vector3.Normalize(dir3));
            var rot2 = rotationNormal.LengthSquared().ApproximatelyEquals(0) ?
                Quaternion.Identity : Quaternion.CreateFromAxisAngle(Vector3.Normalize(rotationNormal), angle);

            var lengthX = dir1.Length();
            var lengthY = dir3.Length();
            var lengthZ = dir2.Length();
            var rotationCombined = Quaternion.Normalize(rot2 * rot1);

            box = box with
            {
                BoundingBoxLocal = new RvmBoundingBox(new Vector3(-lengthX/2, - lengthY /2, -lengthZ /2),
                    new Vector3(lengthX/ 2, lengthY /2, lengthZ/2)),
                Matrix = Matrix4x4.CreateScale(scale) *
                         Matrix4x4.CreateFromQuaternion(rotationCombined * rotation) *
                         // Box center already rotated, so no need to rotate
                         Matrix4x4.CreateTranslation(boxCenter * scale) * Matrix4x4.CreateTranslation(translation),
                LengthX = lengthX,
                LengthY = lengthY,
                LengthZ = lengthZ,
            };
        }
        return true;
    }

    private static bool ApproximatelyEquals(this Vector3 v1, Vector3 v2, float tolerance = 0.001f)
    {
        return ApproximatelyEquals(v1.X, v2.X, tolerance) &&
               ApproximatelyEquals(v1.Y, v2.Y, tolerance) &&
               ApproximatelyEquals(v1.Z, v2.Z, tolerance);
    }

    // This should be in helper class together with the rest of helper functions from CadRevealComposer
    private static bool ApproximatelyEquals(this float f1, float f2, float tolerance = 0.001f)
    {
        return MathF.Abs(f1 - f2) < tolerance;
    }

    private static float AngleToRad(this Vector3 from, Vector3 to)
    {
        return MathF.Acos(Vector3.Dot(from, to) / (from.Length() * to.Length()));
    }

    public static Quaternion FromToRotation(this Vector3 from, Vector3 to)
    {
        var cross = Vector3.Cross(from, to);
        if (cross.LengthSquared().ApproximatelyEquals(0f, 0.00000001f)) // true if vectors are parallel
        {
            var dot = Vector3.Dot(from, to);
            if (dot < 0) // Vectors point in opposite directions
            {
                // We need to find an orthogonal to (to), non-zero vector (v)
                // such as dot product of (v) and (to) is 0
                // or satisfies following equation: to.x * v.x + to.y * v.y + to.z + v.z = 0
                // below some variants depending on which components of (to) is 0
                var xZero = to.X.ApproximatelyEquals(0);
                var yZero = to.Y.ApproximatelyEquals(0);
                var zZero = to.Z.ApproximatelyEquals(0);
                Vector3 axes;
                if (xZero && yZero)
                    axes = new Vector3(to.Z, 0, 0);
                else if (xZero && zZero)
                    axes = new Vector3(to.Y, 0, 0);
                else if (yZero && zZero)
                    axes = new Vector3(0, to.X, 0);
                else if (xZero)
                    axes = new Vector3(0, -to.Z, -to.Y);
                else if (yZero)
                    axes = new Vector3(-to.Z, 0, -to.X);
                else
                    axes = new Vector3(-to.Y, -to.Z, 0);
                return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axes), MathF.PI * 2);
            }
            return Quaternion.Identity;
        }

        return Quaternion.CreateFromAxisAngle(Vector3.Normalize(cross), from.AngleToRad(to));
    }

}