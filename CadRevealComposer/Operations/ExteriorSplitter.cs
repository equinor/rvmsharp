namespace CadRevealComposer.Operations;

using AlgebraExtensions;
using Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Tessellation;
using Utils;

/// <summary>
/// Split geometries into exterior and interior. The main use is too aid sector splitting, prioritizing the exterior higher up in the sector tree.
/// - Primitives are grouped by node ID, and whole nodes are split into exterior and interior.
/// - Ray casting is used to measure distance from outside vantage points to the nodes. The closest nodes are kept as exterior.
/// - TriangleMesh and InstanceMesh are tessellated to triangles and ray casted.
/// - Other primitives are ray casted using their bounding box. The justification is that primitives have a tight bounding box.
/// </summary>
public static class ExteriorSplitter
{
    private record Primitive(APrimitive OriginalPrimitive);

    private sealed record TessellatedPrimitive(Triangle[] Triangles, APrimitive OriginalPrimitive)
        : Primitive(OriginalPrimitive);

    private readonly record struct Node(BoundingBox? BoundingBox, Primitive[] Primitives);

    private readonly record struct RayEx(BoundingBox Box, Ray Ray);

    private static IEnumerable<RayEx> CreateRays(Vector3 boundingBoxMin, Vector3 boundingBoxMax)
    {
        const float cellSize = 1f;
        const float halfCell = cellSize / 2f;

        // calculate grid size
        var gridMin = new Vector3(
            MathF.Floor(boundingBoxMin.X),
            MathF.Floor(boundingBoxMin.Y),
            MathF.Floor(boundingBoxMin.Z)
        );

        var gridMax = new Vector3(
            MathF.Ceiling(boundingBoxMax.X),
            MathF.Ceiling(boundingBoxMax.Y),
            MathF.Ceiling(boundingBoxMax.Z)
        );

        var rayOriginMin = gridMin - new Vector3(100f);
        var rayOriginMax = gridMax + new Vector3(100f);

        // rays for XY plane
        for (float x = gridMin.X; x < gridMax.X; x += cellSize)
            for (float y = gridMin.Y; y < gridMax.Y; y += cellSize)
            {
                var bounds = new BoundingBox(
                    new Vector3(x, y, rayOriginMin.Z),
                    new Vector3(x + cellSize, y + cellSize, rayOriginMax.Z)
                );

                // negative
                yield return new RayEx(
                    bounds,
                    new Ray(new Vector3(x + halfCell, y + halfCell, rayOriginMin.Z), Vector3.UnitZ)
                );

                // positive
                yield return new RayEx(
                    bounds,
                    new Ray(new Vector3(x + halfCell, y + halfCell, rayOriginMax.Z), -Vector3.UnitZ)
                );
            }

        // rays for XZ plane
        for (float x = gridMin.X; x < gridMax.X; x += cellSize)
            for (float z = gridMin.Z; z < gridMax.Z; z += cellSize)
            {
                var bounds = new BoundingBox(
                    new Vector3(x, rayOriginMin.Y, z),
                    new Vector3(x + cellSize, rayOriginMax.Y, z + cellSize)
                );

                // negative
                yield return new RayEx(
                    bounds,
                    new Ray(new Vector3(x + halfCell, rayOriginMin.Y, z + halfCell), Vector3.UnitY)
                );

                // positive
                yield return new RayEx(
                    bounds,
                    new Ray(new Vector3(x + halfCell, rayOriginMax.Y, z + halfCell), -Vector3.UnitY)
                );
            }

        // rays for YZ plane
        for (float y = gridMin.Y; y < gridMax.Y; y += cellSize)
            for (float z = gridMin.Z; z < gridMax.Z; z += cellSize)
            {
                var bounds = new BoundingBox(
                    new Vector3(rayOriginMin.X, y, z),
                    new Vector3(rayOriginMax.X, y + cellSize, z + cellSize)
                );

                // negative
                yield return new RayEx(
                    bounds,
                    new Ray(new Vector3(rayOriginMin.X, y + halfCell, z + halfCell), Vector3.UnitX)
                );

                // positive
                yield return new RayEx(
                    bounds,
                    new Ray(new Vector3(rayOriginMax.X, y + halfCell, z + halfCell), -Vector3.UnitX)
                );
            }
    }

