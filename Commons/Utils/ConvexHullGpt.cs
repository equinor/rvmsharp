namespace Commons.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

public class ConvexHull3D
{

    public class Face
    {
        public int Vertex1,
            Vertex2,
            Vertex3;
        public bool Removed;

        public Face(int vertex1, int vertex2, int vertex3)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
            Removed = false;
        }
    }

    public List<Face> GenerateConvexHull(
        List<System.Numerics.Vector3> points
    )
    {
        if (points == null || points.Count < 4)
        {
            throw new ArgumentException("At least four points required to generate a convex hull.");
        }

        List<Face> faces = [];

        // Create initial convex hull with the first four points
        // making sure they are not co-planar by checking volumes of tetrahedrons
        bool initialHullCreated = false;
        for (int i = 3; i < points.Count && !initialHullCreated; i++)
        {
            var initialVolume = SignedVolume(points[0], points[1], points[2], points[i]);
            if (Math.Abs(initialVolume) > 1e-7)
            {
                faces.Add(new Face(0, 1, 2));
                faces.Add(new Face(0, 2, i));
                faces.Add(new Face(2, 1, i));
                faces.Add(new Face(1, 0, i));
                initialHullCreated = true;
            }
        }

        if (!initialHullCreated)
            throw new ArgumentException("Initial points are coplanar.");

        // Compute the initial outside set for each face
        List<List<int>> outsideSets = faces.Select(_ => new List<int>()).ToList();

        for (int i = 4; i < points.Count; i++)
        {
            for (int j = 0; j < faces.Count; j++)
            {
                if (
                    IsPointAboveFace(
                        points[i],
                        points[faces[j].Vertex1],
                        points[faces[j].Vertex2],
                        points[faces[j].Vertex3]
                    )
                )
                {
                    outsideSets[j].Add(i);
                    break;
                }
            }
        }

        // Start expanding the hull
        bool expanded = true;
        while (expanded)
        {
            expanded = false;

            // Find face with the furthest outside point
            int faceIndex = -1;
            double maxDistance = 0;
            int furthestPointIndex = -1;
            for (int i = 0; i < faces.Count; i++)
            {
                if (faces[i].Removed)
                    continue;

                foreach (int pointIndex in outsideSets[i])
                {
                    var distance = PointFaceDistance(
                        points[pointIndex],
                        points[faces[i].Vertex1],
                        points[faces[i].Vertex2],
                        points[faces[i].Vertex3]
                    );
                    if (distance > maxDistance)
                    {
                        faceIndex = i;
                        maxDistance = distance;
                        furthestPointIndex = pointIndex;
                    }
                }
            }

            // No face with an outside point, convex hull is complete
            if (faceIndex == -1)
                break;

            // Remove faces that can see the new point
            List<int> facesToBeRemoved = [];
            List<Edge> horizonEdges = [];
            FindVisibleFaces(furthestPointIndex, faces, outsideSets, facesToBeRemoved, horizonEdges, points);

            // Remove faces
            foreach (var idx in facesToBeRemoved)
            {
                faces[idx].Removed = true;
            }

            // Re-triangulate the hole
            foreach (var edge in horizonEdges)
            {
                // Make a new face from the edge to the new point
                faces.Add(new Face(edge.Start, edge.End, furthestPointIndex));
                outsideSets.Add([]);
            }

            // Re-distribute outside sets
            RedistributeOutsideSets(points, faces, outsideSets, facesToBeRemoved);

            expanded = true;
        }

        // Finalize the hull by removing all the removed faces
        faces = faces.Where(face => !face.Removed).ToList();
        return faces;
    }

    private void RedistributeOutsideSets(
        List<System.Numerics.Vector3> points,
        List<Face> faces,
        List<List<int>> outsideSets,
        List<int> facesToBeRemoved
    )
    {
        // For each outside point of the faces to be removed, try to assign it to one of the new faces
        foreach (var faceIndex in facesToBeRemoved)
        {
            foreach (var pointIndex in outsideSets[faceIndex])
            {
                bool isAssigned = false;
                for (int i = 0; i < faces.Count && !isAssigned; i++)
                {
                    // Skip removed faces and the one that have been processed already
                    if (faces[i].Removed || facesToBeRemoved.Contains(i))
                        continue;

                    if (
                        IsPointAboveFace(
                            points[pointIndex],
                            points[faces[i].Vertex1],
                            points[faces[i].Vertex2],
                            points[faces[i].Vertex3]
                        )
                    )
                    {
                        outsideSets[i].Add(pointIndex);
                        isAssigned = true;
                    }
                }
            }
        }
    }

    private void FindVisibleFaces(
        int pointIndex,
        List<Face> faces,
        List<List<int>> outsideSets,
        List<int> facesToBeRemoved,
        List<Edge> horizonEdges,
        List<System.Numerics.Vector3> points
    )
    {
        Queue<int> faceQueue = new();
        HashSet<int> visitedFaces = [];

        // Find an initial visible face and start from there
        for (int i = 0; i < faces.Count; i++)
        {
            if (faces[i].Removed)
                continue;
            if (
                IsPointAboveFace(
                    points[pointIndex],
                    points[faces[i].Vertex1],
                    points[faces[i].Vertex2],
                    points[faces[i].Vertex3]
                )
            )
            {
                faceQueue.Enqueue(i);
                facesToBeRemoved.Add(i);
                visitedFaces.Add(i);
                break;
            }
        }

        while (faceQueue.Count > 0)
        {
            int currentFaceIndex = faceQueue.Dequeue();

            var currentFace = faces[currentFaceIndex];
            // Find all adjacent faces that are visible from the point
            List<Edge> edges =
            [
                new Edge(currentFace.Vertex1, currentFace.Vertex2),
                new Edge(currentFace.Vertex2, currentFace.Vertex3),
                new Edge(currentFace.Vertex3, currentFace.Vertex1)
            ];

            // For every edge, see if the opposite face is visible, if so add it to the horizon and queue
            foreach (var edge in edges)
            {
                Edge reversedEdge = new Edge(edge.End, edge.Start);
                int adjacentFaceIndex = FindAdjacentFaceIndex(reversedEdge, faces, currentFaceIndex);

                if (adjacentFaceIndex != -1 && !visitedFaces.Contains(adjacentFaceIndex))
                {
                    if (
                        IsPointAboveFace(
                            points[pointIndex],
                            points[faces[adjacentFaceIndex].Vertex1],
                            points[faces[adjacentFaceIndex].Vertex2],
                            points[faces[adjacentFaceIndex].Vertex3]
                        )
                    )
                    {
                        faceQueue.Enqueue(adjacentFaceIndex);
                        facesToBeRemoved.Add(adjacentFaceIndex);
                        visitedFaces.Add(adjacentFaceIndex);
                    }
                    else
                    {
                        // If it's not visible it's part of the horizon
                        horizonEdges.Add(edge);
                    }
                }
            }
        }
    }

    // Returns the index of the face that is opposite to the given edge and not the given face index
    private int FindAdjacentFaceIndex(Edge edge, List<Face> faces, int currentFaceIndex)
    {
        return faces.FindIndex(
            (Face face) =>
                face.Vertex1 == edge.Start
                && face.Vertex2 == edge.End
                && !face.Removed
                && faces.IndexOf(face) != currentFaceIndex
        );
    }

    private double SignedVolume(
        System.Numerics.Vector3 a,
        System.Numerics.Vector3 b,
        System.Numerics.Vector3 c,
        System.Numerics.Vector3 d
    )
    {
        return (
                (b.X - a.X) * ((c.Y - a.Y) * (d.Z - a.Z) - (d.Y - a.Y) * (c.Z - a.Z))
                - (b.Y - a.Y) * ((c.X - a.X) * (d.Z - a.Z) - (d.X - a.X) * (c.Z - a.Z))
                + (b.Z - a.Z) * ((c.X - a.X) * (d.Y - a.Y) - (d.X - a.X) * (c.Y - a.Y))
            ) / 6;
    }

    private bool IsPointAboveFace(
        System.Numerics.Vector3 p,
        System.Numerics.Vector3 a,
        System.Numerics.Vector3 b,
        System.Numerics.Vector3 c
    )
    {
        return SignedVolume(a, b, c, p) > 1e-7;
    }

    private double PointFaceDistance(
        System.Numerics.Vector3 p,
        System.Numerics.Vector3 a,
        System.Numerics.Vector3 b,
        System.Numerics.Vector3 c
    )
    {
        // The normal of the triangle plane
        var normal = CrossProduct(
            new System.Numerics.Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z),
            new System.Numerics.Vector3(c.X - a.X, c.Y - a.Y, c.Z - a.Z)
        );

        // Normalize the normal
        var length = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
        normal = new System.Numerics.Vector3(
            (float)(normal.X / length),
            (float)(normal.Y / length),
            (float)(normal.Z / length)
        );

        // Using normal and point on the plane (a) calculate distance from point (p) to the plane
        return Math.Abs(normal.X * (p.X - a.X) + normal.Y * (p.Y - a.Y) + normal.Z * (p.Z - a.Z));
    }

    private System.Numerics.Vector3 CrossProduct(System.Numerics.Vector3 v1, System.Numerics.Vector3 v2)
    {
        return new System.Numerics.Vector3(
            v1.Y * v2.Z - v1.Z * v2.Y,
            v1.Z * v2.X - v1.X * v2.Z,
            v1.X * v2.Y - v1.Y * v2.X
        );
    }

    private class Edge
    {
        public int Start,
            End;

        public Edge(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}
