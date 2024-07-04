using CadRevealComposer.Tessellation;
using System.Numerics;

namespace CadRevealFbxProvider.Utils;

public static class MeshUtils
{
    private class Face(Vector3 v1, Vector3 v2, Vector3 v3) : IEquatable<Face>
    {
        private readonly Vector3[] _vertices = [v1, v2, v3];
        private readonly Vector3 _v1 = v1;
        private readonly Vector3 _v2 = v2;
        private readonly Vector3 _v3 = v3;
        private int? hashCodeCahce;

        public bool Contains(Vector3 v) => _v1.Equals(v) || _v2.Equals(v) || _v3.Equals(v);
        public Vector3[] GetVertices() => _vertices;

        public List<Face> AdjacentFaces { get; set; } = new List<Face>();

        public bool Equals(Face? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this._v1 == other._v1 && this._v2 == other._v2 && this._v3 == other._v3;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Face)obj);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode -- For maximized performance
            return hashCodeCahce ??= HashCode.Combine(_v1.GetHashCode(), _v2.GetHashCode(), _v3.GetHashCode());;
        }
    }

    private static IEnumerable<Face> CreateFacesFromMesh(Mesh mesh)
    {
        for (int i = 0; i < mesh.TriangleCount; i++)
        {
            var index = i * 3;
            var v1 = mesh.Vertices[mesh.Indices[index]];
            var v2 = mesh.Vertices[mesh.Indices[index + 1]];
            var v3 = mesh.Vertices[mesh.Indices[index + 2]];
            yield return new Face(v1, v2, v3);
        }
    }


    // Method to build adjacency list
    private static void BuildAdjacencyList(IList<Face> faces)
    {
        // To build the adjacency list efficiently, first map each vertex to the list of faces it belongs to.
        Dictionary<Vector3, List<Face>> vertexFacesMap = new Dictionary<Vector3, List<Face>>();

        foreach (var face in faces)
        {
            foreach (var vertex in face.GetVertices())
            {
                if (!vertexFacesMap.TryGetValue(vertex, out var faceList))
                {
                    faceList = new List<Face>();
                    vertexFacesMap[vertex] = faceList;
                }

                faceList.Add(face);
            }
        }

        // With the vertex-face mapping, we can now determine adjacency by checking shared vertices.
        foreach (var face in faces)
        {
            // Create a set of unique adjacent faces
            HashSet<Face> adjacencySet = [];

            foreach (var vertex in face.GetVertices())
            {
                if (vertexFacesMap.TryGetValue(vertex, out var adjacentFaces))
                {
                    foreach (var adjacentFace in adjacentFaces)
                    {
                        // Do not add the face itself, only other faces sharing the same vertex
                        if (!adjacentFace.Equals(face))
                        {
                            adjacencySet.Add(adjacentFace);
                        }
                    }
                }
            }

            // Set the adjacency list of the current face
            face.AdjacentFaces = adjacencySet.ToList();
        }
    }


    private static Mesh ConvertFacesToMesh(IList<Face> faces)
    {
        // Estimate the number of vertices and indices we might need.
        var estimate = faces.Count * 3;
        var vertices = new List<Vector3>(estimate);
        var indices = new List<uint>(estimate);

        var vertexIndices = new Dictionary<Vector3, int>();

        foreach (var face in faces)
        {
            var faceVertices = face.GetVertices();
            var faceIndices = new uint[3];

            for (int i = 0; i < 3; i++)
            {
                var vertex = faceVertices[i];

                if (!vertexIndices.TryGetValue(vertex, out var index))
                {
                    index = vertices.Count;
                    vertices.Add(vertex);
                    vertexIndices.Add(vertex, index);
                }

                faceIndices[i] = (uint)index;
            }

            indices.AddRange(faceIndices);
        }

        return new Mesh(vertices.ToArray(), indices.ToArray(), 0f);
    }

    public static Mesh[] SplitDisjointPieces(Mesh input)
    {
        // A set to keep track of the visited faces
        HashSet<Face> visited = new HashSet<Face>();
        // A list to store the disjoint meshes
        List<Mesh> disjointMeshes = new List<Mesh>();

        var faces = CreateFacesFromMesh(input).ToList();
        BuildAdjacencyList(faces);

        // Iterate through all faces of this mesh
        foreach (var face in faces)
        {
            // Check if the current face has already been visited
            if (!visited.Contains(face))
            {
                // Start a new set to represent a disjoint piece
                // Start a traversal from the current unvisited face
                var disjointPiece = TraverseDisjointPiece(face, visited, faces);

                disjointMeshes.Add(ConvertFacesToMesh(disjointPiece.ToArray()));
            }
        }

        // Return the array of disjoint pieces
        return disjointMeshes.ToArray();
    }

    private static HashSet<Face> TraverseDisjointPiece(Face startFace, HashSet<Face> visited, IList<Face> allFaces)
    {
        // Stack to manage the depth-first traversal
        Stack<Face> stack = new Stack<Face>();
        // Add the start face to the stack
        stack.Push(startFace);
        var disjointPieceFaces = new HashSet<Face>();
        while (stack.Count > 0)
        {
            Face currentFace = stack.Pop();

            // If the face has not been visited, process it
            if (visited.Add(currentFace))
            {
                // Mark face as visited
                disjointPieceFaces.Add(currentFace); // Add it to the current disjoint piece

                // Get the adjacent faces and add them to the stack if not visited
                foreach (var adjacentFace in currentFace.AdjacentFaces)
                {
                    if (!visited.Contains(adjacentFace))
                    {
                        stack.Push(adjacentFace);
                    }
                }
            }
        }

        return disjointPieceFaces;
    }
}