    public static (APrimitive[] Exterior, APrimitive[] Interior) Split(APrimitive[] primitives)
    {
        var nodes = CreateNodes(primitives);

        // create k-d tree for efficient processing of nodes
        var bb = primitives.CalculateBoundingBox();
        if (bb == null)
            return (Array.Empty<APrimitive>(), Array.Empty<APrimitive>());

        var tree = new IntervalKdTree<Node>(bb.Min, bb.Max, 100);
        foreach (var node in nodes)
        {
            if (node.BoundingBox != null)
            {
                tree.Put(node.BoundingBox.Min, node.BoundingBox.Max, node);
            }
        }

        // ray casting
        IEnumerable<Node> TraceRay(RayEx ray)
        {
            var distance = float.MaxValue;
            Node? node = null;

            var potentialNodes = tree.GetValues(ray.Box.Min, ray.Box.Max);
            foreach (var potentialNode in potentialNodes)
            {
                foreach (var primitive in potentialNode.Primitives)
                {
                    var result = primitive switch
                    {
                        TessellatedPrimitive rayCast => MatchRayCast(rayCast.Triangles, ray.Ray),
                        _ => MatchBoundingBox(primitive.OriginalPrimitive.AxisAlignedBoundingBox, ray.Box, ray.Ray)
                    };
                    if (result.Hit && result.Distance < distance)
                    {
                        distance = result.Distance;
                        node = potentialNode;
                    }
                }
            }

            if (node.HasValue)
            {
                yield return node.Value;
            }
        }

        var exteriorNodeSet = CreateRays(bb.Min, bb.Max).AsParallel().SelectMany(TraceRay).ToHashSet();

        var exterior = exteriorNodeSet.SelectMany(n => n.Primitives.Select(p => p.OriginalPrimitive)).ToArray();
        var interior = primitives.Except(exterior).ToArray();

        return (exterior, interior);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static (bool Hit, float Distance) MatchRayCast(Triangle[] triangles, Ray ray)
    {
        foreach (var triangle in triangles)
        {
            if (ray.Trace(triangle, out var intersectionPoint, out var isFrontFace))
            {
                var distance = (ray.Origin - intersectionPoint).Length();
                return (true, distance);
            }
        }

        return (false, float.NaN);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static (bool Hit, float Distance) MatchBoundingBox(BoundingBox boundingBox, BoundingBox rayBounds, Ray ray)
    {
        // positive if overlaps
        var diff = Vector3.Min(boundingBox.Max, rayBounds.Max) - Vector3.Max(boundingBox.Min, rayBounds.Min);
        var isHit = diff.X > 0f && diff.Y > 0f && diff.Z > 0f;
        if (isHit)
        {
            float distance;
            if (ray.Direction.X < 0f)
            {
                distance = boundingBox.Min.X - ray.Origin.X;
            }
            else if (ray.Direction.Y < 0f)
            {
                distance = boundingBox.Min.Y - ray.Origin.Y;
            }
            else if (ray.Direction.Z < 0f)
            {
                distance = boundingBox.Min.Z - ray.Origin.Z;
            }
            else if (ray.Direction.X > 0f)
            {
                distance = ray.Origin.X - boundingBox.Max.X;
            }
            else if (ray.Direction.Y > 0f)
            {
                distance = ray.Origin.Y - boundingBox.Max.Y;
            }
            else
            {
                distance = ray.Origin.Z - boundingBox.Max.Z;
            }
            return (true, distance);
        }

        return (false, float.NaN);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IEnumerable<Triangle> CollectTrianglesForMesh(Mesh mesh)
    {
        var triangleCount = mesh.Indices.Length / 3;
        var vertices = mesh.Vertices;
        for (var i = 0; i < triangleCount; i++)
        {
            var v1 = vertices[mesh.Indices[i * 3]];
            var v2 = vertices[mesh.Indices[i * 3 + 1]];
            var v3 = vertices[mesh.Indices[i * 3 + 2]];
            yield return new Triangle(v1, v2, v3);
        }
    }

    private static Node[] CreateNodes(APrimitive[] primitives)
    {
        static TessellatedPrimitive? TessellateInstancedMesh(InstancedMesh primitive)
        {
            var triangles = CollectTrianglesForMesh(primitive.TemplateMesh).ToArray();
            return new TessellatedPrimitive(triangles, primitive);
        }

        static TessellatedPrimitive? TessellateTriangleMesh(TriangleMesh primitive)
        {
            var triangles = CollectTrianglesForMesh(primitive.Mesh).ToArray();
            return new TessellatedPrimitive(triangles, primitive);
        }

        static Node ConvertNode(IGrouping<ulong, APrimitive> nodeGroup)
        {
            var boundingBox = nodeGroup.ToArray().CalculateBoundingBox();

            var primitives = nodeGroup
                .Select(
                    p =>
                        p switch
                        {
                            InstancedMesh instancedMesh => TessellateInstancedMesh(instancedMesh),
                            TriangleMesh triangleMesh => TessellateTriangleMesh(triangleMesh),
                            _ => new Primitive(p)
                        }
                )
                .WhereNotNull()
                .ToArray();

            return new Node(boundingBox, primitives);
        }

        return primitives.GroupBy(p => p.TreeIndex).AsParallel().Select(ConvertNode).ToArray();
    }
}
