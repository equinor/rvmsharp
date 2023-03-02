namespace CadRevealComposer.Utils.MeshTools;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class VertexCacheOptimizer
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

    // This work is based on:
    // Tom Forsyth. Linear-Speed Vertex Cache Optimisation. 2006
    // Pedro Sander, Diego Nehab and Joshua Barczak. Fast Triangle Reordering for Vertex Locality and Reduced Overdraw. 2007
    const uint CacheSizeMax = 16;
    const uint ValenceMax = 8;

    class VertexScoreTable
    {
        public float[] Cache = new float[1 + CacheSizeMax];
        public float[] Live = new float[1 + ValenceMax];
    };

    ref struct TriangleAdjacency
    {
        // Count of references to a given Vertex
        public Span<uint> Counts;

        // Offsets of the Vertex in the Data array
        public Span<uint> Offsets;

        // The Vertexes
        public Span<uint> Data;
    };


// Tuned to minimize the Average Cache Miss Ratio (ACMR) of a GPU that has a cache profile similar to NVidia and AMD
// csharpier-ignore
    static readonly VertexScoreTable VertexScoreTableInstance = new VertexScoreTable()
    {
        Cache = new float[]
        {
            0,
            0.779f,
            0.791f,
            0.789f,
            0.981f,
            0.843f,
            0.726f,
            0.847f,
            0.882f,
            0.867f,
            0.799f,
            0.642f,
            0.613f,
            0.600f,
            0.568f,
            0.372f,
            0.234f
        },
        Live = new float[] { 0, 0.995f, 0.713f, 0.450f, 0.404f, 0.059f, 0.005f, 0.147f, 0.006f }
    };

