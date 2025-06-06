namespace CadRevealComposer.Utils.MeshOptimization;

using System;
using System.Diagnostics;

public static class VertexFetchOptimizer
{
    // MIT License
    //
    // Copyright (c) 2016-2023 Arseny Kapoulkine, 2023 Nils Henrik Hals
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
    //
    //     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

    // This is a port of the vcacheoptimizer.cpp file from https://github.com/zeux/meshoptimizer/:
    // https://github.com/zeux/meshoptimizer/blob/b4afc3af005dfeffdbde60bf677106fac41c1f9f/src/vcacheoptimizer.cpp
    // Most of the comments, all magic numbers etc is from that implementation.
    // I have added dotnet specific changes and optimizations

    public static uint OptimizeVertexFetchRemap(
        Span<uint> destination,
        ReadOnlySpan<uint> indices,
        uint indexCount,
        uint vertexCount
    )
    {
        Debug.Assert(indexCount % 3 == 0);

        destination.Fill(uint.MaxValue);

        uint nextVertex = 0;

        for (int i = 0; i < indexCount; ++i)
        {
            uint index = indices[i];
            Debug.Assert(index < vertexCount);

            if (destination[(int)index] == uint.MaxValue)
            {
                destination[(int)index] = nextVertex++;
            }
        }

        Debug.Assert(nextVertex <= vertexCount);

        return nextVertex;
    }

    /// <summary>
    /// * Vertex fetch cache optimizer
    /// * Reorders vertices and changes indices to reduce the amount of GPU memory fetches during vertex processing
    /// * Returns the number of unique vertices, which is the same as input vertex count unless some vertices are unused
    /// </summary>
    /// <param name="destination">The output array, can be same as the vertices input for in-place optimization</param>
    /// <param name="indices">Will reorder the indices</param>
    /// <param name="vertices">A span of vertices</param>
    /// <typeparam name="T">The T is not actually used</typeparam>
    public static uint OptimizeVertexFetch<T>(Span<T> destination, Span<uint> indices, ReadOnlySpan<T> vertices)
    {
        var indexCount = indices.Length;
        var vertexCount = vertices.Length;
        Debug.Assert(indexCount % 3 == 0);
        // support in-place optimization (Copy the input vertices if its the same as output to avoid modifying while running)
        vertices = destination == vertices ? vertices.ToArray() : vertices;

        // build vertex remap table
        Span<uint> vertexRemap = new uint[vertexCount];
        vertexRemap.Fill(uint.MaxValue);

        uint nextVertex = 0;

        for (int i = 0; i < indexCount; ++i)
        {
            uint index = indices[i];
            Debug.Assert(index < vertexCount);

            // Reference to the uint at that point in the Span. Hacky dirty C++ stuffs
            ref var remap = ref vertexRemap[(int)index];

            if (remap == uint.MaxValue) // vertex was not added to destination VB
            {
                // add vertex
                destination[(int)nextVertex] = vertices[(int)index];
                remap = nextVertex++;
            }

            // modify indices in place
            indices[i] = remap;
        }

        Debug.Assert(nextVertex <= vertexCount);
        return nextVertex;
    }
}
