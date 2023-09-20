namespace CadRevealFbxProvider.BatchUtils;

using CadRevealComposer.Tessellation;
using CadRevealComposer.Utils.Comparers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class MeshTools
{
    /// <summary>
    /// Remove re-used vertices, and remap the Triangle indices to the new unique table.
    /// Saves memory but assumes the mesh ONLY has Position and Index data. Discards any normals!
    ///
    /// Returns a new Mesh
    /// </summary>
    public static Mesh DeduplicateVertices(Mesh input)
    {
        var comparer = new XyzVector3EqualityComparer();
        var alreadyFoundVerticesToIndexMap = new Dictionary<Vector3, int>(comparer);

        var newVertices = new List<Vector3>();
        var indicesCopy = input.Indices.ToArray();

        // The index in the oldVertexIndexToNewIndexRemap array is the old index, and the value is the new index. (Think of it as a dict)
        var oldVertexIndexToNewIndexRemap = new int[input.Vertices.Count()];

        for (uint i = 0; i < input.Vertices.Count(); i++)
        {
            var vertex = input.Vertices[(int)i];
            if (!alreadyFoundVerticesToIndexMap.TryGetValue(vertex, out int newIndex))
            {
                newIndex = newVertices.Count;
                newVertices.Add(vertex);
                alreadyFoundVerticesToIndexMap.Add(vertex, newIndex);
            }

            oldVertexIndexToNewIndexRemap[i] = newIndex;
        }
        // Explicitly clear to unload memory as soon as possible.
        alreadyFoundVerticesToIndexMap.Clear();

        for (int i = 0; i < indicesCopy.Length; i++)
        {
            var originalIndex = indicesCopy[i];
            var vertexIndex = oldVertexIndexToNewIndexRemap[originalIndex];
            indicesCopy[i] = vertexIndex;
        }

        return new Mesh(newVertices.ToArray(), indicesCopy, 0);
    }
}