// Tuned to minimize the encoded index buffer size
    static readonly VertexScoreTable KVertexScoreTableStrip = new VertexScoreTable
    {
        Cache = new[]
        {
            0,
            1.000f,
            1.000f,
            1.000f,
            0.453f,
            0.561f,
            0.490f,
            0.459f,
            0.179f,
            0.526f,
            0.000f,
            0.227f,
            0.184f,
            0.490f,
            0.112f,
            0.050f,
            0.131f
        },
        Live = new[] { 0, 0.956f, 0.786f, 0.577f, 0.558f, 0.618f, 0.549f, 0.499f, 0.489f },
    };

    private static void BuildTriangleAdjacency(ref TriangleAdjacency adjacency, ReadOnlySpan<uint> indices,
        uint vertexCount)
    {
        int indexCount = indices.Length;
        int faceCount = indexCount / 3;

        // allocate arrays
        // Vertex use count
        adjacency.Counts = new uint[vertexCount];
        // Vertex offsets?
        adjacency.Offsets = new uint[vertexCount];
        // Data for each index?
        adjacency.Data = new uint[indexCount];

        // fill triangle counts
        for (int i = 0; i < indexCount; ++i)
        {
            Debug.Assert(indices[i] < vertexCount);

            // Vertex use count
            adjacency.Counts[(int)indices[i]]++;
        }

        // fill offset table
        uint offset = 0;

        for (int i = 0; i < vertexCount; ++i)
        {
            adjacency.Offsets[i] = offset;
            offset += adjacency.Counts[i];
        }

        Debug.Assert(offset == indexCount);

        // fill triangle data
        for (int i = 0; i < faceCount; ++i)
        {
            uint a = indices[i * 3 + 0], b = indices[i * 3 + 1], c = indices[i * 3 + 2];

            adjacency.Data[(int)adjacency.Offsets[(int)a]++] = (uint)i;
            adjacency.Data[(int)adjacency.Offsets[(int)b]++] = (uint)i;
            adjacency.Data[(int)adjacency.Offsets[(int)c]++] = (uint)i;
        }

        // fix offsets that have been disturbed by the previous pass
        for (int i = 0; i < vertexCount; ++i)
        {
            Debug.Assert(adjacency.Offsets[i] >= adjacency.Counts[i]);

            adjacency.Offsets[i] -= adjacency.Counts[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetNextVertexDeadEnd(Span<uint> deadEnd, ref uint deadEndTop, ref uint inputCursor,
        Span<uint> liveTriangles,
        uint vertexCount)
    {
        // check dead-end stack
        while (deadEndTop != 0)
        {
            uint vertex = deadEnd[(int)--deadEndTop];

            if (liveTriangles[(int)vertex] > 0)
                return vertex;
        }

        // input order
        while (inputCursor < vertexCount)
        {
            if (liveTriangles[(int)inputCursor] > 0)
                return inputCursor;

            ++inputCursor;
        }

        return ~0u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetNextVertexNeighbor(ReadOnlySpan<uint> nextCandidates,
        ReadOnlySpan<uint> liveTriangles, ReadOnlySpan<uint> cacheTimestamps, uint timestamp, uint cacheSize)
    {
        uint bestCandidate = ~0u;
        int bestPriority = -1;

        for (int i = 0; i < nextCandidates.Length; i++)
        {
            int vertex = (int)nextCandidates[i];

            // otherwise we don't need to process it
            if (liveTriangles[vertex] > 0)
            {
                int priority = 0;

                // will it be in cache after fanning?
                if (2 * liveTriangles[vertex] + timestamp - cacheTimestamps[vertex] <= cacheSize)
                {
                    priority = (int)(timestamp - cacheTimestamps[vertex]); // position in cache
                }

                if (priority > bestPriority)
                {
                    bestCandidate = (uint)vertex;
                    bestPriority = priority;
                }
            }
        }

        return bestCandidate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float VertexScore(ref VertexScoreTable table, int cachePosition, uint liveTriangles)
    {
        Debug.Assert(cachePosition >= -1 && cachePosition < CacheSizeMax);

        uint liveTrianglesClamped = liveTriangles < ValenceMax ? liveTriangles : ValenceMax;

        return table.Cache[1 + cachePosition] + table.Live[liveTrianglesClamped];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetNextTriangleDeadEnd(ref uint inputCursor, ref bool[] emittedFlags, uint faceCount)
    {
        // input order
        while (inputCursor < faceCount)
        {
            if (!emittedFlags[inputCursor])
                return inputCursor;

            ++inputCursor;
        }

        return ~0u;
    }

    static void OptimizeVertexCacheTable(Span<uint> destination, ReadOnlySpan<uint> inputIndices, uint vertexCount,
        VertexScoreTable table)
    {
        // If input and output are the same reference we keep a copy of the input indexes
        var indices = inputIndices == destination ? inputIndices.ToArray() : inputIndices;
        uint indexCount = (uint)indices.Length;
        Debug.Assert(indices.Length % 3 == 0);

        // guard for empty meshes
        if (indexCount == 0 || vertexCount == 0)
            return;

        const uint cacheSize = 16;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        Debug.Assert(cacheSize <= CacheSizeMax);

        uint faceCount = indexCount / 3;

        // build adjacency information
        TriangleAdjacency adjacency = new TriangleAdjacency();
        BuildTriangleAdjacency(ref adjacency, indices, vertexCount);

        // live triangle counts
        uint[] liveTriangles = adjacency.Counts.ToArray();

        // emitted flags
        bool[] emittedFlags = new bool[faceCount];

        // compute initial vertex scores
        float[] vertexScores = new float[vertexCount];

        for (uint i = 0; i < vertexCount; ++i)
            vertexScores[i] = VertexScore(ref table, -1, liveTriangles[i]);

        // compute triangle scores
        float[] triangleScores = new float[faceCount];

        for (uint i = 0; i < faceCount; ++i)
        {
            uint a = indices[(int)(i * 3 + 0)];
            uint b = indices[(int)(i * 3 + 1)];
            uint c = indices[(int)(i * 3 + 2)];

            triangleScores[i] = vertexScores[a] + vertexScores[b] + vertexScores[c];
        }

        // uint cache_holder[2 * (kCacheSizeMax + 3)];
        uint[] cache = new uint[(CacheSizeMax + 3)];
        uint[] cacheNew = new uint[CacheSizeMax + 3];
        uint cacheCount = 0;

        uint currentTriangle = 0;
        uint inputCursor = 1;

        uint outputTriangle = 0;

        while (currentTriangle != ~0u)
        {
            Debug.Assert(outputTriangle < faceCount);

            uint a = indices[(int)(currentTriangle * 3 + 0)];
            uint b = indices[(int)(currentTriangle * 3 + 1)];
            uint c = indices[(int)(currentTriangle * 3 + 2)];

            // output indices
            destination[(int)(outputTriangle * 3 + 0)] = a;
            destination[(int)(outputTriangle * 3 + 1)] = b;
            destination[(int)(outputTriangle * 3 + 2)] = c;
            outputTriangle++;

            // update emitted flags
            emittedFlags[currentTriangle] = true;
            triangleScores[currentTriangle] = 0;

            // new triangle
            uint cacheWrite = 0;
            cacheNew[cacheWrite++] = a;
            cacheNew[cacheWrite++] = b;
            cacheNew[cacheWrite++] = c;

            // old triangles
            for (uint i = 0; i < cacheCount; ++i)
            {
                uint index = cache[i];

                if (index != a && index != b && index != c)
                {
                    cacheNew[cacheWrite++] = index;
                }
            }

            // Swap caches
            (cache, cacheNew) = (cacheNew, cache);
            cacheCount = cacheWrite > cacheSize ? cacheSize : cacheWrite;

            // update live triangle counts
            liveTriangles[a]--;
            liveTriangles[b]--;
            liveTriangles[c]--;

            // remove emitted triangle from adjacency data
            // this makes sure that we spend less time traversing these lists on subsequent iterations
            for (uint k = 0; k < 3; ++k)
            {
                uint index = indices[(int)(currentTriangle * 3 + k)];

                int neighborsSize = (int)adjacency.Counts[(int)index];
                var neighbors = adjacency.Data.Slice((int)adjacency.Offsets[(int)index], neighborsSize);

                for (int i = 0; i < neighborsSize; ++i)
                {
                    uint tri = neighbors[i];

                    if (tri == currentTriangle)
                    {
                        neighbors[i] = neighbors[neighborsSize - 1];
                        adjacency.Counts[(int)index]--;
                        break;
                    }
                }
            }

            uint bestTriangle = ~0u;
            float bestScore = 0;

            // update cache positions, vertex scores and triangle scores, and find next best triangle
            for (uint i = 0; i < cacheWrite; ++i)
            {
                uint index = cache[i];

                int cachePosition = i >= cacheSize ? -1 : (int)i;

                // update vertex score
                float score = VertexScore(ref table, cachePosition, liveTriangles[index]);
                float scoreDiff = score - vertexScores[index];

                vertexScores[index] = score;

                // update scores of vertex triangles

                uint neighborsBegin = adjacency.Offsets[(int)index];
                uint neighborsEnd = neighborsBegin + adjacency.Counts[(int)index];

                for (uint it = neighborsBegin; it != neighborsEnd; ++it)
                {
                    uint tri = adjacency.Data[(int)it];
                    Debug.Assert(!emittedFlags[tri]);

                    float triScore = triangleScores[tri] + scoreDiff;
                    Debug.Assert(triScore > 0);

                    if (bestScore < triScore)
                    {
                        bestTriangle = tri;
                        bestScore = triScore;
                    }

                    triangleScores[tri] = triScore;
                }
            }

            // step through input triangles in order if we hit a dead-end
            currentTriangle = bestTriangle;

            if (currentTriangle == ~0u)
            {
                currentTriangle = GetNextTriangleDeadEnd(ref inputCursor, ref emittedFlags, faceCount);
            }
        }

        Debug.Assert(inputCursor == faceCount);
        Debug.Assert(outputTriangle == faceCount);
    }

    /// <summary>
    /// Vertex transform cache optimizer
    /// Reorders indices to reduce the number of GPU vertex shader invocations
    /// If index buffer contains multiple ranges for multiple draw calls, this functions needs to be called on each range individually.
    /// </summary>
    /// <param name="destination">Destination array, can be same as input if you want in-place edits</param>
    /// <param name="inputIndices">Input indexes, can be same as destination</param>
    /// <param name="vertexCount">Vertex count of the mesh (Vertices.Length)</param>
    public static void OptimizeVertexCache(Span<uint> destination, ReadOnlySpan<uint> inputIndices, uint vertexCount)
    {
        OptimizeVertexCacheTable(destination, inputIndices, vertexCount, VertexScoreTableInstance);
    }

    /// <summary>
    /// Vertex transform cache optimizer for strip-like caches
    /// Produces inferior results to OptimizeVertexCache from the GPU vertex cache perspective
    /// However, the resulting index order is more optimal if the goal is to reduce the triangle strip length or improve compression efficiency
    /// </summary>
    /// <param name="destination">Destination array, can be same as input if you want in-place edits</param>
    /// <param name="inputIndices">Input index array</param>
    /// <param name="vertexCount">Vertex count of the mesh (Vertices.Length)</param>
    public static void OptimizeVertexCacheStrip(Span<uint> destination, ReadOnlySpan<uint> inputIndices, uint
        vertexCount)
    {
        OptimizeVertexCacheTable(destination, inputIndices, vertexCount,
            KVertexScoreTableStrip);
    }

    /// <summary>
    /// Vertex transform cache optimizer for FIFO caches
    /// Reorders indices to reduce the number of GPU vertex shader invocations
    /// Generally takes ~3x less time to optimize meshes but produces inferior results compared to OptimizeVertexCache
    /// If index buffer contains multiple ranges for multiple draw calls, this functions needs to be called on each range individually.
    /// destination must contain enough space for the resulting index buffer (index_count elements)
    /// </summary>
    /// <param name="destination">Destination array, can be same as input if you want in-place edits</param>
    /// <param name="inputIndices">Input index array</param>
    /// <param name="vertexCount">Vertex count of the mesh (Vertices.Length)</param>
    /// <param name="cacheSize">CacheSize should be less than the actual GPU cache size to avoid cache thrashing</param>
    public static void OptimizeVertexCacheFifo(Span<uint> destination, ReadOnlySpan<uint> inputIndices, uint
        vertexCount, uint cacheSize = 16)
    {
        // If input and output are the same, keep a copy of the input indexes
        var indices = inputIndices == destination ? inputIndices.ToArray() : inputIndices;
        uint indexCount = (uint)indices.Length;
        Debug.Assert(indexCount % 3 == 0);
        Debug.Assert(cacheSize >= 3);

        // guard for empty meshes
        if (indexCount == 0 || vertexCount == 0)
            return;

        uint faceCount = indexCount / 3;

        // build adjacency information
        TriangleAdjacency adjacency = new TriangleAdjacency();
        BuildTriangleAdjacency(ref adjacency, indices, vertexCount);

        // live triangle counts
        Span<uint> liveTriangles = adjacency.Counts.ToArray();
        // cache time stamps
        Span<uint> cacheTimestamps = new uint[vertexCount];

        // dead-end stack
        Span<uint> deadEnd = new uint[indexCount];
        uint deadEndTop = 0;

        // emitted flags
        Span<bool> emittedFlags = new bool[faceCount];

        uint currentVertex = 0;

        uint timestamp = cacheSize + 1;
        uint inputCursor = 1; // vertex to restart from in case of dead-end

        uint outputTriangle = 0;


        while (currentVertex != ~0u)
        {
            uint nextCandidatesBegin = deadEndTop;

            // emit all vertex neighbors
            var neighbors = adjacency.Data.Slice((int)adjacency.Offsets[(int)currentVertex],
                (int)adjacency.Counts[(int)currentVertex]);
            for (uint i = 0; i < neighbors.Length; ++i)
            {
                uint triangle = neighbors[(int)i];

                if (!emittedFlags[(int)triangle])
                {
                    uint a = indices[(int)(triangle * 3 + 0)],
                        b = indices[(int)(triangle * 3 + 1)],
                        c = indices[(int)(triangle * 3 + 2)];

                    // output indices
                    destination[(int)(outputTriangle * 3 + 0)] = a;
                    destination[(int)(outputTriangle * 3 + 1)] = b;
                    destination[(int)(outputTriangle * 3 + 2)] = c;
                    outputTriangle++;

                    // update dead-end stack
                    deadEnd[(int)(deadEndTop + 0)] = a;
                    deadEnd[(int)(deadEndTop + 1)] = b;
                    deadEnd[(int)(deadEndTop + 2)] = c;
                    deadEndTop += 3;

                    // update live triangle counts
                    liveTriangles[(int)a]--;
                    liveTriangles[(int)b]--;
                    liveTriangles[(int)c]--;

                    // update cache info
                    // if vertex is not in cache, put it in cache
                    if (timestamp - cacheTimestamps[(int)a] > cacheSize)
                        cacheTimestamps[(int)a] = timestamp++;

                    if (timestamp - cacheTimestamps[(int)b] > cacheSize)
                        cacheTimestamps[(int)b] = timestamp++;

                    if (timestamp - cacheTimestamps[(int)c] > cacheSize)
                        cacheTimestamps[(int)c] = timestamp++;

                    // update emitted flags
                    emittedFlags[(int)triangle] = true;
                }
            }

            // next candidates are the ones we pushed to dead-end stack just now
            var nextCandidates = deadEnd.Slice((int)nextCandidatesBegin, (int)(deadEndTop - nextCandidatesBegin));

            currentVertex = GetNextVertexNeighbor(nextCandidates, liveTriangles,
                cacheTimestamps, timestamp, cacheSize);

            if (currentVertex == ~0u)
            {
                currentVertex = GetNextVertexDeadEnd(deadEnd, ref deadEndTop, ref inputCursor, liveTriangles,
                    vertexCount);
            }
        }

        Debug.Assert(outputTriangle == faceCount);
    }

    /// <summary>
    /// Computes the Average Cache Miss Ratio from the Input indices and selected cache size.
    /// Can be used to evaluate the performance of the OptimizeVertexCache methods.
    /// </summary>
    /// <param name="indices">The indices</param>
    /// <param name="cacheSize">The cache size</param>
    /// <returns></returns>
    public static float ComputeAverageCacheMissRatio(ReadOnlySpan<uint> indices, uint
        cacheSize = CacheSizeMax)
    {
        // From https://github.com/Sigkill79/sts/blob/master/sts_vertex_cache_optimizer.h
        uint numCacheMisses = 0;
        Span<int> cache = new int[cacheSize];

        Debug.Assert(indices.Length % 3 == 0, "Index input has to be triangles");

        for (int i = 0; i < cacheSize; ++i) cache[i] = -1;

        for (int v = 0; v < indices.Length; ++v)
        {
            uint index = indices[v];
            // check if vertex in cache
            bool foundInCache = false;
            for (int c = 0; c < cacheSize && cache[c] >= 0 && !foundInCache; ++c)
            {
                if (cache[c] == index) foundInCache = true;
            }

            if (!foundInCache)
            {
                ++numCacheMisses;
                for (int c = (int)cacheSize - 1; c >= 1; --c)
                {
                    cache[c] = cache[c - 1];
                }

                cache[0] = (int)index;
            }
        }

        float triangleCount = (indices.Length / 3f);
        return numCacheMisses / triangleCount;
    }
}